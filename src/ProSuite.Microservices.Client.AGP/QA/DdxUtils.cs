using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using Google.Protobuf.Collections;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.DataModel;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.DataModel;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Ddx;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using Dataset = ProSuite.DomainModel.Core.DataModel.Dataset;
using GeometryType = ProSuite.DomainModel.Core.DataModel.GeometryType;

namespace ProSuite.Microservices.Client.AGP.QA
{
	public static class DdxUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// Sometimes it takes almost two minutes!
		private const int _timeoutMilliseconds = 180000;

		public static async Task<List<ProjectWorkspace>> GetProjectWorkspaceCandidatesAsync(
			[NotNull] ICollection<Table> tables,
			[NotNull] QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient,
			[CanBeNull] IModelFactory modelFactory = null)
		{
			var datastoresByHandle = new Dictionary<long, Datastore>();
			var spatialReferencesByWkId = new Dictionary<long, SpatialReference>();

			GetProjectWorkspacesRequest request =
				await QueuedTask.Run(() =>
				{
					AddWorkspaces(tables, datastoresByHandle);
					AddSpatialReferences(tables.OfType<FeatureClass>(), spatialReferencesByWkId);

					return CreateProjectWorkspacesRequest(tables);
				});

			if (request.ObjectClasses.Count == 0)
			{
				return new List<ProjectWorkspace>(0);
			}

			List<ProjectWorkspace> candidates =
				await LoadProjectWorkspaceAsync(request, datastoresByHandle,
				                                spatialReferencesByWkId,
				                                ddxClient, modelFactory);

			return candidates;
		}

		public static async Task<IList<IQualitySpecificationReference>> LoadSpecificationsRpcAsync(
			[NotNull] IList<int> datasetIds,
			bool includeHiddenSpecifications,
			[NotNull] QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient)
		{
			var request = new GetSpecificationRefsRequest()
			              {
				              IncludeHidden = includeHiddenSpecifications
			              };

			request.DatasetIds.AddRange(datasetIds);

			_msg.DebugFormat("Getting quality specifications for {0} datasets.", datasetIds.Count);

			GetSpecificationRefsResponse response =
				await GrpcClientUtils.TryAsync(async callOptions =>
					                               await ddxClient.GetQualitySpecificationRefsAsync(
						                               request, callOptions),
				                               CancellationToken.None,
				                               _timeoutMilliseconds);

			if (response == null)
			{
				// Cancelled or timed out:
				return new List<IQualitySpecificationReference>(0);
			}

			var result = new List<IQualitySpecificationReference>();

			foreach (QualitySpecificationRefMsg specificationMsg in response.QualitySpecifications)
			{
				var specification =
					new QualitySpecificationReference(specificationMsg.QualitySpecificationId,
					                                  specificationMsg.Name);

				result.Add(specification);
			}

			_msg.DebugFormat("Found {0} quality specifications for {1} datasets.", result.Count,
			                 datasetIds.Count);

			return result.OrderBy(qs => qs.Name).ToList();
		}

		public static async Task<QualitySpecification> LoadFullSpecification(
			int specificationId,
			[NotNull] ISupportedInstanceDescriptors supportedInstanceDescriptors,
			[NotNull] QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient)
		{
			var request = new GetSpecificationRequest()
			              {
				              QualitySpecificationId = specificationId
			              };

			_msg.DebugFormat("Getting quality specification for <id> {0} datasets.",
			                 specificationId);

			GetSpecificationResponse response =
				await GrpcClientUtils.TryAsync(async callOptions =>
					                               await ddxClient.GetQualitySpecificationAsync(
						                               request, callOptions),
				                               CancellationToken.None,
				                               _timeoutMilliseconds);

			if (response == null)
			{
				// Cancelled or timed out:
				return null;
			}

			QualitySpecification result =
				CreateQualitySpecification(response, supportedInstanceDescriptors);

			result.SetCloneId(specificationId);

			return result;
		}

