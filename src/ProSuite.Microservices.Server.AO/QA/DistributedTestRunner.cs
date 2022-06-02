using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.DomainServices.AO.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.QA.Container;

namespace ProSuite.Microservices.Server.AO.QA
{
	/// <summary>
	/// Dispatcher for test or condition groups created by a <see cref="TestAssembler"/>
	/// to be run in parallel in different processes.
	/// </summary>
	public class DistributedTestRunner : ITestRunner
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly QualityVerificationGrpc.QualityVerificationGrpcClient _qaClient;
		private readonly VerificationRequest _originalRequest;
		private readonly ConcurrentBag<IssueMsg> _issueCollection;

		private readonly IDictionary<Task<bool>, SubResponse> _tasks =
			new ConcurrentDictionary<Task<bool>, SubResponse>();

		public DistributedTestRunner(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient qaClient,
			[NotNull] VerificationRequest originalRequest,
			[CanBeNull] ConcurrentBag<IssueMsg> issueCollection)
		{
			Assert.NotNull(qaClient, nameof(qaClient));
			Assert.NotNull(originalRequest, nameof(originalRequest));
			Assert.ArgumentCondition(originalRequest.MaxParallelProcessing > 1,
			                         "maxParallelDesired must be greater 1");

			_qaClient = qaClient;
			_originalRequest = originalRequest;
			_issueCollection = issueCollection;
		}

		public QualitySpecification QualitySpecification { get; set; }

		public TestAssembler TestAssembler { get; set; }

		public bool FilterTableRowsUsingRelatedGeometry { get; set; }

		private int OverallProgressCurrent { get; set; }
		private int OverallProgressTotal { get; set; }

		private CancellationTokenSource CancellationTokenSource { get; set; }

		public event EventHandler<QaErrorEventArgs> QaError;

		public event EventHandler<VerificationProgressEventArgs> Progress;

		public QualityVerification QualityVerification { get; set; }

		// TODO: Simplify to the number of rows and adapt report builder interface
		public RowsWithStopConditions RowsWithStopConditions { get; } =
			new RowsWithStopConditions();

		public string CancellationMessage { get; private set; }

		public bool Cancelled => CancellationTokenSource.IsCancellationRequested;

		public void Execute(IEnumerable<ITest> tests, AreaOfInterest areaOfInterest,
		                    CancellationTokenSource cancellationTokenSource)
		{
			Assert.NotNull(QualitySpecification, "QualitySpecification has not been initialized.");
			Assert.NotNull(TestAssembler, "TestAssembler has not been initialized.");

			StartVerification(QualityVerification);

			CancellationTokenSource = cancellationTokenSource;

			IList<QualityConditionGroup> qcGroups =
				TestAssembler.BuildQualityConditionGroups(tests.ToList(), areaOfInterest,
				                                          FilterTableRowsUsingRelatedGeometry,
				                                          _originalRequest.MaxParallelProcessing);

			foreach (QualityConditionGroup qualityConditionGroup in qcGroups)
			{
				IList<QualityCondition> qcGroup = qualityConditionGroup.QualityConditions;
				if (qcGroup.Count == 0)
				{
					continue;
				}

				VerificationRequest subRequest =
					CreateSubRequest(_originalRequest, QualitySpecification, qcGroup);

				SubResponse subResponse = new SubResponse();

				Task<bool> task = Task.Run(
					async () =>
						await VerifyAsync(_qaClient, subRequest, subResponse,
						                  CancellationTokenSource),
					CancellationTokenSource.Token);

				// Process the messages even though the foreground thread is blocking/busy processing results
				task.ConfigureAwait(false);
				_tasks.Add(Assert.NotNull(task), subResponse);

				Thread.Sleep(50);
			}

			while (_tasks.Count > 0)
			{
				if (TryTakeCompletedRun(_tasks, out Task<bool> task,
				                        out SubResponse subResponse))
				{
					ProcessFinalResult(task, subResponse);

					_msg.InfoFormat("Remaining work unit verifications: {0}", _tasks.Count);
				}
				else
				{
					// Process intermediate errors (might need de-duplication or other manipulation in the future)

					bool reportProgress = false;
					foreach (SubResponse response in _tasks.Values)
					{
						if (DrainIssues(response))
						{
							reportProgress = true;
						}

						if (UpdateOverallProgress(response))
						{
							reportProgress = true;
						}
					}

					if (reportProgress)
					{
						var eventArgs = new VerificationProgressEventArgs(
							VerificationProgressType.ProcessParallel, OverallProgressCurrent,
							OverallProgressTotal);

						eventArgs.Tag = "TODO";
						Progress?.Invoke(this, eventArgs);
					}
				}

				Thread.Sleep(100);
			}

			EndVerification(QualityVerification);
		}

