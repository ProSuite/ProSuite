using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Google.Protobuf.Collections;
using Grpc.Core;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Client.QualityTestService;
using ProSuite.Microservices.Definitions.QA.Test;
using ProSuite.Microservices.Definitions.Shared;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests
{
	/// <summary>
	/// Test implementation for external service. At some point the features (current tile) could be streamed.
	/// Currently the workspace has to be accessible (and probably known) by the external service.
	///  TODO:
	/// - Naming?
	/// - Cancelling? -> generally not implemented
	/// 
	/// - Timeout? server error -> throw exception -> special error type 
	/// 
	/// </summary>
	public class QaExternalService : NonContainerTest
	{
		private readonly IList<ITable> _tables;
		private readonly string _parameters;

		private readonly ExternalTestClient _externalService;

		private ConcurrentBag<DetectedIssueMsg> _foundIssues;

		public QaExternalService([NotNull] IList<ITable> tables,
		                         string connectionUrl,
		                         string parameters) : base(tables)
		{
			_tables = tables;
			_parameters = parameters;

			_externalService = new ExternalTestClient(connectionUrl);
		}

		public override int Execute()
		{
			return RunTest(_externalService.TestClient, null);
		}

		public override int Execute(IEnvelope boundingBox)
		{
			return RunTest(_externalService.TestClient, boundingBox);
		}

		public override int Execute(IPolygon area)
		{
			return RunTest(_externalService.TestClient, area);
		}

		public override int Execute(IEnumerable<IRow> selectedRows)
		{
			// TODO, if this is ever called
			throw new NotImplementedException();
		}

		public override int Execute(IRow row)
		{
			// TODO, if this is ever called
			throw new NotImplementedException();
		}

		protected override ISpatialReference GetSpatialReference()
		{
			var geoDataset = _tables.FirstOrDefault() as IGeoDataset;

			return geoDataset?.SpatialReference;
		}

		private int RunTest(
			QualityTestGrpc.QualityTestGrpcClient client,
			IGeometry aoi)
		{
			// TODO: Get from service?
			CancellationTokenSource cancellationSource = new CancellationTokenSource();

			ExecuteTestRequest request = CreateRequest(aoi, _parameters);

			_foundIssues = new ConcurrentBag<DetectedIssueMsg>();

			Task<int> task = Task.Run(
				async () =>
					await ExecuteAndProcessMessagesAsync(request, client, cancellationSource),
				cancellationSource.Token);

			while (! task.IsCompleted)
			{
				Thread.Sleep(100);
			}

			int errorCount = ProcessResponse(_foundIssues);

			Assert.AreEqual(errorCount, task.Result,
			                "Number of issues reported is not equal number of issues saved");

			return errorCount;
		}

		private ExecuteTestRequest CreateRequest(IGeometry aoi, string parameters)
		{
			List<IWorkspace> workspaces = new List<IWorkspace>();

			var testDatasets = new List<TestDatasetMsg>();

			foreach (ITable table in _tables)
			{
				IWorkspace tableWorkspace = DatasetUtils.GetWorkspace(table);

				int wsIndex = workspaces.FindIndex(w => w == tableWorkspace);

				if (wsIndex < 0)
				{
					workspaces.Add(tableWorkspace);
					wsIndex = workspaces.Count - 1;
				}

				TestDatasetMsg testDatasetMsg = ToInvolvedTable(table, wsIndex);

				testDatasets.Add(testDatasetMsg);
			}

			List<WorkspaceMsg> workspaceMsgs = workspaces.Select(ToWorkspaceMsg).ToList();

			ShapeMsg aoiMsg = ProtobufGeometryUtils.ToShapeMsg(aoi, ShapeMsg.FormatOneofCase.Wkb);

			var request = new ExecuteTestRequest()
			              {
				              Perimeter = aoiMsg,
			              };

			request.Workspaces.AddRange(workspaceMsgs);

			request.InvolvedTables.AddRange(testDatasets);

			return request;
		}

		private TestDatasetMsg ToInvolvedTable(ITable table, int workspaceIndex)
		{
			var testDataset = new TestDatasetMsg();

			IObjectClass objectClass = (IObjectClass) table;

			ObjectClassMsg classMsg = ProtobufGdbUtils.ToObjectClassMsg(objectClass, true);
			classMsg.WorkspaceHandle = workspaceIndex;

			testDataset.ClassDefinition = classMsg;

			string constraint = GetConstraint(table);

			CallbackUtils.DoWithNonNull(constraint, s => testDataset.FilterExpression = s);

			return testDataset;
		}

		private static WorkspaceMsg ToWorkspaceMsg(IWorkspace workspace, int handleId)
		{
			WorkspaceMsg.Types.WorkspaceType workspaceType = GetWorkspaceType(workspace);

			var result = new WorkspaceMsg
			             {
				             WorkspaceHandle = handleId,
				             WorkspaceType = workspaceType
			             };

			IPropertySet propSet = workspace.ConnectionProperties;

			IDictionary<string, object> dictionary = PropertySetUtils.GetDictionary(propSet);

			foreach (KeyValuePair<string, object> keyValuePair in dictionary)
			{
				string value;

				if (keyValuePair.Key == "PASSWORD")
				{
					// It's an encrypted byte array - the receiver must get it from elsewhere
					value = string.Empty;
				}
				else
				{
					value = keyValuePair.Value?.ToString() ?? string.Empty;
				}

				KeyValuePairMsg kvp = new KeyValuePairMsg
				                      {
					                      Key = keyValuePair.Key,
					                      Value = value
				};

				result.ConnectionProperties.Add(kvp);
			}

			return result;
		}

		private static WorkspaceMsg.Types.WorkspaceType GetWorkspaceType(IWorkspace workspace)
		{
			if (WorkspaceUtils.IsFileGeodatabase(workspace))
			{
				return WorkspaceMsg.Types.WorkspaceType.FileGeodatabase;
			}

			if (WorkspaceUtils.IsSDEGeodatabase(workspace))
			{
				return WorkspaceMsg.Types.WorkspaceType.SdeGeodatabase;
			}

			if (WorkspaceUtils.IsShapefileWorkspace(workspace))
			{
				return WorkspaceMsg.Types.WorkspaceType.ShapefileWorkspace;
			}

			if (WorkspaceUtils.IsPersonalGeodatabase(workspace))
			{
				return WorkspaceMsg.Types.WorkspaceType.PersonalGeodatabase;
			}

			return WorkspaceMsg.Types.WorkspaceType.Unknown;
		}

		private async Task<int> ExecuteAndProcessMessagesAsync(
			[NotNull] ExecuteTestRequest request,
			[NotNull] QualityTestGrpc.QualityTestGrpcClient client,
			[NotNull] CancellationTokenSource cancellationTokenSource)
		{
			AsyncServerStreamingCall<ExecuteTestResponse> call = client.Execute(request);

			while (await call.ResponseStream.MoveNext(cancellationTokenSource.Token))
			{
				ExecuteTestResponse responseMsg = call.ResponseStream.Current;

				foreach (DetectedIssueMsg issueMsg in responseMsg.Issues)
				{
					_foundIssues.Add(issueMsg);
				}
			}

			return _foundIssues.Count;
		}

		private int ProcessResponse(IEnumerable<DetectedIssueMsg> issues)
		{
			ISpatialReference spatialReference = GetSpatialReference();

			int errorCount = 0;

			foreach (DetectedIssueMsg issueMsg in issues)
			{
				IGeometry issueGeometry =
					issueMsg.IssueGeometry == null
						? null
						: ProtobufGeometryUtils.FromShapeMsg(
							issueMsg.IssueGeometry, spatialReference);

				IssueCode issueCode =
					new IssueCode(issueMsg.IssueCodeId, issueMsg.IssueCodeDescription);

				IList<InvolvedRow> involvedRows = GetInvolvedRows(issueMsg.InvolvedObjects);

				errorCount += ReportError(issueMsg.Description, issueGeometry, issueCode,
				                          issueMsg.AffectedComponent, involvedRows);
			}

			// TODO: Make sure the obsolete exceptions are still handled in the domain verification service
			//foreach (GdbObjRefMsg objRefMsg in responseMsg.ObsoleteExceptions)
			//{
			//	ResultIssueCollector?.AddObsoleteException(objRefMsg);
			//}

			// TODO: Progress

			//LogProgress(responseMsg.Progress);
			return errorCount;
		}

		private static IList<InvolvedRow> GetInvolvedRows(
			RepeatedField<InvolvedObjectsMsg> involvedObjectsMsg)
		{
			var result = new List<InvolvedRow>();

			foreach (InvolvedObjectsMsg involvedTableMsg in involvedObjectsMsg)
			{
				foreach (int objectId in involvedTableMsg.ObjectIds)
				{
					result.Add(new InvolvedRow(involvedTableMsg.Dataset.Name, objectId));
				}
			}

			return result;
		}
	}
}