		public static QualitySpecification CreateQualitySpecification(
			[NotNull] GetSpecificationResponse getSpecificationResponse,
			[CanBeNull] ISupportedInstanceDescriptors instanceDescriptors = null)
		{
			IEnumerable<InstanceDescriptorMsg> descriptorsMsg =
				getSpecificationResponse.ReferencedInstanceDescriptors;

			if (instanceDescriptors == null)
			{
				instanceDescriptors = new SupportedInstanceDescriptors(
					new List<TestDescriptor>(),
					new List<TransformerDescriptor>(),
					new List<IssueFilterDescriptor>());
			}

			foreach (InstanceDescriptorMsg descriptorMsg in descriptorsMsg)
			{
				InstanceDescriptor instanceDescriptor = GetInstanceDescriptor(descriptorMsg);
				instanceDescriptors.AddDescriptor(instanceDescriptor);
			}

			Dictionary<int, IDdxDataset> datasetsById =
				FromDatasetMsgs(getSpecificationResponse.ReferencedDatasets);

			var models = new List<DdxModel>();

			foreach (ModelMsg modelMsg in getSpecificationResponse.ReferencedModels)
			{
				DdxModel model =
					CreateDdxModel(modelMsg, (msg) => new BasicModel(msg.ModelId, msg.Name));

				foreach (int datasetId in modelMsg.DatasetIds)
				{
					if (datasetsById.TryGetValue(datasetId, out IDdxDataset dataset) &&
					    ! model.Contains(dataset))
					{
						model.AddDataset((Dataset) dataset);
					}
				}

				models.Add(model);
			}

			IDictionary<string, DdxModel> modelsByWorkspaceId =
				models.ToDictionary(m => m.Id.ToString(CultureInfo.InvariantCulture),
				                    m => (DdxModel) m);

			var factory = new ProtoBasedQualitySpecificationFactory(
				modelsByWorkspaceId, instanceDescriptors);

			QualitySpecification result =
				factory.CreateQualitySpecification(getSpecificationResponse.Specification);

			return result;
		}

		public static DdxModel CreateDdxModel(ModelMsg modelMsg,
		                                      Func<ModelMsg, DdxModel> modelFactory)
		{
			DdxModel model = modelFactory(modelMsg);

			model.SqlCaseSensitivity = (SqlCaseSensitivity) modelMsg.SqlCaseSensitivity;
			model.DefaultDatabaseName = modelMsg.DefaultDatabaseName;
			model.DefaultDatabaseSchemaOwner = modelMsg.DefaultDatabaseSchemaOwner;
			model.ElementNamesAreQualified = modelMsg.ElementNamesAreQualified;

			return model;
		}

		/// <summary>
		/// Creates the ID/datasets dictionary. The datasets have their models assigned but
		/// do not contain any details, such as Attributes or ObjectCategories.
		/// </summary>
		/// <param name="datasetMsgs"></param>
		/// <param name="modelMsgs"></param>
		/// <returns></returns>
		public static Dictionary<int, IDdxDataset> CreateDatasets(
			[NotNull] IEnumerable<DatasetMsg> datasetMsgs,
			[NotNull] IEnumerable<ModelMsg> modelMsgs)
		{
			// TODO: Make sure everyone provides their own model factory, get rid of this method
			Dictionary<int, IDdxDataset> datasetsById = FromDatasetMsgs(datasetMsgs);

			foreach (ModelMsg modelMsg in modelMsgs)
			{
				DdxModel model = new BasicModel(modelMsg.ModelId, modelMsg.Name);

				foreach (int datasetId in modelMsg.DatasetIds)
				{
					if (! datasetsById.TryGetValue(datasetId, out IDdxDataset dataset))
					{
						continue;
					}

					if (modelMsg.ErrorDatasetIds.Contains(datasetId))
					{
						ObjectDataset objectDataset = (ObjectDataset) dataset;
						IErrorDataset errorDataset = CreateErrorDataset(
							datasetId, dataset.GeometryType, dataset.Name, objectDataset.Attributes,
							objectDataset.ObjectTypes);

						datasetsById[datasetId] = (Dataset) errorDataset;
						dataset = errorDataset;
					}

					if (! model.Contains(dataset) && model.Datasets.All(ds => ds.Id != datasetId))
					{
						model.AddDataset((Dataset) dataset);
					}
				}
			}

			return datasetsById;
		}

