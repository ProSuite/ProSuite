using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.QA
{
	public static class DdxUtils
	{
		public static async Task<List<ProjectWorkspace>> GetProjectWorkspaceCandidates(
			[NotNull] ICollection<Table> objectClasses,
			[NotNull] QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient)
		{
			var datastoresByHandle = new Dictionary<long, Datastore>();

			GetProjectWorkspacesRequest request =
				await QueuedTask.Run(() =>
				{
					AddWorkspaces(objectClasses, datastoresByHandle);

					return CreateProjectWorkspacesRequest(objectClasses);
				});

			if (request.ObjectClasses.Count == 0)
			{
				return new List<ProjectWorkspace>(0);
			}

			List<ProjectWorkspace> candidates =
				await LoadProjectWorkspaceAsync(request, datastoresByHandle,
				                                ddxClient);

			return candidates;
		}

		public static async Task<IList<QualitySpecificationReference>> LoadSpecificationsRpcAsync(
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

			var result = new List<QualitySpecificationReference>();

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
			QualityVerificationDdxGrpc.QualityVerificationDdxGrpcClient ddxClient)
		{
			DateTime timeout = GetTimeout();

			GetProjectWorkspacesResponse response =
				await ddxClient.GetProjectWorkspacesAsync(request, null, timeout);

			var candidates = new List<ProjectWorkspace>();
			foreach (ProjectWorkspaceMsg projectWorkspaceMsg in response.ProjectWorkspaces)
			{
				Datastore datastore = datastores[projectWorkspaceMsg.WorkspaceHandle];

				candidates.Add(
					new ProjectWorkspace(projectWorkspaceMsg.ProjectId,
					                     projectWorkspaceMsg.DatasetIds.ToList(), datastore));
			}

			return candidates;
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

		private static DateTime GetTimeout()
		{
			DateTime timeout = DateTime.Now.AddSeconds(20).ToUniversalTime();

			return timeout;
		}
	}
}