		private void ProcessFinalResult(Task<bool> task, SubResponse subVerification)
		{
			if (task.IsFaulted)
			{
				_msg.WarnFormat("Sub-verification has faulted: {0}", subVerification);

				CancellationTokenSource.Cancel();
				CancellationMessage = task.Exception?.InnerException?.Message;
			}

			if (! string.IsNullOrEmpty(subVerification.CancellationMessage))
			{
				CancellationMessage = subVerification.CancellationMessage;
			}

			QualityVerificationMsg verificationMsg = subVerification.VerificationMsg;

			if (verificationMsg != null)
			{
				AddVerification(verificationMsg, QualityVerification);
			}

			DrainIssues(subVerification);
		}

		private bool DrainIssues(SubResponse fromSubVerification)
		{
			if (fromSubVerification == null)
			{
				return false;
			}

			bool drained = false;
			IssueMsg issueMsg;
			if (fromSubVerification.Issues.TryTake(out issueMsg))
			{
				// TODO: Consider adding the issue to some IssueProcessor that
				//       keeps track of what has been found and performs
				//       - de-duplication
				//       - potentially re-assembling of partial issues on tile boundaries
				//       - adds the final issues to a 'outbox' colleciton that will be
				//         sent on the next progress
				// This is the global issue collection that will be sent on progress:
				_issueCollection.Add(issueMsg);

				drained = true;
			}

			return drained;
		}

		private static void StartVerification([CanBeNull] QualityVerification qualityVerification)
		{
			if (qualityVerification == null)
			{
				return;
			}

			qualityVerification.Operator = EnvironmentUtils.UserDisplayName;
			qualityVerification.StartDate = DateTime.Now;
		}

		private static void AddVerification([NotNull] QualityVerificationMsg verificationMsg,
		                                    [CanBeNull] QualityVerification toOverallVerification)
		{
			if (toOverallVerification == null)
			{
				return;
			}

			if (verificationMsg.Cancelled)
			{
				toOverallVerification.Cancelled = true;
			}

			foreach (var conditionVerificationMsg in verificationMsg.ConditionVerifications)
			{
				int conditionId = conditionVerificationMsg.QualityConditionId;

				QualityConditionVerification conditionVerification =
					FindQualityConditionVerification(toOverallVerification, conditionId);

				conditionVerification.ErrorCount += conditionVerificationMsg.ErrorCount;
				conditionVerification.ExecuteTime += conditionVerificationMsg.ExecuteTime;
				conditionVerification.RowExecuteTime += conditionVerificationMsg.RowExecuteTime;
				conditionVerification.TileExecuteTime += conditionVerificationMsg.TileExecuteTime;

				if (! conditionVerificationMsg.Fulfilled)
				{
					conditionVerification.Fulfilled = false;
				}

				if (conditionVerificationMsg.StopConditionId >= 0)
				{
					conditionVerification.StopCondition =
						FindQualityConditionVerification(toOverallVerification,
						                                 conditionVerificationMsg.StopConditionId)
							.QualityCondition;
				}
			}

			foreach (var datasetMsg in verificationMsg.VerificationDatasets)
			{
				QualityVerificationDataset qualityVerificationDataset =
					toOverallVerification.VerificationDatasets.First(
						d => d.Dataset.Id == datasetMsg.DatasetId);

				qualityVerificationDataset.LoadTime += datasetMsg.LoadTime;
			}

			toOverallVerification.ContextName = verificationMsg.ContextName;
			toOverallVerification.ContextType = verificationMsg.ContextType;
			toOverallVerification.ProcessorTimeSeconds += verificationMsg.ProcessorTimeSeconds;
			toOverallVerification.RowsWithStopConditions += verificationMsg.RowsWithStopConditions;
			toOverallVerification.Operator = verificationMsg.UserName;
		}

		private void EndVerification([CanBeNull] QualityVerification qualityVerification)
		{
			if (qualityVerification == null)
			{
				return;
			}

			qualityVerification.EndDate = DateTime.Now;

			qualityVerification.Cancelled = Cancelled;
			qualityVerification.CalculateStatistics();
		}

		private static QualityConditionVerification FindQualityConditionVerification(
			QualityVerification toOverallVerification, int conditionId)
		{
			QualityConditionVerification conditionVerification =
				toOverallVerification.ConditionVerifications.First(
					c => Assert.NotNull(c.QualityCondition).Id ==
					     conditionId);
			return conditionVerification;
		}

