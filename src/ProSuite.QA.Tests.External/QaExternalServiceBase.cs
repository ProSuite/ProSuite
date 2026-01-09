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
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Client.GrpcCore.QualityTestService;
using ProSuite.Microservices.Definitions.QA.Test;
using ProSuite.Microservices.Definitions.Shared.Commons;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.QA.Container;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Tests.External
{
	public abstract class QaExternalServiceBase : NonContainerTest
	{
		private readonly ExternalTestClient _externalService;
		private ConcurrentBag<DetectedIssueMsg> _foundIssues;

		protected QaExternalServiceBase([NotNull] IEnumerable<IReadOnlyTable> tables,
		                                string connectionUrl)
			: base(tables)
		{
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

		public override int Execute(IEnumerable<IReadOnlyRow> selectedRows)
		{
			// TODO, if this is ever called
			throw new NotImplementedException();
		}

		public override int Execute(IReadOnlyRow row)
		{
			// TODO, if this is ever called
			throw new NotImplementedException();
		}

		protected override ISpatialReference GetSpatialReference()
		{
			var geoDataset = InvolvedTables.FirstOrDefault() as IGeoDataset;

			return geoDataset?.SpatialReference;
		}

		private int RunTest(
			QualityTestGrpc.QualityTestGrpcClient client,
			IGeometry aoi)
		{
			// TODO: Get from service?
			CancellationTokenSource cancellationSource = new CancellationTokenSource();

			ExecuteTestRequest request = CreateRequest(aoi);

			AddRequestParameters(request);

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

		protected abstract void AddRequestParameters(ExecuteTestRequest request);

		private ExecuteTestRequest CreateRequest(IGeometry aoi)
		{
			List<IWorkspace> workspaces = new List<IWorkspace>();

			var testDatasets = new List<TestDatasetMsg>();

			foreach (IReadOnlyTable table in InvolvedTables)
			{
				IWorkspace tableWorkspace = table.Workspace;

				int wsIndex = workspaces.FindIndex(w => w == tableWorkspace);

				if (wsIndex < 0)
				{
					workspaces.Add(tableWorkspace);
					wsIndex = workspaces.Count - 1;
				}

				TestDatasetMsg testDatasetMsg = ToInvolvedTable(table, wsIndex);

				testDatasets.Add(testDatasetMsg);
			}

			var workspaceMsgs = workspaces.Select(ToWorkspaceMsg).ToList();

			ShapeMsg aoiMsg = ProtobufGeometryUtils.ToShapeMsg(aoi, ShapeMsg.FormatOneofCase.Wkb);

			var request = new ExecuteTestRequest()
			              {
				              Perimeter = aoiMsg,
			              };

			request.Workspaces.AddRange(workspaceMsgs);

			request.InvolvedTables.AddRange(testDatasets);

			return request;
		}

		private TestDatasetMsg ToInvolvedTable(IReadOnlyTable table, int workspaceIndex)
		{
			var testDataset = new TestDatasetMsg();

			IObjectClass objectClass =
				(IObjectClass) (table as ReadOnlyTable)?.BaseTable ?? (IObjectClass) table;

			ObjectClassMsg classMsg = ProtobufGdbUtils.ToObjectClassMsg(objectClass, true);
			classMsg.WorkspaceHandle = workspaceIndex;

			testDataset.ClassDefinition = classMsg;

			string constraint = GetConstraint(table);

			CallbackUtils.DoWithNonNull(constraint, s => testDataset.FilterExpression = s);

			return testDataset;
		}

		private static WorkspaceMsg ToWorkspaceMsg(IWorkspace workspace, int handleId)
		{
			WorkspaceDbType workspaceType = WorkspaceUtils.GetWorkspaceDbType(workspace);

			var result = new WorkspaceMsg
			             {
				             WorkspaceHandle = handleId,
				             WorkspaceDbType = (int) workspaceType
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

				InvolvedRows involvedRows = GetInvolvedRows(issueMsg.InvolvedObjects);

				errorCount += ReportError(issueMsg.Description, involvedRows, issueGeometry,
				                          issueCode,
				                          issueMsg.AffectedComponent);
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

		private static InvolvedRows GetInvolvedRows(
			RepeatedField<InvolvedObjectsMsg> involvedObjectsMsg)
		{
			var result = new InvolvedRows();

			foreach (InvolvedObjectsMsg involvedTableMsg in involvedObjectsMsg)
			{
				foreach (long objectId in involvedTableMsg.ObjectIds)
				{
					result.Add(new InvolvedRow(involvedTableMsg.Dataset.Name, objectId));
				}
			}

			return result;
		}
	}
}