		/// <summary>
		/// Creates the ID/datasets dictionary. The datasets have their models assigned but
		/// do not contain any details, such as Attributes or ObjectCategories.
		/// </summary>
		/// <param name="datasetMsgs"></param>
		/// <param name="modelMsgs"></param>
		/// <param name="modelFactory"></param>
		/// <returns></returns>
		public static Dictionary<int, IDdxDataset> CreateDatasets(
			[NotNull] IEnumerable<DatasetMsg> datasetMsgs,
			[NotNull] IEnumerable<ModelMsg> modelMsgs,
			[CanBeNull] IModelFactory modelFactory)
		{
			if (modelFactory == null)
			{
				// TODO: Make modelFactory mandatory for everyone, get rid of this method
				return CreateDatasets(datasetMsgs, modelMsgs);
			}

			Dictionary<int, IDdxDataset> datasetsById = FromDatasetMsgs(datasetMsgs, modelFactory);

			foreach (ModelMsg modelMsg in modelMsgs)
			{
				// This method adds the datasets to the model, even if it already exists (TODO: reverse this logic, add to model in CreateDataset)
				DdxModel model = modelFactory.CreateModel(modelMsg);
			}

			return datasetsById;
		}

		public static void AddDatasetsDetailsAsync(
			IList<Dataset> datasets,
			[NotNull] QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient,
			[NotNull] IModelFactory modelFactory)
		{
			// Get the details
			var request = new GetDatasetDetailsRequest();
			request.DatasetIds.AddRange(datasets.Select(d => d.Id));

			_msg.DebugFormat("Getting dataset details for {0} datasets.",
			                 datasets.Count);

			GetDatasetDetailsResponse response =
				GrpcClientUtils.Try(
					callOptions => ddxClient.GetDatasetDetails(request, callOptions),
					CancellationToken.None, _timeoutMilliseconds);

			if (response == null)
			{
				_msg.DebugFormat("The get-dataset-details request failed or timed out.");
				return;
			}

			Dictionary<int, IDdxDataset> datasetsById =
				datasets.ToDictionary(d => d.Id, IDdxDataset (d) => d);

			foreach (DatasetMsg datasetMsg in response.Datasets)
			{
				_msg.DebugFormat("Adding dataset details to {0}", datasetMsg.Name);

				// TODO: Move this to the model factory
				if (! datasetsById.TryGetValue(datasetMsg.DatasetId, out IDdxDataset dataset))
				{
					// Additional dataset referenced by an association end:
					dataset = ProtoDataQualityUtils.FromDatasetMsg(
						datasetMsg, modelFactory.CreateDataset);
					datasetsById.Add(datasetMsg.DatasetId, dataset);

					// NOTE: Multiple models in the same response is not supported! Consider adding ModelId to DatasetMsg
					var model = datasets.Select(d => d.Model).First();
					if (! model.Contains(dataset) && model.Datasets.All(ds => ds.Id != dataset.Id))
					{
						model.AddDataset((Dataset) dataset);
					}
				}

				ObjectDataset originalDataset = dataset as ObjectDataset;

				if (originalDataset == null)
				{
					continue;
				}

				if (originalDataset.Attributes.Count > 0)
				{
					_msg.DebugFormat("Dataset details already loaded for {0}",
					                 originalDataset.Name);
					continue;
				}

				ProtoDataQualityUtils.AddDetailsToDataset(originalDataset, datasetMsg);
			}

			foreach (AssociationMsg associationMsg in response.Associations)
			{
				modelFactory.CreateAssociation(associationMsg);
			}

			_msg.DebugFormat("Added details to {0} datasets.", response.Datasets.Count);
		}

		public static IList<LinearNetwork> CreateLinearNetworks(
			[NotNull] IEnumerable<LinearNetworkMsg> linearNetworkMsgs,
			[NotNull] ICollection<VectorDataset> vectorDatasets)
		{
			var result = new List<LinearNetwork>();
			foreach (LinearNetworkMsg linearNetworkMsg in linearNetworkMsgs)
			{
				result.Add(FromLinearNetworkMsg(linearNetworkMsg, vectorDatasets));
			}

			return result;
		}

