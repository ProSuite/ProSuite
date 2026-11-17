using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.AGP;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Client.AGP.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.AGP.QA
{
	/// <summary>
	/// Shared gRPC verification pipeline. Implementations bind the application context,
	/// dataset mapping and persistence dependencies for a single execution.
	/// </summary>
	public abstract class GrpcVerificationExecutor : IVerificationExecution
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected GrpcVerificationExecutor([CanBeNull] string resultsPath,
		                                   [CanBeNull] string contextDisplayName)
		{
			ResultsPath = resultsPath;
			ContextDisplayName = contextDisplayName;
		}

		public string ResultsPath { get; }
		public string ContextDisplayName { get; }

		[CanBeNull]
		public string DdxEnvironmentName { get; set; }

		[CanBeNull]
		public VerificationParameters Parameters { get; set; }

		public string HtmlReportName { get; set; }
		public string VerificationReportName { get; set; }

		protected abstract bool RunsLocally { get; }

		protected abstract bool AlwaysUseClientData { get; }

		[NotNull]
		protected abstract QualityVerificationGrpc.QualityVerificationGrpcClient GetQaClient();

		[NotNull]
		protected abstract WorkContextMsg CreateWorkContextMsg();

		protected abstract void SetObjectsToVerify(
			[NotNull] VerificationRequest request, [CanBeNull] IList<Row> objectsToVerify);

		[NotNull]
		protected virtual QualitySpecificationMsg CreateSpecificationMsg(
			[NotNull] IQualitySpecificationReference specification)
		{
			return QAUtils.CreateSpecificationMsg(specification.Id);
		}

		[NotNull]
		protected virtual QualitySpecificationMsg CreateSpecificationMsg(
			[NotNull] QualitySpecification specification)
		{
			return QAUtils.CreateSpecificationMsg(specification);
		}

		[NotNull]
		protected virtual ClientIssueMessageCollector CreateIssueMessageCollector()
		{
			return new ClientIssueMessageCollector();
		}

		[NotNull]
		protected virtual BackgroundVerificationRun CreateVerificationRun(
			[NotNull] VerificationRequest request,
			[NotNull] ClientIssueMessageCollector collector,
			[NotNull] QualityVerificationProgressTracker progress)
		{
			return QAUtils.CreateQualityVerificationRun(request, collector, progress);
		}

		public Task<ServiceCallStatus> Verify(
			IQualitySpecificationReference specification, Geometry perimeter,
			QualityVerificationProgressTracker progress)
		{
			return VerifyReference(specification, null, perimeter, progress, false);
		}

		public Task<ServiceCallStatus> Verify(
			QualitySpecification specification, Geometry perimeter,
			QualityVerificationProgressTracker progress)
		{
			return VerifySpecification(specification, null, perimeter, progress, false);
		}

		public Task<ServiceCallStatus> VerifySelection(
			IQualitySpecificationReference specification, IList<Row> objectsToVerify,
			Geometry perimeter, QualityVerificationProgressTracker progress)
		{
			return VerifyReference(specification, objectsToVerify, perimeter, progress, true);
		}

		public Task<ServiceCallStatus> VerifySelection(
			QualitySpecification specification, IList<Row> objectsToVerify,
			Geometry perimeter, QualityVerificationProgressTracker progress)
		{
			return VerifySpecification(specification, objectsToVerify, perimeter, progress, true);
		}

		private Task<ServiceCallStatus> VerifyReference(
			IQualitySpecificationReference specification, IList<Row> objectsToVerify,
			Geometry perimeter, QualityVerificationProgressTracker progress, bool isSelection)
		{
			Assert.ArgumentNotNull(specification, nameof(specification));
			Assert.NotNull(specification as QualitySpecificationReference,
			               "Unexpected type of quality specification");

			return VerifyCore(() => CreateSpecificationMsg(specification),
			                  collector => collector.SetVerifiedSpecificationId(specification.Id),
			                  objectsToVerify, perimeter, progress, isSelection);
		}

		private Task<ServiceCallStatus> VerifySpecification(
			QualitySpecification specification, IList<Row> objectsToVerify,
			Geometry perimeter, QualityVerificationProgressTracker progress, bool isSelection)
		{
			Assert.ArgumentNotNull(specification, nameof(specification));

			return VerifyCore(() => CreateSpecificationMsg(specification),
			                  collector => collector.SetVerifiedSpecification(specification),
			                  objectsToVerify, perimeter, progress, isSelection);
		}

		private async Task<ServiceCallStatus> VerifyCore(
			Func<QualitySpecificationMsg> createSpecification,
			Action<ClientIssueMessageCollector> setVerifiedSpecification,
			IList<Row> objectsToVerify, Geometry perimeter,
			QualityVerificationProgressTracker progress, bool isSelection)
		{
			Assert.ArgumentNotNull(progress, nameof(progress));

			VerificationRequest request =
				await CreateVerificationRequestAsync(createSpecification, perimeter,
				                                     objectsToVerify);

			SetRequestParameters(request);

			ClientIssueMessageCollector collector = CreateIssueMessageCollector();
			if (isSelection)
			{
				collector.SetVerifiedObjects(objectsToVerify);
			}

			setVerifiedSpecification(collector);

			return await ExecuteRunAsync(request, collector, progress);
		}

		/// <summary>
		/// Builds the request on the MCT, where context and selected-row access must execute.
		/// </summary>
		protected virtual Task<VerificationRequest> CreateVerificationRequestAsync(
			[NotNull] Func<QualitySpecificationMsg> createSpecification,
			[CanBeNull] Geometry perimeter, [CanBeNull] IList<Row> objectsToVerify)
		{
			return QueuedTask.Run(() =>
			{
				WorkContextMsg context = CreateWorkContextMsg();
				VerificationRequest request = QAUtils.CreateRequest(
					context, createSpecification(), perimeter, DdxEnvironmentName);

				SetObjectsToVerify(request, objectsToVerify);
				return request;
			});
		}

		protected virtual async Task<ServiceCallStatus> ExecuteRunAsync(
			[NotNull] VerificationRequest request,
			[NotNull] ClientIssueMessageCollector collector,
			[NotNull] QualityVerificationProgressTracker progress)
		{
			QualityVerificationGrpc.QualityVerificationGrpcClient client =
				Assert.NotNull(GetQaClient());
			BackgroundVerificationRun run = CreateVerificationRun(request, collector, progress);
			bool provideDataFromClient = await ConfigureVerificationRunAsync(run);

			return await run.ExecuteAndProcessMessagesAsync(
				       client, provideDataFromClient, schemaMsg: null);
		}

		protected void SetRequestParameters([NotNull] VerificationRequest request)
		{
			SetPathParameters(request);
			QAUtils.SetVerificationParameters(
				request, -1, Parameters?.SaveVerificationStatisticsInDdx ?? false, true, false);
		}

		/// <summary>
		/// Creates the data provider that serves verified data (and schema) from the client's edit
		/// session for the given project workspace. The base implementation returns null, which
		/// lets the server pull the data itself. Override to enable client-provided data.
		/// </summary>
		[CanBeNull]
		protected virtual IVerificationDataProvider CreateVerificationDataProvider()
		{
			return null;
		}

		[NotNull]
		protected abstract Task<Datastore> GetCurrentDatastore();

		private async Task<bool> ConfigureVerificationRunAsync(
			[NotNull] BackgroundVerificationRun verificationRun)
		{
			bool provideDataFromClient = false;

			IVerificationDataProvider dataProvider =
				CreateVerificationDataProvider();

			Datastore datastore = await GetCurrentDatastore();

			ClientDataReason clientDataReason = dataProvider == null
				                                    ? ClientDataReason.None
				                                    : await GetClientDataReason(datastore);

			if (clientDataReason != ClientDataReason.None)
			{
				provideDataFromClient = true;

				verificationRun.VerificationDataProvider = dataProvider;

				// The provider reads the live workspace; the reads must run on the MCT so that
				// unsaved edits are visible. The interactive verification runs on a worker thread,
				// so blocking on the MCT here does not dead-lock.
				verificationRun.DataProvisionScheduler = func => ProContext.Run(func);

				_msg.Info(GetClientDataMessage(clientDataReason));
			}

			return provideDataFromClient;
		}

		private void SetPathParameters(VerificationRequest request)
		{
			if (string.IsNullOrEmpty(ResultsPath))
			{
				return;
			}

			// GOTOP-796: Local paths are not necessarily accessible on a remote server.
			if (! RunsLocally && ! ResultsPath.StartsWith(@"\\"))
			{
				_msg.Debug("Paths will not be provided to server (no localhost and not UNC path)");
				return;
			}

			string htmlReport = Path.Combine(ResultsPath, HtmlReportName);
			string xmlReport = Path.Combine(ResultsPath, VerificationReportName);
			string gdbDir = Path.Combine(ResultsPath, "issues.gdb");

			if (Parameters?.CreateReports ?? true)
			{
				request.Parameters.HtmlReportPath = htmlReport;
				request.Parameters.VerificationReportPath = xmlReport;
			}

			if (Parameters?.CreateLocalIssueFileGdb ?? true)
			{
				request.Parameters.IssueFileGdbPath = gdbDir;
			}
		}

		private static bool IsFeatureService([CanBeNull] Datastore datastore)
		{
			return datastore?.GetConnector() is ServiceConnectionProperties;
		}

		private enum ClientDataReason
		{
			None,
			Forced,
			FeatureService,
			UnsavedEdits
		}

		private async Task<ClientDataReason> GetClientDataReason(
			[NotNull] Datastore datastore)
		{
			if (AlwaysUseClientData)
			{
				return ClientDataReason.Forced;
			}

			return await QueuedTask.Run(() =>
			{
				if (IsFeatureService(datastore))
				{
					return ClientDataReason.FeatureService;
				}

				return HasUnsavedEdits(datastore)
					       ? ClientDataReason.UnsavedEdits
					       : ClientDataReason.None;
			});
		}

		[NotNull]
		private static string GetClientDataMessage(ClientDataReason reason)
		{
			const string sentFromHere =
				"the data is read from this ArcGIS Pro session and sent to the verification service";

			switch (reason)
			{
				case ClientDataReason.UnsavedEdits:
					return $"The verified data has unsaved edits, so {sentFromHere}.";

				case ClientDataReason.FeatureService:
					return
						$"The verified data is provided from a feature service, so {sentFromHere}.";

				case ClientDataReason.Forced:
					return $"Client-provided verification data is switched on, so {sentFromHere}.";

				default:
					return $"For this verification, {sentFromHere}.";
			}
		}

		private static bool HasUnsavedEdits([CanBeNull] Datastore datastore)
		{
			if (datastore == null)
			{
				return false;
			}

			try
			{
				Project project = Project.Current;
				if (project?.HasEdits != true)
				{
					return false;
				}

				IReadOnlyList<Datastore> editedDatastores = project.EditedDatastores;
				if (editedDatastores == null || editedDatastores.Count == 0)
				{
					return false;
				}

				return editedDatastores.Any(edited =>
					                            WorkspaceUtils.IsSameDatastore(edited, datastore));
			}
			catch (Exception e)
			{
				_msg.Debug($"Error checking edited datastores: {e.Message}", e);
				return false;
			}
		}
	}
}
