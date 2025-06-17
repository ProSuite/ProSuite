using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.QA
{
	/// <summary>
	/// Encapsulates the execution, progress and result of a background verification run.
	/// Optionally, client-specific actions such as saving newly found issues or showing
	/// the verification can be defined before starting the run.
	/// </summary>
	public class BackgroundVerificationRun
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly IDomainTransactionManager _domainTransactions;
		[NotNull] private readonly IQualityVerificationRepository _qualityVerificationRepository;
		[NotNull] private readonly IQualityConditionRepository _qualityConditionRepository;

		private readonly CancellationTokenSource _cancellationTokenSource;

		public BackgroundVerificationRun(
			[NotNull] VerificationRequest verificationRequest,
			[NotNull] IDomainTransactionManager domainTransactions,
			[NotNull] IQualityVerificationRepository qualityVerificationRepository,
			[NotNull] IQualityConditionRepository qualityConditionRepository,
			CancellationTokenSource cancellationTokenSource)
			: this(verificationRequest, domainTransactions, qualityVerificationRepository,
			       qualityConditionRepository,
			       new QualityVerificationProgressTracker
			       {
				       CancellationTokenSource = cancellationTokenSource
			       }) { }

		public BackgroundVerificationRun(
			[NotNull] VerificationRequest verificationRequest,
			[NotNull] IDomainTransactionManager domainTransactions,
			[NotNull] IQualityVerificationRepository qualityVerificationRepository,
			[NotNull] IQualityConditionRepository qualityConditionRepository,
			QualityVerificationProgressTracker progress)
		{
			VerificationRequest = verificationRequest;
			_domainTransactions = domainTransactions;
			_qualityVerificationRepository = qualityVerificationRepository;
			_qualityConditionRepository = qualityConditionRepository;
			_cancellationTokenSource = progress.CancellationTokenSource;

			Progress = progress;
		}

		[NotNull]
		public VerificationRequest VerificationRequest { get; }

		[CanBeNull]
		public IClientIssueMessageCollector ResultIssueCollector { get; set; }

		[CanBeNull]
		public IVerificationDataProvider VerificationDataProvider { get; set; }

		[CanBeNull]
		public BackgroundVerificationResult QualityVerificationResult { get; private set; }

		[NotNull]
		public IQualityVerificationProgressTracker Progress { get; }

		[CanBeNull]
		public Action<IQualityVerificationResult, ErrorDeletionInPerimeter, bool> SaveAction
		{
			get;
			set;
		}

		public async Task<ServiceCallStatus> ExecuteAndProcessMessagesAsync(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient rpcClient,
			bool provideDataFromClient = false,
			SchemaMsg schemaMsg = null)
		{
			return provideDataFromClient
				       ? await TryExecuteAsync(c => VerifyDataAsync(rpcClient, c, schemaMsg))
				       : await TryExecuteAsync(c => VerifyAsync(rpcClient, c));
		}

		private async Task<ServiceCallStatus> TryExecuteAsync(
			[NotNull] Func<CancellationTokenSource, Task<bool>> func)
		{
			QualityVerificationResult =
				new BackgroundVerificationResult(
					ResultIssueCollector, _domainTransactions,
					_qualityVerificationRepository,
					_qualityConditionRepository)
				{
					HtmlReportPath = VerificationRequest.Parameters.HtmlReportPath,
					IssuesGdbPath = VerificationRequest.Parameters.IssueFileGdbPath
				};

			// The service progress can be used in the non-modal progress dialogue
			Progress.QualityVerificationResult = QualityVerificationResult;

			try
			{
				await func(_cancellationTokenSource);
			}
			catch (RpcException rpcException)
			{
				_msg.Debug("Error in rpc: ", rpcException);

				if (rpcException.StatusCode == StatusCode.Cancelled)
				{
					if (Progress.RemoteCallStatus != ServiceCallStatus.Running)
					{
						// The client has gone, but the final status has already been set:
						return Progress.RemoteCallStatus;
					}
					else
					{
						// Actual cancellation:
						return Progress.RemoteCallStatus = ServiceCallStatus.Cancelled;
					}
				}

				Progress.RemoteCallStatus = ServiceCallStatus.Failed;

				throw;
			}

			return Progress.RemoteCallStatus;
		}

		private async Task<bool> VerifyAsync(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient rpcClient,
			CancellationTokenSource cancellationSource)
		{
			using (var call = rpcClient.VerifyQuality(VerificationRequest))
			{
				while (await call.ResponseStream.MoveNext(cancellationSource.Token))
				{
					VerificationResponse responseMsg = call.ResponseStream.Current;

					HandleProgressMsg(responseMsg);
				}
			}

			return true;
		}

		private async Task<bool> VerifyDataAsync(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient rpcClient,
			[NotNull] CancellationTokenSource cancellationSource,
			[CanBeNull] SchemaMsg schemaMsg = null)
		{
			using (var call = rpcClient.VerifyDataQuality())
			{
				var initialRequest =
					new DataVerificationRequest
					{
						Request = VerificationRequest,
						Schema = schemaMsg
					};

				await call.RequestStream.WriteAsync(initialRequest);

				while (await call.ResponseStream.MoveNext(cancellationSource.Token))
				{
					var responseMsg = call.ResponseStream.Current;

					if (responseMsg.SchemaRequest != null || responseMsg.DataRequest != null)
					{
						await ProvideDataToServer(responseMsg, call.RequestStream,
						                          cancellationSource);
					}
					else
					{
						HandleProgressMsg(responseMsg.Response);
					}
				}
			}

			return true;
		}

		private async Task<bool> ProvideDataToServer(
			[NotNull] DataVerificationResponse serverResponseMsg,
			[NotNull] IClientStreamWriter<DataVerificationRequest> dataStream,
			[NotNull] CancellationTokenSource cancellationSource)
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Sending verification data for {0}...", serverResponseMsg);
			}

			Assert.NotNull(VerificationDataProvider, "No verification data provider available.");

			// If this is called in a different STA thread, everything is extremely slow!

			bool result =
				SatisfyDataRequest(serverResponseMsg, VerificationDataProvider, dataStream);

			//// TODO: Alternative for async-supporting platforms:
			//result = await SatisfyDataQueryAsync(serverResponseMsg.DataRequest, dataStream);

			if (result)
			{
				_msg.DebugFormat("Successfully provided verification data for to the server.");
			}

			return result;
		}

		private static bool SatisfyDataRequest(
			[NotNull] DataVerificationResponse arg,
			[NotNull] IVerificationDataProvider verificationDataProvider,
			[NotNull] IClientStreamWriter<DataVerificationRequest> callRequestStream)
		{
			DataVerificationRequest result = new DataVerificationRequest();

			try
			{
				try
				{
					if (arg.SchemaRequest != null)
					{
						result.Schema = verificationDataProvider.GetGdbSchema(arg.SchemaRequest);
					}
					else if (arg.DataRequest != null)
					{
						result.Data = verificationDataProvider
						              .GetData(arg.DataRequest).FirstOrDefault();
					}
				}
				catch (Exception e)
				{
					_msg.Debug("Error handling data request", e);

					// Communicate the error to the server but do not throw here (it might just be
					// a test whether a where clause is valid). The server shall decide whether it
					// wants to continue or not.
					result.ErrorMessage = ExceptionUtils.FormatMessage(e);
					callRequestStream.WriteAsync(result);

					return false;
				}

				callRequestStream.WriteAsync(result);

				return true;
			}
			catch (Exception e)
			{
				_msg.Debug("Error handling data request", e);

				// Send an empty message to make sure the server does not wait forever:
				callRequestStream.WriteAsync(result);
				throw;
			}
		}

		private async Task<bool> SatisfyDataQueryAsync(
			[NotNull] DataRequest dataRequest,
			[NotNull] IClientStreamWriter<DataVerificationRequest> targetStream)
		{
			// Once the result messages are split up, this could be used for higher throughput
			foreach (GdbData data in Assert.NotNull(VerificationDataProvider).GetData(dataRequest))
			{
				DataVerificationRequest r = new DataVerificationRequest
				                            {
					                            Data = data
				                            };

				await targetStream.WriteAsync(r);
			}

			return true;
		}

		private void HandleProgressMsg(VerificationResponse responseMsg)
		{
			try
			{
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

				LogProgress(responseMsg.Progress, responseMsg.Issues.Count);
			}
			catch (Exception e)
			{
				_msg.Warn($"Error handling progress: {e.Message}", e);
				throw;
			}
		}

		private static void LogProgress(VerificationProgressMsg progressMsg,
		                                int issueCount)
		{
			if (progressMsg == null)
			{
				return;
			}

			_msg.VerboseDebug(() => $"{DateTime.Now} - {progressMsg}");

			string issueText =
				issueCount > 0 ? $" (New issues received: {issueCount})" : string.Empty;

			_msg.DebugFormat(
				"Received service progress of type {0}/{1}: {2} / {3}{4}: {5}",
				(VerificationProgressType) progressMsg.ProgressType,
				(VerificationProgressStep) progressMsg.ProgressStep,
				progressMsg.OverallProgressCurrentStep,
				progressMsg.OverallProgressTotalSteps, issueText, progressMsg.Message);
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

			serviceProgress.ProcessingMessage = progressMsg.ProcessingStepMessage;

			if (progressMsg.OverallProgressTotalSteps > 0)
			{
				serviceProgress.OverallProgressTotalSteps = progressMsg.OverallProgressTotalSteps;
			}

			serviceProgress.OverallProgressCurrentStep = progressMsg.OverallProgressCurrentStep;

			if (progressMsg.DetailedProgressTotalSteps > 0)
			{
				serviceProgress.DetailedProgressTotalSteps = progressMsg.DetailedProgressTotalSteps;
			}

			serviceProgress.DetailedProgressCurrentStep = progressMsg.DetailedProgressCurrentStep;

			if (progressMsg.CurrentBox != null)
			{
				serviceProgress.CurrentTile = new EnvelopeXY(
					progressMsg.CurrentBox.XMin, progressMsg.CurrentBox.YMin,
					progressMsg.CurrentBox.XMax, progressMsg.CurrentBox.YMax);
			}

			serviceProgress.StatusMessage = progressMsg.Message;

			serviceProgress.RemoteCallStatus = (ServiceCallStatus) messageProto.ServiceCallStatus;
		}

		public override string ToString()
		{
			return $"BackgroundVerificationRun for request {VerificationRequest}";
		}
	}
}
