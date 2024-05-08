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
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.DataModel;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Ddx;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using Dataset = ProSuite.DomainModel.Core.DataModel.Dataset;
using DatasetType = ProSuite.Commons.GeoDb.DatasetType;
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
			[NotNull] QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient)
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
				                                ddxClient);

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

			var models = new List<BasicModel>();

			foreach (ModelMsg modelMsg in getSpecificationResponse.ReferencedModels)
			{
				BasicModel model = new BasicModel(modelMsg.ModelId, modelMsg.Name);

				foreach (int datasetId in modelMsg.DatasetIds)
				{
					if (datasetsById.TryGetValue(datasetId, out IDdxDataset dataset))
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

		public static void AddDatasetsDetailsAsync(
			IList<Dataset> datasets,
			[NotNull] QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient)
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

			foreach (DatasetMsg errorDatasetMsg in response.Datasets)
			{
				ObjectDataset originalDataset = (ObjectDataset)
					datasets.First(e => e.Id == errorDatasetMsg.DatasetId);

				ProtoDataQualityUtils.AddDetailsToDataset(originalDataset, errorDatasetMsg);
			}

			_msg.DebugFormat("Added details to {0} datasets.", response.Datasets.Count);
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
			QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient)
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

			Dictionary<int, IDdxDataset> datasetsById = FromDatasetMsgs(datasetMsgs);

			var models = new List<BasicModel>();

			foreach (ModelMsg modelMsg in response.Models)
			{
				BasicModel model = new BasicModel(modelMsg.ModelId, modelMsg.Name);

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

					model.AddDataset((Dataset) dataset);
				}

				models.Add(model);
			}

			List<ProjectWorkspace> candidates = null;

			await QueuedTask.Run(() =>
			{
				candidates =
					GetProjectWorkspacesQueued(response,
					                           datastores, spatialReferencesByWkId, datasetsById);
			});

			return candidates;
		}

		private static IErrorDataset CreateErrorDataset(int datasetId,
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

				List<IDdxDataset> datasets = projectWorkspaceMsg.DatasetIds
				                                                .Select(datasetId =>
					                                                datasetsById[datasetId])
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

			Dataset dataset;
			foreach (DatasetMsg datasetMsg in datasetMsgs)
			{
				var datasetType = (DatasetType) datasetMsg.DatasetType;

				// TODO: Proper type! -> Requires factory
				//switch (datasetType)
				//{
				//	case DatasetType.Null:
				//		break;
				//	case DatasetType.Any:
				//		break;
				//	case DatasetType.Table:
				//		break;
				//	case DatasetType.FeatureClass:
				//		new ModelVectorDataset(datasetMsg.DatasetId, datasetMsg.Name, null,
				//									                       datasetMsg.AliasName);
				//		break;
				//	case DatasetType.Topology:
				//		break;
				//	case DatasetType.Raster:
				//		break;
				//	case DatasetType.RasterMosaic:
				//		break;
				//	case DatasetType.Terrain:
				//		break;
				//	default:
				//		throw new ArgumentOutOfRangeException();
				//}

				dataset = ProtoDataQualityUtils.FromDatasetMsg(
					datasetMsg, (id, name) => new BasicDataset(id, name));

				if (! datasetsById.ContainsKey(dataset.Id))
				{
					datasetsById.Add(dataset.Id, dataset);
				}
			}

			return datasetsById;
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
			foreach (Datastore datastore in tables.Select(table => table?.GetDatastore())
			                                      .Where(datastore => datastore != null))
			{
				var handle = datastore.Handle.ToInt64();

				if (! datastoresByHandle.ContainsKey(handle))
				{
					datastoresByHandle.Add(handle, datastore);
				}
			}
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
