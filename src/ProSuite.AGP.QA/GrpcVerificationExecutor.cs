using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
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

		/// <summary>
		/// Configures optional client-data provision and returns whether it is enabled.
		/// </summary>
		protected virtual Task<bool> ConfigureVerificationRunAsync(
			[NotNull] BackgroundVerificationRun verificationRun)
		{
			return Task.FromResult(false);
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
	}
}
