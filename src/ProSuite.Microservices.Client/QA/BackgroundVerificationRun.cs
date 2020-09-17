using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.QA
{
	/// <summary>
	/// Encapsulates the execution, progress and result of a background verification run.
	/// Optionally, client-specific actions such as saving newly found issues or showing
	/// the verification can be defined before starting the run.
	/// </summary>
	public class BackgroundVerificationRun
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[NotNull] private readonly IDomainTransactionManager _domainTransactions;
		[NotNull] private readonly IQualityVerificationRepository _qualityVerificationRepository;
		[NotNull] private readonly IQualityConditionRepository _qualityConditionRepository;

		private readonly CancellationTokenSource _cancellationTokenSource;

		public BackgroundVerificationRun(
			[NotNull] IDomainTransactionManager domainTransactions,
			[NotNull] IQualityVerificationRepository qualityVerificationRepository,
			[NotNull] IQualityConditionRepository qualityConditionRepository,
			CancellationTokenSource cancellationTokenSource)
		{
			_domainTransactions = domainTransactions;
			_qualityVerificationRepository = qualityVerificationRepository;
			_qualityConditionRepository = qualityConditionRepository;
			_cancellationTokenSource = cancellationTokenSource;

			Progress = new QualityVerificationProgressTracker
			           {
				           CancellationTokenSource = cancellationTokenSource
			           };
		}

		[CanBeNull]
		public IClientIssueMessageCollector ResultIssueCollector { get; set; }

		[CanBeNull]
		public BackgroundVerificationResult QualityVerificationResult { get; private set; }

		[NotNull]
		public IQualityVerificationProgressTracker Progress { get; }

		[CanBeNull]
		public Action<IQualityVerificationResult> SaveAction { get; set; }

		[CanBeNull]
		public Action<QualityVerification> ShowReportAction { get; set; }

		public async Task<ServiceCallStatus> ExecuteAndProcessMessagesAsync(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient rpcClient,
			[NotNull] VerificationRequest verificationRequest)
		{
			return await ExecuteAndProcessMessagesAsync(
				       () => rpcClient.VerifyQuality(verificationRequest));
		}

		public async Task<ServiceCallStatus> ExecuteAndProcessMessagesAsync(
			[NotNull] Func<AsyncServerStreamingCall<VerificationResponse>> verificationFunc)
		{
			QualityVerificationResult = new BackgroundVerificationResult(
				ResultIssueCollector, _domainTransactions, _qualityVerificationRepository,
				_qualityConditionRepository);

			// The service progress can be used in the non-modal progress dialogue
			Progress.QualityVerificationResult = QualityVerificationResult;

			try
			{
				using (AsyncServerStreamingCall<VerificationResponse> call = verificationFunc())
				{
					while (await call.ResponseStream.MoveNext(_cancellationTokenSource.Token))
					{
						VerificationResponse responseMsg = call.ResponseStream.Current;

						Progress.RemoteCallStatus =
							(ServiceCallStatus) responseMsg.ServiceCallStatus;

						foreach (IssueMsg issueMessage in responseMsg.Issues)
						{
							ResultIssueCollector?.AddIssueMessage(issueMessage);
						}

						foreach (GdbObjRefMsg objRefMsg in responseMsg.ObsoleteExceptions)
						{
							ResultIssueCollector?.AddObsoleteException(objRefMsg);
						}

						UpdateServiceProgress(Progress, responseMsg);

						if (responseMsg.ServiceCallStatus != (int) ServiceCallStatus.Running)
						{
							// Final message: Finished, Failed or Cancelled

							if (QualityVerificationResult != null)
							{
								QualityVerificationResult.VerificationMsg =
									responseMsg.QualityVerification;

								ResultIssueCollector?.SetVerifiedPerimeter(
									responseMsg.VerifiedPerimeter);
							}
						}

						LogProgress(responseMsg.Progress);
					}
				}
			}
			catch (RpcException rpcException)
			{
				if (rpcException.StatusCode == StatusCode.Cancelled)
				{
					return Progress.RemoteCallStatus = ServiceCallStatus.Cancelled;
				}

				Progress.RemoteCallStatus = ServiceCallStatus.Failed;

				throw;
			}

			return Progress.RemoteCallStatus;
		}

		private static void LogProgress(VerificationProgressMsg progressMsg)
		{
			if (progressMsg == null)
			{
				return;
			}

			_msg.VerboseDebug($"{DateTime.Now} - {progressMsg}");

			_msg.DebugFormat(
				"Received service progress of type {0}/{1}: {2} / {3}",
				(VerificationProgressType) progressMsg.ProgressType,
				(VerificationProgressStep) progressMsg.ProgressStep,
				progressMsg.OverallProgressCurrentStep,
				progressMsg.OverallProgressTotalSteps);
		}

		private static void UpdateServiceProgress(
			IQualityVerificationProgressTracker serviceProgress,
			VerificationResponse messageProto)
		{
			// No access to COM objects here (we're on the background thread!)

			serviceProgress.ErrorCount += messageProto.Issues.Count(i => ! i.Allowable);

			serviceProgress.WarningCount += messageProto.Issues.Count(i => i.Allowable);

			VerificationProgressMsg progressMsg = messageProto.Progress;

			if (progressMsg == null)
			{
				return;
			}

			serviceProgress.ProgressType = (VerificationProgressType) progressMsg.ProgressType;
			serviceProgress.ProgressStep = (VerificationProgressStep) progressMsg.ProgressStep;

			serviceProgress.ProcessingMessage =
				progressMsg.ProcessingStepMessage;

			serviceProgress.OverallProgressTotalSteps =
				progressMsg.OverallProgressTotalSteps;
			serviceProgress.OverallProgressCurrentStep =
				progressMsg.OverallProgressCurrentStep;

			serviceProgress.DetailedProgressTotalSteps =
				progressMsg.DetailedProgressTotalSteps;
			serviceProgress.DetailedProgressCurrentStep =
				progressMsg.DetailedProgressCurrentStep;

			if (progressMsg.CurrentBox != null &&
			    progressMsg.CurrentBox.XMax > 0 &&
			    progressMsg.CurrentBox.YMax > 0)
			{
				serviceProgress.CurrentTile = new EnvelopeXY(
					progressMsg.CurrentBox.XMin, progressMsg.CurrentBox.YMin,
					progressMsg.CurrentBox.XMax, progressMsg.CurrentBox.YMax);
			}

			serviceProgress.StatusMessage = progressMsg.Message;

			serviceProgress.RemoteCallStatus = (ServiceCallStatus) messageProto.ServiceCallStatus;
		}
	}
}