		private static LinearNetwork FromLinearNetworkMsg(
			[NotNull] LinearNetworkMsg linearNetworkMsg,
			[NotNull] ICollection<VectorDataset> vectorDatasets)
		{
			var networkDatasets = new List<LinearNetworkDataset>();

			foreach (NetworkDatasetMsg networkDatasetMsg in linearNetworkMsg.NetworkDatasets)
			{
				VectorDataset vectorDataset = vectorDatasets.FirstOrDefault(
					vd => vd.Id == networkDatasetMsg.DatasetId);

				Assert.NotNull(
					$"Vector dataset <id> {networkDatasetMsg.DatasetId} not found in provided datasets");

				var networkDataset = new LinearNetworkDataset(vectorDataset);
				networkDataset.WhereClause = networkDatasetMsg.WhereClause;
				networkDataset.IsDefaultJunction = networkDatasetMsg.IsDefaultJunction;
				networkDataset.Splitting = networkDatasetMsg.IsSplitting;

				networkDatasets.Add(networkDataset);
			}

			var result = new LinearNetwork(linearNetworkMsg.Name,
			                               networkDatasets);

			result.SetCloneId(linearNetworkMsg.LinearNetworkId);

			//result.Description = linearNetworkMsg.Description;
			result.CustomTolerance = linearNetworkMsg.CustomTolerance;
			result.EnforceFlowDirection = linearNetworkMsg.EnforceFlowDirection;

			return result;
		}

