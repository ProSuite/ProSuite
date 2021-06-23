using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.QA
{
	public static class DdxUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static async Task<List<ProjectWorkspace>> GetProjectWorkspaceCandidates(
			[NotNull] ICollection<Table> objectClasses,
			[NotNull] QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient)
		{
			var datastoresByHandle = new Dictionary<long, Datastore>();
			var spatialReferencesByWkId = new Dictionary<long, SpatialReference>();

			GetProjectWorkspacesRequest request =
				await QueuedTask.Run(() =>
				{
					AddWorkspaces(objectClasses, datastoresByHandle);
					AddSpatialReferences(objectClasses, spatialReferencesByWkId);

					return CreateProjectWorkspacesRequest(objectClasses);
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
			GetSpecificationsRequest request = new GetSpecificationsRequest()
			                                   {
				                                   IncludeHidden = includeHiddenSpecifications
			                                   };

			request.DatasetIds.AddRange(datasetIds);

			DateTime timeout = GetTimeout();

			GetSpecificationsResponse response =
				await ddxClient.GetQualitySpecificationsAsync(request, null, timeout);

			var result = new List<IQualitySpecificationReference>();

			foreach (QualitySpecificationRefMsg specificationMsg in response.QualitySpecifications)
			{
				var specification =
					new QualitySpecificationReference(specificationMsg.QualitySpecificationId,
					                                  specificationMsg.Name);

				result.Add(specification);
			}

			return result;
		}

		private static GetProjectWorkspacesRequest CreateProjectWorkspacesRequest(
			[NotNull] IEnumerable<Table> objectClasses)
		{
			var request = new GetProjectWorkspacesRequest();

			List<WorkspaceMsg> workspaceMessages = new List<WorkspaceMsg>();

			foreach (Table table in objectClasses)
			{
				ObjectClassMsg objectClassMsg = ProtobufConversionUtils.ToObjectClassMsg(table);

				request.ObjectClasses.Add(objectClassMsg);

				WorkspaceMsg workspaceMsg =
					workspaceMessages.FirstOrDefault(
						wm => wm.WorkspaceHandle == objectClassMsg.WorkspaceHandle);

				if (workspaceMsg == null)
				{
					using (Datastore datastore = table.GetDatastore())
					{
						workspaceMsg = ProtobufConversionUtils.ToWorkspaceRefMsg(datastore);

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
			DateTime timeout = GetTimeout();

			GetProjectWorkspacesResponse response =
				await ddxClient.GetProjectWorkspacesAsync(request, null, timeout);

			var candidates = new List<ProjectWorkspace>();
			foreach (ProjectWorkspaceMsg projectWorkspaceMsg in response.ProjectWorkspaces)
			{
				Datastore datastore = datastores[projectWorkspaceMsg.WorkspaceHandle];

				ProjectMsg projectMsg =
					response.Projects.First(p => p.ProjectId == projectWorkspaceMsg.ProjectId);

				ModelMsg modelMsg = response.Models.First(m => m.ModelId == projectMsg.ModelId);

				SpatialReference sr = GetSpatialReference(spatialReferencesByWkId, modelMsg);

				candidates.Add(
					new ProjectWorkspace(projectWorkspaceMsg.ProjectId,
					                     projectWorkspaceMsg.DatasetIds.ToList(), datastore, sr));
			}

			return candidates;
		}

		private static SpatialReference GetSpatialReference(
			Dictionary<long, SpatialReference> spatialReferencesByWkId,
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

			QueuedTask.Run(() =>
			{
				result = ProtobufConversionUtils.FromSpatialReferenceMsg(srMsg);
			});

			return result;
		}

		private static void AddWorkspaces([NotNull] IEnumerable<Table> objectClasses,
		                                  [NotNull] Dictionary<long, Datastore> datastoresByHandle)
		{
			foreach (Table objectClass in objectClasses)
			{
				Datastore datastore = objectClass.GetDatastore();

				long handle = datastore.Handle.ToInt64();

				if (! datastoresByHandle.ContainsKey(handle))
				{
					datastoresByHandle.Add(handle, datastore);
				}
			}
		}

		private static void AddSpatialReferences(ICollection<Table> objectClasses,
		                                         Dictionary<long, SpatialReference>
			                                         spatialReferencesByWkId)
		{
			foreach (Table objectClass in objectClasses)
			{
				if (objectClass is FeatureClass featureClass)
				{
					SpatialReference sr = DatasetUtils.GetSpatialReference(featureClass);

					if (! spatialReferencesByWkId.ContainsKey(sr.Wkid))
					{
						spatialReferencesByWkId.Add(sr.Wkid, sr);
					}
				}
			}
		}

		private static DateTime GetTimeout()
		{
			// Better late than never (if the service has just been started, all the workspaces need to be opened...)
			DateTime timeout = DateTime.Now.AddSeconds(60).ToUniversalTime();

			return timeout;
		}
	}
}