		private bool UpdateOverallProgress(SubResponse response)
		{
			// The 'slowest' wins:

			if (response.ProgressTotal == 0)
			{
				return false;
			}

			SubResponse slowest = GetSlowestResponse(_tasks.Values);

			OverallProgressCurrent = slowest.ProgressCurrent;
			OverallProgressTotal = slowest.ProgressTotal;

			return response == slowest;
		}

		private static SubResponse GetSlowestResponse(ICollection<SubResponse> subResponses)
		{
			double slowest = double.MaxValue;

			SubResponse result = null;
			foreach (SubResponse subResponse in subResponses)
			{
				if (subResponse.ProgressRatio < slowest)
				{
					result = subResponse;
					slowest = subResponse.ProgressRatio;
				}
			}

			return result;
		}

		private static bool TryTakeCompletedRun(
			IDictionary<Task<bool>, SubResponse> tasks,
			out Task<bool> task,
			out SubResponse subVerification)
		{
			KeyValuePair<Task<bool>, SubResponse> keyValuePair =
				tasks.FirstOrDefault(kvp => kvp.Key.IsCompleted);

			// NOTE: 'Default' is an empty keyValuePair struct
			task = keyValuePair.Key;
			subVerification = keyValuePair.Value;

			if (task == null)
			{
				return false;
			}

			return tasks.Remove(task);
		}

		private static VerificationRequest CreateSubRequest(
			[NotNull] VerificationRequest originalRequest,
			[NotNull] QualitySpecification specification,
			[NotNull] IList<QualityCondition> qualityConditions)
		{
			var result = new VerificationRequest(originalRequest);

			result.MaxParallelProcessing = 1;

			var requestedConditionIds = new HashSet<int>(qualityConditions.Select(c => c.Id));

			foreach (QualitySpecificationElement element in specification.Elements)
			{
				QualityCondition condition = element.QualityCondition;

				if (! requestedConditionIds.Contains(condition.Id))
				{
					result.Specification.ExcludedConditionIds.Add(condition.Id);
				}
			}

			return result;
		}

		private async Task<bool> VerifyAsync(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient rpcClient,
			[NotNull] VerificationRequest request,
			[NotNull] SubResponse subResponse,
			CancellationTokenSource cancellationSource)
		{
			using (var call = rpcClient.VerifyQuality(request))
			{
				while (await call.ResponseStream.MoveNext(cancellationSource.Token))
				{
					VerificationResponse responseMsg = call.ResponseStream.Current;

					HandleResponseMsg(responseMsg, subResponse);
				}
			}

			return true;
		}

		private void HandleResponseMsg([NotNull] VerificationResponse responseMsg,
		                               [NotNull] SubResponse subResponse)
		{
			foreach (IssueMsg issueMessage in responseMsg.Issues)
			{
				subResponse.Issues.Add(issueMessage);
			}

			subResponse.Status = (ServiceCallStatus) responseMsg.ServiceCallStatus;

			UpdateSubProgress(responseMsg, subResponse);

			if (responseMsg.QualityVerification != null)
			{
				subResponse.VerificationMsg = responseMsg.QualityVerification;
			}

			LogProgress(responseMsg.Progress);
		}

		private static void UpdateSubProgress([NotNull] VerificationResponse responseMsg,
		                                      [NotNull] SubResponse subResponse)
		{
			VerificationProgressMsg progressMsg = responseMsg.Progress;

			if (progressMsg == null)
			{
				return;
			}

			if (subResponse.Status != ServiceCallStatus.Running &&
			    subResponse.Status != ServiceCallStatus.Finished)
			{
				subResponse.CancellationMessage = progressMsg.Message;
			}

			// TODO: More stuff? Box?
			subResponse.ProgressTotal = progressMsg.OverallProgressTotalSteps;
			subResponse.ProgressCurrent = progressMsg.OverallProgressCurrentStep;

			if (progressMsg.CurrentBox != null)
			{
				subResponse.CurrentBox = progressMsg.CurrentBox;
			}
		}

		private static void LogProgress(VerificationProgressMsg progressMsg)
		{
			if (progressMsg == null)
			{
				return;
			}

			_msg.VerboseDebug(() => $"{DateTime.Now} - {progressMsg}");

			_msg.DebugFormat(
				"Received service progress of type {0}/{1}: {2} / {3}",
				(VerificationProgressType) progressMsg.ProgressType,
				(VerificationProgressStep) progressMsg.ProgressStep,
				progressMsg.OverallProgressCurrentStep,
				progressMsg.OverallProgressTotalSteps);
		}
	}
}