		private static InstanceDescriptor GetInstanceDescriptor(
			InstanceDescriptorMsg descriptorMessage)
		{
			{
				InstanceType instanceType = (InstanceType) descriptorMessage.Type;

				InstanceDescriptor result;

				switch (instanceType)
				{
					case InstanceType.Test:
						result = ProtoDataQualityUtils.FromInstanceDescriptorMsg<TestDescriptor>(
							descriptorMessage);
						break;
					case InstanceType.Transformer:
						result = ProtoDataQualityUtils
							.FromInstanceDescriptorMsg<TransformerDescriptor>(
								descriptorMessage);
						break;
					case InstanceType.IssueFilter:
						result = ProtoDataQualityUtils
							.FromInstanceDescriptorMsg<IssueFilterDescriptor>(
								descriptorMessage);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				return result;
			}
		}

		private static IEnumerable<IssueFilterDescriptor> GetIssueFilterDescriptors(
			IEnumerable<InstanceDescriptorMsg> descriptorMessages)
		{
			foreach (InstanceDescriptorMsg instanceDescriptorMsg in descriptorMessages)
			{
				if (instanceDescriptorMsg.Type != (int) InstanceType.IssueFilter)
				{
					continue;
				}

				IssueFilterDescriptor issueFilterDescriptor =
					ProtoDataQualityUtils.FromInstanceDescriptorMsg<IssueFilterDescriptor>(
						instanceDescriptorMsg);

				yield return issueFilterDescriptor;
			}
		}

		private static IEnumerable<TransformerDescriptor> GetTransformerDescriptors(
			IEnumerable<InstanceDescriptorMsg> descriptorMessages)
		{
			foreach (InstanceDescriptorMsg instanceDescriptorMsg in descriptorMessages)
			{
				if (instanceDescriptorMsg.Type != (int) InstanceType.Transformer)
				{
					continue;
				}

				TransformerDescriptor transformerDescriptor =
					ProtoDataQualityUtils.FromInstanceDescriptorMsg<TransformerDescriptor>(
						instanceDescriptorMsg);

				yield return transformerDescriptor;
			}
		}

		private static IEnumerable<TestDescriptor> GetTestDescriptors(
			IEnumerable<InstanceDescriptorMsg> descriptorMessages)
		{
			foreach (InstanceDescriptorMsg instanceDescriptorMsg in descriptorMessages)
			{
				if (instanceDescriptorMsg.Type != (int) InstanceType.Test)
				{
					continue;
				}

				TestDescriptor testDescriptor =
					ProtoDataQualityUtils.FromInstanceDescriptorMsg<TestDescriptor>(
						instanceDescriptorMsg);

				yield return testDescriptor;
			}
		}

		private static GetProjectWorkspacesRequest CreateProjectWorkspacesRequest(
			[NotNull] IEnumerable<Table> objectClasses)
		{
			var request = new GetProjectWorkspacesRequest();

			List<WorkspaceMsg> workspaceMessages = new List<WorkspaceMsg>();

			foreach (Table table in objectClasses)
			{
				if (table.GetDatastore() is not Geodatabase)
				{
					continue;
				}

				ObjectClassMsg objectClassMsg = ProtobufConversionUtils.ToObjectClassMsg(
					table, Convert.ToInt32(table.GetID()));

				request.ObjectClasses.Add(objectClassMsg);

				WorkspaceMsg workspaceMsg =
					workspaceMessages.FirstOrDefault(
						wm => wm.WorkspaceHandle == objectClassMsg.WorkspaceHandle);

				if (workspaceMsg == null)
				{
					using (Datastore datastore = table.GetDatastore())
					{
						workspaceMsg =
							ProtobufConversionUtils.ToWorkspaceRefMsg(datastore, true);

						workspaceMessages.Add(workspaceMsg);
					}
				}
			}

			request.Workspaces.AddRange(workspaceMessages);

			return request;
		}

		private static async Task<List<ProjectWorkspace>> LoadProjectWorkspaceAsync(
			[NotNull] GetProjectWorkspacesRequest request,
			[NotNull] Dictionary<long, Datastore> datastores,
			Dictionary<long, SpatialReference> spatialReferencesByWkId,
			QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient,
			[CanBeNull] IModelFactory modelFactory = null)
		{
			GetProjectWorkspacesResponse response =
				await GrpcClientUtils.TryAsync(async callOptions =>
					                               await ddxClient.GetProjectWorkspacesAsync(
						                               request, callOptions),
				                               CancellationToken.None,
				                               _timeoutMilliseconds);

			if (response == null)
			{
				// Cancelled or timed out:
				return new List<ProjectWorkspace>(0);
			}

			RepeatedField<DatasetMsg> datasetMsgs = response.Datasets;
			RepeatedField<ModelMsg> modelMsgs = response.Models;

			Dictionary<int, IDdxDataset> datasetsById =
				CreateDatasets(datasetMsgs, modelMsgs, modelFactory);

			List<ProjectWorkspace> candidates = null;

			await QueuedTask.Run(() =>
			{
				candidates =
					GetProjectWorkspacesQueued(response,
					                           datastores, spatialReferencesByWkId, datasetsById);
			});

			return candidates;
		}

		public static IErrorDataset CreateErrorDataset(int datasetId,
		                                               GeometryType geometryType,
		                                               string name,
		                                               IList<ObjectAttribute> attributes,
		                                               IList<ObjectType> objectTypes)
		{
			GeometryTypeShape shapeType = geometryType as GeometryTypeShape;

			ProSuiteGeometryType proSuiteGeometryType =
				shapeType?.ShapeType ?? ProSuiteGeometryType.Null;

			ObjectDataset result = ProtoDataQualityUtils.CreateErrorDataset(
				datasetId, name, proSuiteGeometryType);

			result.GeometryType = geometryType;

			foreach (ObjectAttribute attribute in attributes)
			{
				result.AddAttribute(attribute);
			}

			int index = 0;
			foreach (ObjectType objectType in objectTypes)
			{
				result.AddObjectType(objectType.SubtypeCode, objectType.Name, index++);
			}

			return (IErrorDataset) result;
		}

		/// <summary>
		/// Must be called inside a queued task to create the datastore's display name and in case
		/// the spatial reference is not a WKID.
		/// </summary>
		/// <param name="response"></param>
		/// <param name="datastores"></param>
		/// <param name="spatialReferencesByWkId"></param>
		/// <param name="datasetsById"></param>
		/// <returns></returns>
		private static List<ProjectWorkspace> GetProjectWorkspacesQueued(
			[NotNull] GetProjectWorkspacesResponse response,
			[NotNull] IReadOnlyDictionary<long, Datastore> datastores,
			[NotNull] IReadOnlyDictionary<long, SpatialReference> spatialReferencesByWkId,
			[NotNull] IReadOnlyDictionary<int, IDdxDataset> datasetsById)
		{
			List<ProjectWorkspace> result = new List<ProjectWorkspace>();

			foreach (ProjectWorkspaceMsg projectWorkspaceMsg in response.ProjectWorkspaces)
			{
				Datastore datastore = datastores[projectWorkspaceMsg.WorkspaceHandle];

				ProjectMsg projectMsg =
					response.Projects.First(p => p.ProjectId == projectWorkspaceMsg.ProjectId);

				ModelMsg modelMsg = response.Models.First(m => m.ModelId == projectMsg.ModelId);

				SpatialReference sr = GetSpatialReference(spatialReferencesByWkId, modelMsg);

				List<IDdxDataset> datasets =
					projectWorkspaceMsg.DatasetIds
					                   .Select(datasetId => datasetsById[datasetId])
					                   .ToList();

				var projectWorkspace = new ProjectWorkspace(projectWorkspaceMsg.ProjectId,
				                                            projectMsg.Name,
				                                            datasets, datastore, sr)
				                       {
					                       IsMasterDatabaseWorkspace =
						                       projectWorkspaceMsg.IsMasterDatabaseWorkspace
				                       };

				result.Add(projectWorkspace);
			}

			return result;
		}

		private static Dictionary<int, IDdxDataset> FromDatasetMsgs(
			[NotNull] IEnumerable<DatasetMsg> datasetMsgs)
		{
			var datasetsById = new Dictionary<int, IDdxDataset>();

			foreach (DatasetMsg datasetMsg in datasetMsgs)
			{
				if (datasetsById.ContainsKey(datasetMsg.DatasetId))
				{
					continue;
				}

				Func<DatasetMsg, Dataset> factoryMethod =
					msg => new BasicDataset(msg.DatasetId, msg.Name);

				Dataset dataset = ProtoDataQualityUtils.FromDatasetMsg(
					datasetMsg, factoryMethod);

				datasetsById.Add(dataset.Id, dataset);
			}

			return datasetsById;
		}

		private static Dictionary<int, IDdxDataset> FromDatasetMsgs(
			[NotNull] IEnumerable<DatasetMsg> datasetMsgs,
			[NotNull] IModelFactory modelFactory)
		{
			var datasetsById = new Dictionary<int, IDdxDataset>();

			foreach (DatasetMsg datasetMsg in datasetMsgs)
			{
				if (datasetsById.ContainsKey(datasetMsg.DatasetId))
				{
					continue;
				}

				Dataset dataset = modelFactory.CreateDataset(datasetMsg);

				datasetsById.Add(dataset.Id, dataset);
			}

			return datasetsById;
		}

		private static IEnumerable<Association> FromAssociationMsgs(
			[NotNull] IEnumerable<AssociationMsg> associationMsgs,
			[NotNull] IDictionary<int, IDdxDataset> datasetsById)
		{
			foreach (AssociationMsg associationMsg in associationMsgs)
			{
				Association association = ProtoDataQualityUtils.FromAssociationMsg(
					associationMsg, datasetsById);

				yield return association;
			}
		}

		private static SpatialReference GetSpatialReference(
			IReadOnlyDictionary<long, SpatialReference> spatialReferencesByWkId,
			ModelMsg modelMsg)
		{
			if (modelMsg == null || modelMsg.SpatialReference == null)
			{
				_msg.Debug("No model provided or model has no spatial reference.");
				return null;
			}

			SpatialReferenceMsg srMsg = modelMsg.SpatialReference;

			SpatialReference result = null;

			if (srMsg.FormatCase == SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid)
			{
				if (spatialReferencesByWkId.TryGetValue(srMsg.SpatialReferenceWkid, out result))
				{
					return result;
				}
			}

			result = ProtobufConversionUtils.FromSpatialReferenceMsg(srMsg);

			return result;
		}

		private static void AddWorkspaces(
			[NotNull] IEnumerable<Table> tables,
			[NotNull] IDictionary<long, Datastore> datastoresByHandle)
		{
			// NOTE: 'This object has been previously disposed and cannot be manipulated'
			//       happens sometimes and the problem in these cases are, that we cannot even
			//       access the table name.
			foreach (Table table in tables)
			{
				try
				{
					Datastore datastore = table?.GetDatastore();

					_msg.DebugFormat("Successfully extracted datastore from table {0}",
					                 table?.GetName());

					if (datastore != null)
					{
						var handle = datastore.Handle.ToInt64();
						datastoresByHandle.TryAdd(handle, datastore);
					}
				}
				catch (Exception e)
				{
					_msg.Warn("Error getting workspace from table", e);
				}
			}

			//foreach (Datastore datastore in tables.Select(table => table?.GetDatastore())
			//                                      .Where(datastore => datastore != null))
			//{
			//	var handle = datastore.Handle.ToInt64();

			//	if (! datastoresByHandle.ContainsKey(handle))
			//	{
			//		datastoresByHandle.Add(handle, datastore);
			//	}
			//}
		}

		private static void AddSpatialReferences(
			[NotNull] IEnumerable<FeatureClass> featureClasses,
			[NotNull] IDictionary<long, SpatialReference> spatialReferencesByWkId)
		{
			foreach (SpatialReference sr in featureClasses.Select(fc => fc?.GetSpatialReference())
			                                              .Where(sr => sr != null))
			{
				if (! spatialReferencesByWkId.ContainsKey(sr.Wkid))
				{
					spatialReferencesByWkId.Add(sr.Wkid, sr);
				}
			}
		}
	}
}
