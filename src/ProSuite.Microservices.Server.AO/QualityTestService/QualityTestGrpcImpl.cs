using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using Grpc.Core;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.Microservices.Definitions.QA.Test;
using ProSuite.Microservices.Definitions.Shared.Commons;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.QualityTestService
{
	public abstract class QualityTestGrpcImpl : QualityTestGrpc.QualityTestGrpcBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly StaTaskScheduler _staTaskScheduler;

		protected QualityTestGrpcImpl()
		{
			_staTaskScheduler = new StaTaskScheduler(1);
		}

		public override async Task Execute(ExecuteTestRequest request,
		                                   IServerStreamWriter<ExecuteTestResponse> responseStream,
		                                   ServerCallContext context)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			ProcessUtils.TrySetThreadIdAsName();

			Func<ITrackCancel, ServiceCallStatus> func =
				trackCancel => ExecuteTest(request, responseStream, trackCancel);

			ServiceCallStatus response =
				await GrpcServerUtils.ExecuteServiceCall(func, context, _staTaskScheduler, true);

			_msg.DebugStopTiming(watch, "Executed quality test '{0}' for peer {1}. Response: {2}",
			                     request.TestName, context.Peer, response);
		}

		private ServiceCallStatus ExecuteTest(
			[NotNull] ExecuteTestRequest request,
			[NotNull] IServerStreamWriter<ExecuteTestResponse> responseStream,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			ConcurrentBag<DetectedIssueMsg> issueCollection = new ConcurrentBag<DetectedIssueMsg>();

			foreach (ExecuteTestResponse response in ExecuteTestCore(
				         request, issueCollection, trackCancel))
			{
				TrySendIssues(responseStream, response, issueCollection);
			}

			var callStatus = trackCancel == null || trackCancel.Continue()
				                 ? ServiceCallStatus.Finished
				                 : ServiceCallStatus.Cancelled;

			// Any remaining issues (TrySend might fail):
			var lastResponse = new ExecuteTestResponse { ServiceCallStatus = (int) callStatus };

			TrySendIssues(responseStream, lastResponse, issueCollection, true);

			return callStatus;
		}

		protected abstract IEnumerable<ExecuteTestResponse> ExecuteTestCore(
			[NotNull] ExecuteTestRequest request,
			[NotNull] ConcurrentBag<DetectedIssueMsg> issueCollection,
			[CanBeNull] ITrackCancel trackCancel);

		private static void TrySendIssues(
			[NotNull] IServerStreamWriter<ExecuteTestResponse> responseStream,
			ExecuteTestResponse response,
			[NotNull] ConcurrentBag<DetectedIssueMsg> issues,
			bool force = false)
		{
			if (issues.Count == 0)
			{
				return;
			}

			while (issues.TryTake(out DetectedIssueMsg issue))
			{
				response.Issues.Add(issue);
			}

			//_msg.DebugFormat("Sending {0} errors back to client...", issues.Count);

			if (force)
			{
				MessagingUtils.SendResponse(responseStream, response);
			}
			else
			{
				bool success = MessagingUtils.TrySendResponse(responseStream, response);

				if (! success)
				{
					// The issues would be lost, so put them back into the collection
					foreach (DetectedIssueMsg issue in response.Issues)
					{
						issues.Add(issue);
					}
				}
			}
		}

		protected IDictionary<ITable, TestDatasetMsg> FromTestDatasetMsgs(
			ExecuteTestRequest request)
		{
			IDictionary<long, IWorkspace> workspaces = new Dictionary<long, IWorkspace>();

			foreach (WorkspaceMsg workspaceMsg in request.Workspaces)
			{
				var dictionary = ToDictionary(workspaceMsg.ConnectionProperties);

				IPropertySet propertySet = PropertySetUtils.GetPropertySet(dictionary);

				IWorkspaceFactory workspaceFactory =
					GetWorkspaceFactory((WorkspaceDbType) workspaceMsg.WorkspaceDbType);

				IWorkspace workspace =
					WorkspaceUtils.OpenWorkspace(workspaceFactory, propertySet, 0);

				workspaces.Add(workspaceMsg.WorkspaceHandle, workspace);
			}

			var result = new Dictionary<ITable, TestDatasetMsg>();

			foreach (TestDatasetMsg involvedDatasetMsg in request.InvolvedTables)
			{
				long workspaceIndex = involvedDatasetMsg.ClassDefinition.WorkspaceHandle;

				IFeatureWorkspace featureWorkspace = (IFeatureWorkspace) workspaces[workspaceIndex];

				ITable table = featureWorkspace.OpenTable(involvedDatasetMsg.ClassDefinition.Name);

				result.Add(table, involvedDatasetMsg);
			}

			return result;
		}

		private static IWorkspaceFactory GetWorkspaceFactory(WorkspaceDbType workspaceType)
		{
			if (workspaceType == WorkspaceDbType.FileGeodatabase)
			{
				return WorkspaceUtils.GetFileGdbWorkspaceFactory();
			}

			if (workspaceType == WorkspaceDbType.MobileGeodatabase)
			{
				return WorkspaceUtils.GetSqliteWorkspaceFactory();
			}

			if (workspaceType == WorkspaceDbType.FileSystem)
			{
				return WorkspaceUtils.GetShapefileWorkspaceFactory();
			}

			if (workspaceType == WorkspaceDbType.PersonalGeodatabase)
			{
				return WorkspaceUtils.GetAccessWorkspaceFactory();
			}

			var esriWorkspaceType = WorkspaceUtils.ToEsriWorkspaceType(workspaceType);

			if (esriWorkspaceType == esriWorkspaceType.esriRemoteDatabaseWorkspace)
			{
				return WorkspaceUtils.GetSdeWorkspaceFactory();
			}

			throw new ArgumentOutOfRangeException(nameof(workspaceType));
		}

		private IDictionary<string, object> ToDictionary(
			IEnumerable<KeyValuePairMsg> keyValuePairsMsg)
		{
			var dictionary = new Dictionary<string, object>();

			foreach (KeyValuePairMsg kvp in keyValuePairsMsg)
			{
				dictionary.Add(kvp.Key, kvp.Value);
			}

			const string passwordKeyWord = "PASSWORD";
			const string instanceKeyWord = "INSTANCE";
			const string userKeyWord = "USER";

			if (dictionary.ContainsKey(passwordKeyWord))
			{
				dictionary.TryGetValue(instanceKeyWord, out object instance);
				dictionary.TryGetValue(userKeyWord, out object user);

				string password = GetPassword((string) instance, (string) user);

				dictionary[passwordKeyWord] = password;
			}

			return dictionary;
		}

		protected virtual string GetPassword(string instance, string user)
		{
			throw new NotImplementedException("GetPassword is not implemented");
		}
	}
}
