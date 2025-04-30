using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.Geodatabase;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.DomainServices.AO.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Standalone;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.Microservices.Server.AO.QA.Distributed
{
	/// <summary>
	/// Dispatcher for test or condition groups created by a <see cref="TestAssembler"/>
	/// to be run in parallel in different processes. This class exists once per request
	/// where the request has MaxParallelProcessing property > 1 which signals the desire
	/// to run a distributed verification.
	/// </summary>
	public class DistributedTestRunner : ITestRunner
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static int _currentModelId = -100;

		private const int _maxReTryCount = 1;

		[NotNull] private readonly DistributedWorkers _distributedWorkers;

		private readonly VerificationRequest _originalRequest;

		private BoxTree<SubVerification> _subVerificationsTree;

		private readonly IDictionary<Task, SubVerification> _tasks =
			new ConcurrentDictionary<Task, SubVerification>();

		public DistributedTestRunner(
			[NotNull] IList<IQualityVerificationClient> workersClients,
			[NotNull] VerificationRequest originalRequest)
		{
			Assert.NotNull(workersClients, nameof(workersClients));
			Assert.NotNull(originalRequest, nameof(originalRequest));
			Assert.ArgumentCondition(originalRequest.MaxParallelProcessing > 1,
			                         "maxParallelDesired must be greater 1");

			_distributedWorkers = new DistributedWorkers(workersClients.ToList());

			_originalRequest = originalRequest;
			ParallelConfiguration = new ParallelConfiguration();
		}

		public DistributedTestRunner(
			[NotNull] DistributedWorkers distributedWorkers,
			[NotNull] VerificationRequest originalRequest)
		{
			Assert.NotNull(distributedWorkers, nameof(distributedWorkers));
			Assert.NotNull(originalRequest, nameof(originalRequest));
			Assert.ArgumentCondition(originalRequest.MaxParallelProcessing > 1,
			                         "maxParallelDesired must be greater 1");

			_distributedWorkers = distributedWorkers;
			_originalRequest = originalRequest;
			ParallelConfiguration = new ParallelConfiguration();
		}

		public QualitySpecification QualitySpecification { get; set; }

		public ISupportedInstanceDescriptors SupportedInstanceDescriptors { get; set; }

		private ISpatialReference IssueSpatialReference
		{
			get
			{
				if (_issueSpatialReference == null)
				{
					Model primaryModel =
						StandaloneVerificationUtils.GetPrimaryModel(QualitySpecification);
					_issueSpatialReference =
						primaryModel?.SpatialReferenceDescriptor?.GetSpatialReference();
				}

				return _issueSpatialReference;
			}
			set { _issueSpatialReference = value; }
		}

		[NotNull]
		public ParallelConfiguration ParallelConfiguration { get; set; }

		public TestAssembler TestAssembler { get; set; }

		public bool FilterTableRowsUsingRelatedGeometry { get; set; }

		private int OverallProgressCurrent { get; set; }
		private int OverallProgressTotal { get; set; }

		private CancellationTokenSource CancellationTokenSource { get; set; }

		public event EventHandler<QaErrorEventArgs> QaError;

		public event EventHandler<VerificationProgressEventArgs> Progress;

		private ISubVerificationObserver SubVerificationObserver { get; set; }

		public QualityVerification QualityVerification { get; set; }

		// TODO: Simplify to the number of rows and adapt report builder interface
		public RowsWithStopConditions RowsWithStopConditions { get; } =
			new RowsWithStopConditions();

		public string CancellationMessage { get; private set; }

		public bool Cancelled => CancellationTokenSource.IsCancellationRequested;

		public bool SendModelsWithRequest { get; set; }

		/// <summary>
		/// The data model to be used by the server instead of re-harvesting the (entire) schema.
		/// </summary>
		private SchemaMsg KnownModels { get; set; }

		private static void AddRecursive(IReadOnlyTable table,
		                                 HashSet<IReadOnlyTable> usedTables)
		{
			usedTables.Add(table);
			if (! (table is IDataContainerAware transformed))
			{
				return;
			}

			foreach (var baseTable in transformed.InvolvedTables)
			{
				AddRecursive(baseTable, usedTables);
			}
		}

		public void CancelSubverifications()
		{
			CancellationTokenSource?.Cancel();
		}

		public ISubVerificationObserver AddObserver(
			VerificationReporter verificationReporter,
			ISpatialReference spatialReference)
		{
			try
			{
				SubVerificationObserver =
					verificationReporter.CreateSubVerificationObserver(
						IssueRepositoryType.FileGdb, spatialReference);
			}
			catch (Exception e)
			{
				_msg.Warn("Error creating sub-verification progress geodatabase. " +
				          "No sub-verification progress will be reported.", e);
			}

			return SubVerificationObserver;
		}

		public void Execute(IEnumerable<ITest> tests, AreaOfInterest areaOfInterest,
		                    CancellationTokenSource cancellationTokenSource)
		{
			Assert.NotNull(QualitySpecification, "QualitySpecification has not been initialized.");
			Assert.NotNull(TestAssembler, "TestAssembler has not been initialized.");

			CancellationTokenSource = cancellationTokenSource;

			StartVerification(QualityVerification);

			if (SendModelsWithRequest)
			{
				InitializeModelsToSend(QualitySpecification);
			}

			bool singleProcess = _originalRequest.MaxParallelProcessing <= 1;

			IList<QualityConditionGroup> qcGroups =
				TestAssembler.BuildQualityConditionGroups(tests.ToList(), areaOfInterest,
				                                          FilterTableRowsUsingRelatedGeometry,
				                                          singleProcess);

			IList<SubVerification> subVerifications = CreateSubVerifications(
				_originalRequest, QualitySpecification,
				qcGroups,
				_originalRequest.MaxParallelProcessing);

			// TODO: Create a structure to check which tileParallel verifications are completed

			OverallProgressTotal = subVerifications.Count;

			Stack<SubVerification> unhandledSubverifications =
				new Stack<SubVerification>(subVerifications.Reverse());
			int id = 1;
			foreach (SubVerification subVerification in unhandledSubverifications)
			{
				subVerification.Id = id++;
			}

			if (SubVerificationObserver != null)
			{
				ReportSubverifcationsCreated(unhandledSubverifications);
			}

			IDictionary<Task, SubVerification> started =
				StartSubVerifications(unhandledSubverifications);

			if (started.Count <= 0)
			{
				_msg.Debug(
					"Could not start any sub-verification. They will be started once free workers are available...");
			}

			if (ProcessesSubVerifications(qcGroups, subVerifications,
			                              unhandledSubverifications))
			{
				EndVerification(QualityVerification);
			}
		}

		private bool ProcessesSubVerifications(
			[NotNull] IList<QualityConditionGroup> qcGroups,
			[NotNull] IList<SubVerification> subVerifications,
			[NotNull] Stack<SubVerification> unhandledSubverifications)
		{
			Task countTask = null;
			if (ParallelConfiguration.SortByNumberOfObjects)
			{
				Thread.Sleep(10);

				IList<ReadOnlyFeatureClass> baseFcs = GetParallelBaseFeatureClasses(qcGroups);
				IList<WorkspaceInfo> wsInfos = GetWorkspaceInfos(baseFcs);
				//CountData(subVerifications, wsInfos);
				countTask = Task.Run(() => CountData(subVerifications, wsInfos));
			}

			int failureCount = 0;
			int successCount = 0;
			int retryCount = 0;
			while (_tasks.Count > 0 || unhandledSubverifications.Count > 0)
			{
				if (_distributedWorkers.TryTakeCompleted(
					    _tasks,
					    out Task task, out SubVerification completed,
					    out IQualityVerificationClient finishedClient))
				{
					string failureMessage =
						ProcessFinalResult(task, completed, finishedClient,
						                   cancelWhenFaulted: false);

					if (failureMessage != null)
					{
						_msg.WarnFormat("{0}{1}Failed verification: {2}", failureMessage,
						                Environment.NewLine, completed);

						if (completed.FailureCount >= _maxReTryCount)
						{
							_msg.InfoFormat(
								"Failure count {0} exceeded re-try count  {1}. Giving up.",
								completed.FailureCount, _maxReTryCount);
							failureCount++;
							CompleteSubverification(completed);

							SubVerificationObserver?.Finished(
								completed.Id, ServiceCallStatus.Failed);
						}
						else
						{
							_msg.Warn($"Task {task.Id} failed, trying rerun");

							SubVerification retry =
								new SubVerification(completed.SubRequest,
								                    completed.QualityConditionGroup)
								{
									TileEnvelope = completed.TileEnvelope,
									FailureCount = completed.FailureCount + 1
								};
							retry.Id = completed.Id;

							retryCount++;
							unhandledSubverifications.Push(retry);

							SubVerificationObserver?.Finished(
								completed.Id, ServiceCallStatus.Retry);
						}

						// TODO: Communicate error to client?!
					}
					else
					{
						_msg.InfoFormat(
							"Finished verification: {0} at {1} with {2} issues of which {3} were filtered out",
							completed, finishedClient.GetAddress(), completed.IssueCount,
							completed.FilteredIssueCount);

						successCount++;
						CompleteSubverification(completed);
						SubVerificationObserver?.Finished(completed.Id, ServiceCallStatus.Finished);
					}

					StartSubVerifications(unhandledSubverifications);

					_msg.InfoFormat(
						"{0} failed and {1} successful sub-verifications. Re-tried verifications: {2}. Remaining: {3}.",
						failureCount, successCount, retryCount, _tasks.Count);
				}
				else
				{
					// Process intermediate errors (might need de-duplication or other manipulation in the future)

					bool reportProgress = false;

					var activeSubverifications = _tasks.Values.ToList();
					foreach (SubVerification subVerification in activeSubverifications)
					{
						if (DrainIssues(subVerification))
						{
							reportProgress = true;
						}
					}

					if (UpdateOverallProgress(subVerifications))
					{
						reportProgress = true;
					}

					if (reportProgress)
					{
						var eventArgs = new VerificationProgressEventArgs(
							VerificationProgressType.ProcessParallel, OverallProgressCurrent,
							OverallProgressTotal);

						eventArgs.Tag = "TODO";
						Progress?.Invoke(this, eventArgs);
					}

					if (_tasks.Count < _distributedWorkers.MaxParallelCount &&
					    _tasks.Count < _originalRequest.MaxParallelProcessing &&
					    unhandledSubverifications.Count > 0)
					{
						// Running under-powered. Check if another worker has become available.
						StartSubVerifications(unhandledSubverifications);
					}
				}

				if (countTask?.IsCompleted == true)
				{
					countTask = null;
					var listToSort = new List<SubVerification>(unhandledSubverifications);
					listToSort.Sort((x, y) =>
						                Math.Sign(
							                (x.InvolvedBaseRowsCount ?? 0)
							                - (y.InvolvedBaseRowsCount ?? 0))
					);
					unhandledSubverifications = new Stack<SubVerification>(listToSort);
				}

				Thread.Sleep(100);
			}

			_msg.InfoFormat(
				"Finished distributed verification with {0} failures and {1} successful sub-verifications",
				failureCount, successCount);

			return true;
		}

		private static IEnumerable<DdxModel> GetReferencedModels(
			[NotNull] QualitySpecification qualitySpecification)
		{
			HashSet<DdxModel> result = new HashSet<DdxModel>();
			foreach (var element in qualitySpecification.Elements)
			{
				foreach (Dataset dataset in element.QualityCondition.GetDatasetParameterValues(
					         true, true))
				{
					if (! result.Contains(dataset.Model))
					{
						result.Add(dataset.Model);
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Initializes the <see cref="KnownModels"/> with the referenced data models. In this case
		/// the SchemaMsg is used to transport the model information for Standalone Verifications
		/// in order to avoid re-harvesting in each sub-verification.
		/// </summary>
		/// <param name="qualitySpecification"></param>
		private void InitializeModelsToSend(QualitySpecification qualitySpecification)
		{
			var modelMsg = new SchemaMsg();

			foreach (DdxModel ddxModel in GetReferencedModels(qualitySpecification))
			{
				if (ddxModel.Id == -1)
				{
					// It's a non-persistent model. Make sure they have a unique identifier
					ddxModel.SetCloneId(_currentModelId--);
				}

				int modelId = ddxModel.Id;

				ISpatialReference spatialReference =
					ddxModel.SpatialReferenceDescriptor?.GetSpatialReference();

				foreach (Dataset dataset in ddxModel.GetDatasets())
				{
					// If persistent, use model id

					ObjectClassMsg objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(
						dataset, modelId, spatialReference);

					modelMsg.ClassDefinitions.Add(objectClassMsg);
				}
			}

			KnownModels = modelMsg;
		}

		[NotNull]
		private IDictionary<Task, SubVerification> StartSubVerifications(
			[NotNull] Stack<SubVerification> subVerifications)
		{
			IDictionary<Task, SubVerification> startedVerifications =
				new ConcurrentDictionary<Task, SubVerification>();

			int originialCount = subVerifications.Count;

			while (true)
			{
				if (subVerifications.Count == 0)
				{
					_msg.Debug("No subverifications provided.");
					return startedVerifications;
				}

				if (_tasks.Count >= _originalRequest.MaxParallelProcessing)
				{
					_msg.DebugFormat(
						"{0} tasks have already been started (requested degree of parallelism: {1})",
						_tasks.Count, _originalRequest.MaxParallelProcessing);
					return startedVerifications;
				}

				Task newTask = _distributedWorkers.StartNext(
					subVerifications, Verify, out SubVerification next);

				if (newTask != null)
				{
					Assert.NotNull(next, "Task started without sub-verification!");

					_tasks.Add(newTask, next);

					startedVerifications.Add(newTask, next);

					SubVerificationObserver?.Started(
						next.Id, _distributedWorkers.GetWorkerClient(next)?.GetAddress());
				}
				else
				{
					if (! _distributedWorkers.HasFreeWorkers())
					{
						_msg.Debug("No more free workers.");
					}

					if (_tasks.Count == 0 && subVerifications.Count > 0)
					{
						_msg.Warn(
							$"All workers busy or exhausted. However, {subVerifications.Count} jobs are left! " +
							$"Possibly, the parallel runner is overwhelmed by multiple requests.");
					}

					_msg.Debug($"Started {startedVerifications.Count} sub-verifications of " +
					           $"{originialCount}. Remaining sub-verifications will be queued...");

					return startedVerifications;
				}
			}
		}

		/// <summary>
		/// Starts the verification on a separate thread-pool(!) thread which will also handle
		/// the worker's responses (progress, issue messages). No COM or other thread-affine state
		/// should be used here!
		/// </summary>
		/// <param name="subVerification"></param>
		/// <param name="verificationClient"></param>
		/// <returns></returns>
		private Task Verify([NotNull] SubVerification subVerification,
		                    [NotNull] IQualityVerificationClient verificationClient)
		{
			VerificationRequest subRequest = subVerification.SubRequest;
			SubResponse subResponse = subVerification.SubResponse;

			Func<Task<bool>> verifyAsync;

			// Sends schema as protobuf:
			if (KnownModels != null)
			{
				verifyAsync = async () => await VerifySchemaAsync(
					                          verificationClient, subRequest, subResponse,
					                          CancellationTokenSource, KnownModels);
			}
			else
			{
				// Re-harvest model in worker or use DDX access, if necessary:
				verifyAsync = async () => await VerifyAsync(verificationClient, subRequest,
				                                            subResponse,
				                                            CancellationTokenSource);
			}

			// The objective is to make sure does not flow across threads inside the scheduling
			// and handling of running sub-verifications and the error processing.
			// The sub-verifications are all scheduled on one of the worker clients using the
			// DistributedWorkers class.
			// This is a kind of inverse work stealing strategy (a.k.a work sharing) where the
			// results are all dealt with on the main (STA) thread.
			// The sub-verifications are scheduled using the default (thread-pool) task scheduler
			// and the task continuations (i.e. handling the worker's progress stream) do not
			// require thread affinity and not special synchronization context is required. See
			// https://devblogs.microsoft.com/pfxteam/await-synchronizationcontext-and-console-apps/
			// This makes the progress stream handling a lot more efficient and applies no extra
			// load on the StaTaskScheduler used for the main request(s).
			//
			// Additionally, we do not want execution context flow back to the STA thread which
			// is ensured by separating the worker-calls onto an MTA thread. See
			// https://devblogs.microsoft.com/dotnet/how-async-await-really-works/.
			// This is achieved by
			// - Using the Task.Factory.StartNew overload with the creation option
			//   TaskCreationOptions.HideScheduler and the default task scheduler (most likely a
			//   redundant overkill but it makes it clear).
			// - Unwrapping the task created by Task.Factory.StartNew() ensures that the correct
			//   parent task is used in the sub-tasks created in the async code flow inside the
			//   verifyAsync delegate.

			Task<bool> task = Task.Factory.StartNew(
				                      verifyAsync, CancellationTokenSource.Token,
				                      TaskCreationOptions.HideScheduler,
				                      TaskScheduler.Default)
			                      .Unwrap();

			return task;
		}

		private string ProcessFinalResult(
			[NotNull] Task task,
			[NotNull] SubVerification subVerification,
			[NotNull] IQualityVerificationClient client,
			bool cancelWhenFaulted)
		{
			string resultMessage = null;

			SubResponse subResponse = subVerification.SubResponse;
			if (task.IsFaulted)
			{
				// This is probably a network failure or process crash:
				_msg.WarnFormat("Sub-verification on {0} has faulted: {1}", client.GetAddress(),
				                subVerification);

				if (cancelWhenFaulted)
				{
					CancellationTokenSource.Cancel();
					CancellationMessage = task.Exception?.InnerException?.Message;
				}

				resultMessage =
					$"Failure in worker {client.GetAddress()}: {subResponse.CancellationMessage}";
			}

			if (! string.IsNullOrEmpty(subResponse.CancellationMessage))
			{
				// This happens if an error occurred in the worker. 
				CancellationMessage = subResponse.CancellationMessage;
			}

			if (subResponse.Status == ServiceCallStatus.Failed &&
			    QualityVerification != null)
			{
				// This happens if an error occurred in the worker (a serious one that stops the process)
				// Cancel the main verification but only on the second attempt.
				if (subVerification.FailureCount >= _maxReTryCount)
				{
					CancelVerification(QualityVerification,
					                   $"Sub-verification {subVerification} has failed more than {_maxReTryCount} times");
				}

				resultMessage =
					$"Failure in worker {client.GetAddress()}: {subResponse.CancellationMessage}";
			}

			DrainIssues(subVerification);

			QualityVerificationMsg verificationMsg = subResponse.VerificationMsg;

			if (verificationMsg != null)
			{
				// Failures in tests that get reported as issues (typically a configuration error):
				AddVerification(verificationMsg, QualityVerification);
			}

			return resultMessage;
		}

		private void CancelVerification([CanBeNull] QualityVerification verification,
		                                [NotNull] string message)
		{
			if (verification == null)
			{
				return;
			}

			verification.Cancelled = true;

			_msg.WarnFormat("{0}. The verification will be marked as cancelled.", message);

			if (CancellationMessage == null)
			{
				CancellationMessage = message;
			}
			else
			{
				CancellationMessage += Environment.NewLine;
				CancellationMessage += message;
			}
		}

		private IDictionary<IssueKey, IssueKey> _knownIssues;
		private ISpatialReference _issueSpatialReference;

		private IDictionary<IssueKey, IssueKey> KnownIssues =>
			_knownIssues ??
			(_knownIssues =
				 new ConcurrentDictionary<IssueKey, IssueKey>(new IssueKeyComparer()));

		private bool DrainIssues([CanBeNull] SubVerification fromSubVerification,
		                         int max = int.MaxValue)
		{
			if (fromSubVerification == null)
			{
				return false;
			}

			SubVerification verification = fromSubVerification;

			_msg.VerboseDebug(
				() =>
					$"Draining {verification.SubResponse.Issues.Count} from {verification} with hashcode {verification.GetHashCode()}");

			bool drained = false;
			int drainedCount = 0;
			Stopwatch w = Stopwatch.StartNew();
			while (drainedCount < max &&
			       verification.SubResponse.Issues.TryTake(out IssueMsg issueMsg))
			{
				drainedCount++;

				// NOTE: This method performs basic de-duplication. In the future it could also
				//       re-assemble partial issues on tile boundaries.

				// TODO: improve logic to remove issues from KnownIssues (handle it when subVerification finishes)
				ITest test = verification.GetFirstTest(issueMsg.ConditionId);
				IssueKey key = new IssueKey(issueMsg, test, IssueSpatialReference);

				if (! KnownIssues.ContainsKey(key))
				{
					// Add to concurrent HashSet
					key.EnsureKeyData();
					KnownIssues.Add(key, key);

					QaError error = ReCreateQaError(issueMsg, verification);

					QualityCondition qualityCondition =
						verification.GetQualityCondition(issueMsg.ConditionId);

					bool processed = ProcessQaError(error, qualityCondition);

					verification.IssueCount++;

					if (! processed)
					{
						verification.FilteredIssueCount++;
						key.Filtered = true;
					}
				}

				drained = true;
			}

			w.Stop();

			return drained;
		}

		private QaError ReCreateQaError(IssueMsg issueMsg, SubVerification verification)
		{
			ITest test = verification.GetFirstTest(issueMsg.ConditionId);

			List<InvolvedRow> involvedRows = new List<InvolvedRow>();
			foreach (InvolvedTableMsg involvedTable in issueMsg.InvolvedTables)
			{
				foreach (long objectId in involvedTable.ObjectIds)
				{
					var involvedRow =
						new InvolvedRow(involvedTable.TableName, objectId);

					involvedRows.Add(involvedRow);
				}
			}

			IssueCode issueCode =
				! string.IsNullOrWhiteSpace(issueMsg.IssueCodeId)
					? new IssueCode(issueMsg.IssueCodeId, issueMsg.IssueCodeDescription)
					: null;

			var error = new QaError(test, issueMsg.Description,
			                        involvedRows,
			                        ProtobufGeometryUtils.FromShapeMsg(
				                        issueMsg.IssueGeometry, IssueSpatialReference),
			                        issueCode, issueMsg.AffectedComponent);
			return error;
		}

		private void CompleteSubverification(SubVerification forSubVerification)
		{
			Thread.Sleep(100);
			forSubVerification.Completed = true;
			EnvelopeXY tile = forSubVerification.TileEnvelope;
			IDictionary<IssueKey, IssueKey> knownIssues = KnownIssues;
			IList<IssueKey> fullyProcessed;
			if (tile != null)
			{
				fullyProcessed = ProcessTileCompletion(forSubVerification, tile);
			}
			else
			{
				fullyProcessed = new List<IssueKey>();
				foreach (var issue in knownIssues.Keys)
				{
					if (forSubVerification.ContainsCondition(issue.ConditionId))
					{
						fullyProcessed.Add(issue);
					}
				}
			}

			foreach (IssueKey issue in fullyProcessed)
			{
				knownIssues.Remove(issue);
			}
		}

		[NotNull]
		private IList<IssueKey> ProcessTileCompletion([NotNull] SubVerification forSubVerification,
		                                              [NotNull] EnvelopeXY tile)
		{
			if (_subVerificationsTree == null)
			{
				return new List<IssueKey>();
			}

			Box.BoxComparer cmp = new Box.BoxComparer();
			Box tileBox = new Box(tile.GetLowerLeftPoint(), tile.GetUpperRightPoint());
			foreach (BoxTree<SubVerification>.TileEntry tileEntry
			         in _subVerificationsTree.Search(tileBox))
			{
				if (cmp.Equals(tileEntry.Box, tileBox))
				{
					tileEntry.Value.Completed = true;
				}
			}

			List<IssueKey> fullyProcessed = new List<IssueKey>();
			foreach (var issue in KnownIssues.Keys)
			{
				if (forSubVerification.IsFullyProcessed(issue, _subVerificationsTree))
				{
					fullyProcessed.Add(issue);
				}
			}

			return fullyProcessed;
		}

		private void StartVerification([CanBeNull] QualityVerification qualityVerification)
		{
			_msg.InfoFormat(
				"Starting client request with a maximum desired degree of parallelism of {0}...",
				_originalRequest.MaxParallelProcessing);

			if (qualityVerification == null)
			{
				return;
			}

			qualityVerification.Operator = EnvironmentUtils.UserDisplayName;
			qualityVerification.StartDate = DateTime.Now;
		}

		private void AddVerification([NotNull] QualityVerificationMsg verificationMsg,
		                             [CanBeNull] QualityVerification toOverallVerification)
		{
			if (toOverallVerification == null)
			{
				return;
			}

			if (verificationMsg.Cancelled)
			{
				CancelVerification(toOverallVerification,
				                   $"Sub-verification {verificationMsg} was cancelled.");
			}

			foreach (var conditionVerificationMsg in verificationMsg.ConditionVerifications)
			{
				int conditionId = conditionVerificationMsg.QualityConditionId;

				QualityConditionVerification conditionVerification =
					FindQualityConditionVerification(toOverallVerification, conditionId);

				// NOTE: The errors are counted (and de-duplicated) via ProcessQaError() 
				conditionVerification.ExecuteTime += conditionVerificationMsg.ExecuteTime;
				conditionVerification.RowExecuteTime += conditionVerificationMsg.RowExecuteTime;
				conditionVerification.TileExecuteTime += conditionVerificationMsg.TileExecuteTime;

				if (! conditionVerificationMsg.Fulfilled)
				{
					bool hasErrors =
						KnownIssues.Keys.Any(i => i.ConditionId == conditionId && ! i.Filtered);

					string message =
						$"The condition {conditionVerification.QualityConditionName} is not fulfilled according to the worker.";

					if (! hasErrors)
					{
						message += " However, all errors have been filtered by the verification.";
					}

					// According to the client (could be un-fulfilled due to previous worker or verification service):
					message += $" The condition is fulfilled: {conditionVerification.Fulfilled}";

					// The condition verification should already have been set to false!
					_msg.Debug(message);
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

			if (Cancelled)
			{
				// Cancelled by caller (there is also the possibility that a worker has failed)
				_msg.Info("The quality verification has likely been cancelled by the caller.");
				qualityVerification.Cancelled = true;
			}

			qualityVerification.CalculateStatistics();

			if (! qualityVerification.Fulfilled)
			{
				_msg.InfoFormat("Un-fulfilled conditions:{0}{1}",
				                Environment.NewLine,
				                StringUtils.Concatenate(
					                qualityVerification.ConditionVerifications.Where(
						                cv => ! cv.Fulfilled), cv => cv.QualityConditionName,
					                Environment.NewLine));
			}
		}

		private static QualityConditionVerification FindQualityConditionVerification(
			QualityVerification toOverallVerification, int conditionId)
		{
			QualityConditionVerification conditionVerification =
				toOverallVerification.ConditionVerifications.FirstOrDefault(
					c =>
					{
						int qualityConditionid = Assert.NotNull(c.QualityCondition).Id;

						return qualityConditionid == conditionId;
					});

			if (conditionVerification == null)
			{
				Func<QualityConditionVerification, string> conditionVerificationToString =
					v =>
						$"Name:{v.QualityConditionName}, Id: {v.QualityConditionId}, Condition Id: {v.QualityCondition?.Id}";

				throw new InvalidOperationException(
					$"Quality condition id {conditionId} not found in verification {toOverallVerification.SpecificationName}. " +
					$"Available condition IDs: {StringUtils.Concatenate(toOverallVerification.ConditionVerifications, conditionVerificationToString, Environment.NewLine)}");
			}

			return conditionVerification;
		}

		private bool UpdateOverallProgress(IEnumerable<SubVerification> subVerifications)
		{
			int completedCount = subVerifications.Count(v => v.Completed);

			if (completedCount == OverallProgressCurrent)
			{
				return false;
			}

			OverallProgressCurrent = completedCount;

			_msg.InfoFormat("Overall progress: {0} of {1} sub-verifications completed",
			                OverallProgressCurrent, OverallProgressTotal);

			return true;
		}

		private IList<SubVerification> CreateSubVerifications(
			[NotNull] VerificationRequest originalRequest,
			[NotNull] QualitySpecification specification,
			[NotNull] IList<QualityConditionGroup> qualityConditionGroups,
			int maxParallel)
		{
			if (maxParallel <= 1)
			{
				Assert.AreEqual(1, qualityConditionGroups.Count,
				                $"Expected 1 qualitycondition group, got {qualityConditionGroups.Count}");
				return new[]
				       {
					       CreateVerification(
						       originalRequest, specification,
						       qualityConditionGroups[0])
				       };
			}

			List<QualityConditionGroup> unhandledQualityConditions =
				new List<QualityConditionGroup>(qualityConditionGroups);

			// Non Container
			var nonContainerGroups =
				qualityConditionGroups
					.Where(x => x.ExecType == QualityConditionExecType.NonContainer)
					.ToList();
			Assert.True(nonContainerGroups.Count <= 1,
			            $"Expected <= 1 non container group, got {nonContainerGroups.Count}");

			Dictionary<QualityConditionExecType, IList<SubVerification>> typeSubVerifications =
				new Dictionary<QualityConditionExecType, IList<SubVerification>>();

			List<SubVerification> nonContainerVerifications = new List<SubVerification>();
			if (nonContainerGroups.Count > 0)
			{
				foreach (QualityConditionGroup nonContainerGroup in nonContainerGroups)
				{
					unhandledQualityConditions.Remove(nonContainerGroup);

					var subs = GetNonContainerSubverifications(nonContainerGroup);
					foreach (var subVerification in subs)
					{
						nonContainerVerifications.Add(
							CreateVerification(originalRequest, specification, subVerification));
					}

					_msg.Info(
						$"Built {subs.Count} subverifications for {nonContainerGroup.QualityConditions.Count} non-container quality conditions");
				}
			}

			typeSubVerifications.Add(QualityConditionExecType.NonContainer,
			                         nonContainerVerifications);

			// TileParallel
			List<QualityConditionGroup> parallelGroups =
				unhandledQualityConditions
					.Where(x => x.ExecType == QualityConditionExecType.TileParallel)
					.ToList();
			Assert.True(parallelGroups.Count <= 1,
			            $"Expected <= 1 tile parallel group, got {parallelGroups.Count}");

			IList<SubVerification> tileParallelVerifications = new List<SubVerification>();
			foreach (QualityConditionGroup parallelGroup in parallelGroups)
			{
				if (parallelGroup.QualityConditions.Count <= 0)
				{
					continue;
				}

				tileParallelVerifications =
					CreateSplitAreaSubverifications(
						originalRequest, specification, parallelGroup);

				_subVerificationsTree =
					BoxTreeUtils.CreateBoxTree(
						tileParallelVerifications,
						x =>
						{
							EnvelopeXY envelope = Assert.NotNull(x.TileEnvelope);
							return new Box(envelope.GetLowerLeftPoint(),
							               envelope.GetUpperRightPoint());
						}, 4);

				_msg.Info(
					$"Built {tileParallelVerifications.Count} tile parallel subverifications for {parallelGroup.QualityConditions.Count} quality conditions");
				unhandledQualityConditions.Remove(parallelGroup);
			}

			typeSubVerifications.Add(QualityConditionExecType.TileParallel,
			                         tileParallelVerifications);

			// Remaining
			int nVerifications =
				ParallelConfiguration.MaxFullAreaTasks <= 0
					? Math.Max(maxParallel / 2,
					           maxParallel - nonContainerVerifications.Count -
					           tileParallelVerifications.Count)
					: ParallelConfiguration.MaxFullAreaTasks;

			List<QualityConditionGroup> qcsPerSubverification = new List<QualityConditionGroup>();
			int iVerification = 0;
			foreach (QualityConditionGroup conditionGroup in unhandledQualityConditions)
			{
				foreach (var pair in conditionGroup.QualityConditions)
				{
					while (qcsPerSubverification.Count <= iVerification)
					{
						qcsPerSubverification.Add(
							new QualityConditionGroup(QualityConditionExecType.Mixed));
					}

					qcsPerSubverification[iVerification].QualityConditions
					                                    .Add(pair.Key, pair.Value);
					iVerification++;
					if (iVerification >= nVerifications)
					{
						iVerification = 0;
					}
				}
			}

			List<SubVerification> containerVerifications = new List<SubVerification>();
			foreach (var qualityConditionGroup in qcsPerSubverification)
			{
				containerVerifications.Add(
					CreateVerification(
						originalRequest, specification, qualityConditionGroup));
			}

			typeSubVerifications.Add(QualityConditionExecType.Container, containerVerifications);
			_msg.Info(
				$"Built {qcsPerSubverification.Count} subverifications for {qcsPerSubverification.Sum(x => x.QualityConditions.Count)} non-tile-parallel container quality conditions");

			// Sorting subverifications corresponding to priorities
			HashSet<QualityConditionExecType> defaultPriorities =
				new HashSet<QualityConditionExecType>
				{
					QualityConditionExecType.NonContainer, QualityConditionExecType.Container,
					QualityConditionExecType.TileParallel
				};

			List<SubVerification> subVerifications = new List<SubVerification>();
			if (ParallelConfiguration.TypePriority != null)
			{
				foreach (QualityConditionExecType execType in ParallelConfiguration.TypePriority)
				{
					subVerifications.AddRange(typeSubVerifications[execType]);
					if (! defaultPriorities.Remove(execType))
					{
						throw new InvalidOperationException($"Unexpected type priority {execType}");
					}
				}
			}

			foreach (QualityConditionExecType execType in defaultPriorities)
			{
				subVerifications.AddRange(typeSubVerifications[execType]);
			}

			return subVerifications;
		}

		[NotNull]
		private List<QualityConditionGroup> GetNonContainerSubverifications(
			QualityConditionGroup nonContainerGroup)
		{
			if (nonContainerGroup.QualityConditions.Count <= 0)
			{
				return new List<QualityConditionGroup>();
			}

			int maxTasks = ParallelConfiguration.MaxNonContainerTasks == 0
				               ? nonContainerGroup.QualityConditions.Count
				               : ParallelConfiguration.MaxNonContainerTasks;

			List<QualityConditionGroup> grps = new List<QualityConditionGroup>();
			int iTask = 0;
			foreach (var pair in nonContainerGroup.QualityConditions)
			{
				while (grps.Count <= iTask)
				{
					grps.Add(new QualityConditionGroup(nonContainerGroup.ExecType));
				}

				QualityConditionGroup grp = grps[iTask];
				grp.QualityConditions.Add(pair.Key, pair.Value);

				iTask++;
				if (iTask >= maxTasks)
				{
					iTask = 0;
				}
			}

			return grps;
		}

		private SubVerification CreateVerification(
			[NotNull] VerificationRequest originalRequest,
			[NotNull] QualitySpecification specification,
			[NotNull] QualityConditionGroup qualityConditionGroup)
		{
			var requestedConditionIds =
				new HashSet<int>(qualityConditionGroup.QualityConditions.Keys.Select(c => c.Id));

			var excludedConditionIds = new List<int>();
			foreach (QualitySpecificationElement element in specification.Elements)
			{
				QualityCondition condition = element.QualityCondition;

				if (! requestedConditionIds.Contains(condition.Id))
				{
					excludedConditionIds.Add(condition.Id);
				}
			}

			VerificationRequest subRequest =
				CreateSubRequest(originalRequest, specification, excludedConditionIds);

			SubVerification subVerification =
				new SubVerification(subRequest, qualityConditionGroup);

			_msg.Debug($"Created sub-verification {subVerification} " +
			           $"with hashcode {subVerification.GetHashCode()}");

			return subVerification;
		}

		private VerificationRequest CreateSubRequest(
			[NotNull] VerificationRequest originalRequest,
			[NotNull] QualitySpecification specification,
			[NotNull] IEnumerable<int> excludedConditionIds)
		{
			var subRequest = new VerificationRequest(originalRequest)
			                 {
				                 MaxParallelProcessing = 1
			                 };

			// Translate to Proto-based specification for better performance
			ConditionListSpecificationMsg listSpecification =
				ProtoDataQualityUtils.CreateConditionListSpecificationMsg(
					specification, SupportedInstanceDescriptors,
					out IDictionary<int, DdxModel> usedModelsById);

			foreach (KeyValuePair<int, DdxModel> kvp in usedModelsById)
			{
				var model = (Model) kvp.Value;
				string dataSourceId = kvp.Key.ToString(CultureInfo.InvariantCulture);

				DataSourceMsg dataSourceMsg =
					listSpecification.DataSources.Single(ds => ds.Id == dataSourceId);

				IFeatureWorkspace workspace = model.UserConnectionProvider.OpenWorkspace();

				// TODO: Handle ddx-based model without catalog path
				dataSourceMsg.CatalogPath =
					WorkspaceUtils.TryGetCatalogPath((IWorkspace) workspace) ?? string.Empty;
			}

			subRequest.Specification.ConditionListSpecification =
				listSpecification;

			// Sub-requests must not write the issue GDB and reports:
			subRequest.Parameters.WriteDetailedVerificationReport = false;
			subRequest.Parameters.IssueFileGdbPath = string.Empty;
			subRequest.Parameters.VerificationReportPath = string.Empty;
			subRequest.Parameters.HtmlReportPath = string.Empty;

			// Sub-requests must never save the verification:
			subRequest.Parameters.SaveVerificationStatistics = false;

			subRequest.Specification.ExcludedConditionIds.AddRange(excludedConditionIds);

			return subRequest;
		}

		private IList<SubVerification> CreateSplitAreaSubverifications(
			[NotNull] VerificationRequest originalRequest,
			[NotNull] QualitySpecification specification,
			[NotNull] QualityConditionGroup qualityConditionGroup)
		{
			List<ContainerTest> tests = new List<ContainerTest>();
			foreach (KeyValuePair<QualityCondition, IList<ITest>> pair in
			         qualityConditionGroup.QualityConditions)
			{
				foreach (var test in pair.Value)
				{
					tests.Add((ContainerTest) test);
				}
			}

			var requestedConditionIds = new HashSet<int>(
				qualityConditionGroup.QualityConditions.Select(c => c.Key.Id));

			List<SubVerification> subVerifications = new List<SubVerification>();
			HashSet<int> excludedConditionIds = new HashSet<int>();
			foreach (QualitySpecificationElement element in specification.Elements)
			{
				QualityCondition condition = element.QualityCondition;

				if (! requestedConditionIds.Contains(condition.Id))
				{
					excludedConditionIds.Add(condition.Id);
				}
			}

			IGeometry perimeter =
				ProtobufGeometryUtils.FromShapeMsg(originalRequest.Parameters.Perimeter);
			IEnvelope executeEnvelope = perimeter?.Envelope;

			double splitSize = ParallelConfiguration.MinimumSplitAreaExtent <= 0
				                   ? originalRequest.Parameters.TileSize
				                   : ParallelConfiguration.MinimumSplitAreaExtent;

			TileEnum tileEnum = new TileEnum(tests, executeEnvelope,
			                                 splitSize,
			                                 executeEnvelope?.SpatialReference);

			int maxSplits = ParallelConfiguration.MaxSplitAreaTasks;
			IEnumerable<IEnvelope> tileBoxEnum;
			if (maxSplits <= 0 || maxSplits >= tileEnum.GetTotalTileCount())
			{
				tileBoxEnum = tileEnum.EnumTiles().Select(x => x.FilterEnvelope);
			}
			else
			{
				tileBoxEnum = EnumTileEnvelopes(tileEnum.GetTestRunEnvelope(), maxSplits);
			}

			foreach (IEnvelope tileBox in tileBoxEnum)
			{
				IGeometry filter = tileBox;
				if (perimeter?.GeometryType == esriGeometryType.esriGeometryPolygon)
				{
					IGeometry clone = GeometryFactory.Clone(perimeter);
					var op = (ITopologicalOperator) clone;
					op.Clip(tileBox);
					if (clone.IsEmpty)
					{
						continue;
					}

					filter = clone;
				}

				var subRequest =
					CreateSubRequest(originalRequest, specification, excludedConditionIds);
				subRequest.Parameters.Perimeter = ProtobufGeometryUtils.ToShapeMsg(filter);

				SubVerification subVerification =
					new SubVerification(subRequest, qualityConditionGroup);
				subVerification.TileEnvelope = GeometryConversionUtils.CreateEnvelopeXY(tileBox);
				subVerifications.Add(subVerification);
			}

			return subVerifications;
		}

		private IEnumerable<IEnvelope> EnumTileEnvelopes(IEnvelope fullEnv, int maxTiles)
		{
			double w = fullEnv.Width;
			double h = fullEnv.Height;
			double x0 = fullEnv.XMin;
			double y0 = fullEnv.YMin;

			int nx = (int) Math.Ceiling(Math.Sqrt(h / w * maxTiles));
			int ny = maxTiles / nx;
			double dx = w / nx;
			double dy = h / ny;

			for (int ix = 0; ix < nx; ix++)
			{
				for (int iy = 0; iy < ny; iy++)
				{
					WKSEnvelope wks = new WKSEnvelope
					                  {
						                  XMin = x0 + ix * dx,
						                  YMin = y0 + iy * dy,
						                  XMax = x0 + (ix + 1) * dx,
						                  YMax = y0 + (iy + 1) * dy
					                  };
					IEnvelope tileBox =
						GeometryFactory.CreateEnvelope(wks, fullEnv.SpatialReference);
					yield return tileBox;
				}
			}
		}

		private static async Task<bool> VerifyAsync(
			[NotNull] IQualityVerificationClient verificationClient,
			[NotNull] VerificationRequest request,
			[NotNull] SubResponse subResponse,
			CancellationTokenSource cancellationSource)
		{
			string workerAddress = verificationClient.GetAddress();

			QualityVerificationGrpc.QualityVerificationGrpcClient rpcClient =
				Assert.NotNull(verificationClient.QaGrpcClient);

			_msg.Debug($"Calling rpc VerifyQuality on {workerAddress}...");

			using (var call = rpcClient.VerifyQuality(request))
			{
				while (await call.ResponseStream.MoveNext(cancellationSource.Token))
				{
					VerificationResponse responseMsg = call.ResponseStream.Current;

					HandleResponseMsg(responseMsg, subResponse, workerAddress);
				}
			}

			return true;
		}

		private static async Task<bool> VerifySchemaAsync(
			[NotNull] IQualityVerificationClient verificationClient,
			[NotNull] VerificationRequest request,
			[NotNull] SubResponse subResponse,
			[NotNull] CancellationTokenSource cancellationSource,
			[NotNull] SchemaMsg schemaMsg)
		{
			string workerAddress = verificationClient.GetAddress();

			QualityVerificationGrpc.QualityVerificationGrpcClient rpcClient =
				Assert.NotNull(verificationClient.QaGrpcClient);

			using (var call = rpcClient.VerifyDataQuality())
			{
				var initialRequest =
					new DataVerificationRequest
					{
						Request = request,
						Schema = schemaMsg
					};

				await call.RequestStream.WriteAsync(initialRequest);

				while (await call.ResponseStream.MoveNext(cancellationSource.Token))
				{
					var responseMsg = call.ResponseStream.Current;

					if (responseMsg.SchemaRequest != null || responseMsg.DataRequest != null)
					{
						throw new NotImplementedException();
						//await ProvideDataToServer(responseMsg, call.RequestStream,
						//                          cancellationSource);
					}

					HandleResponseMsg(responseMsg.Response, subResponse, workerAddress);
				}
			}

			return true;
		}

		private static void HandleResponseMsg([NotNull] VerificationResponse responseMsg,
		                                      [NotNull] SubResponse subResponse,
		                                      [NotNull] string workerAddress)
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

			LogProgress(responseMsg, subResponse, workerAddress);
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

		private static void LogProgress([NotNull] VerificationResponse responseMsg,
		                                [NotNull] SubResponse subResponse,
		                                [NotNull] string workerAddress)
		{
			var progressMsg = responseMsg.Progress;

			if (progressMsg == null)
			{
				return;
			}

			// Log every 10 seconds and the first 10 errors or if there are many errors every 10th error or if verbose:

			const int minimumLogInterval = 10;
			bool itsTimeToLog = subResponse.LastProgressLog <
			                    DateTime.Now.AddSeconds(-minimumLogInterval);

			int issueCount = subResponse.Issues.Count;
			bool isFinalResult = responseMsg.ServiceCallStatus != (int) ServiceCallStatus.Running;

			if (_msg.IsVerboseDebugEnabled || itsTimeToLog || isFinalResult || (issueCount <= 10) ||
			    issueCount % 10 == 0)
			{
				string issueText = issueCount > 0 ? $" (Issue count: {issueCount})" : string.Empty;

				VerificationProgressType progressType =
					(VerificationProgressType) progressMsg.ProgressType;

				string progressText =
					$"Received service progress on {Thread.CurrentThread.GetApartmentState()} thread from {workerAddress} of type {progressType}/{(VerificationProgressStep) progressMsg.ProgressStep}:" +
					$" {progressMsg.OverallProgressCurrentStep} / {progressMsg.OverallProgressTotalSteps}{issueText}";

				_msg.DebugFormat(progressText);

				subResponse.LastProgressLog = DateTime.Now;
			}
		}

		private bool ProcessQaError([NotNull] QaError qaError,
		                            QualityCondition qualityCondition)
		{
			// Consider common base class with SingleThreaded runner, or better a separate
			// IssueProcessor class/hierarchy that could also encapsulate the logic of the
			// QualityVerificationServiceBase
			Assert.ArgumentNotNull(qaError, nameof(qaError));

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug(() => $"Issue drained from sub-verification queue: {qaError}");
			}

			// Standard error processing (filtering, updating condition verification, etc.)
			var eventArgs = new QaErrorEventArgs(qaError);
			QaError?.Invoke(this, eventArgs);

			if (eventArgs.Cancel)
			{
				return false;
			}

			TestExecutionUtils.ReportRowWithStopCondition(qaError, qualityCondition,
			                                              RowsWithStopConditions);

			return true;
		}

		#region Row Count

		//
		// These methods are only used in a special configuration (ParallelConfiguration.SortByNumberOfObjects)
		[NotNull]
		private IList<ReadOnlyFeatureClass> GetParallelBaseFeatureClasses(
			[NotNull] IList<QualityConditionGroup> qcGroups)
		{
			QualityConditionGroup parallelGroup =
				qcGroups.FirstOrDefault(
					x => x.ExecType == QualityConditionExecType.TileParallel);

			if (parallelGroup == null)
			{
				return new List<ReadOnlyFeatureClass>();
			}

			HashSet<IReadOnlyTable> usedTables = new HashSet<IReadOnlyTable>();
			foreach (var tests in parallelGroup.QualityConditions.Values)
			{
				foreach (ITest test in tests)
				{
					foreach (IReadOnlyTable table in test.InvolvedTables)
					{
						AddRecursive(table, usedTables);
					}
				}
			}

			IList<ReadOnlyFeatureClass> baseFcs = new List<ReadOnlyFeatureClass>();
			foreach (IReadOnlyTable table in usedTables)
			{
				if (table is ITransformedTable)
				{
					continue;
				}

				if (table is ReadOnlyFeatureClass baseFc)
				{
					baseFcs.Add(baseFc);
				}
			}

			return baseFcs;
		}

		private void CountData(IEnumerable<SubVerification> verifications,
		                       IList<WorkspaceInfo> workspaceInfos)
		{
			List<IReadOnlyFeatureClass> baseFcs = new List<IReadOnlyFeatureClass>();

			foreach (WorkspaceInfo workspaceInfo in workspaceInfos)
			{
				IWorkspace workspace = workspaceInfo.GetWorkspace();

				foreach (string tableName in workspaceInfo.TableNames)
				{
					IFeatureClass fc =
						((IFeatureWorkspace) workspace).OpenFeatureClass(tableName);
					baseFcs.Add(ReadOnlyTableFactory.Create(fc));
				}
			}

			foreach (SubVerification verification in verifications)
			{
				if (verification.QualityConditionGroup.ExecType !=
				    QualityConditionExecType.TileParallel)
				{
					continue;
				}

				IGeometry envelope =
					ProtobufGeometryUtils.FromShapeMsg(
						verification.SubRequest.Parameters.Perimeter);
				if (envelope == null)
				{
					continue;
				}

				long baseRowsCount = 0;
				IFeatureClassFilter filter = new AoFeatureClassFilter(
					envelope, esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects);
				foreach (var baseFc in baseFcs)
				{
					baseRowsCount += baseFc.RowCount(filter);
				}

				verification.InvolvedBaseRowsCount = baseRowsCount;

				IEnvelope e = envelope.Envelope;
				_msg.Debug(
					$"RowCount: {baseRowsCount} [{e.XMin:N0}, {e.YMin:N0}, {e.XMax:N0}, {e.YMax:N0}]");
			}
		}

		private void ReportSubverifcationsCreated(
			[NotNull] IEnumerable<SubVerification> subVerifications)
		{
			if (SubVerificationObserver == null)
			{
				return;
			}

			foreach (SubVerification subVerification in subVerifications)
			{
				List<string> qcNames = new List<string>();

				var sr = subVerification.SubRequest.Specification;
				HashSet<int> excludes = new HashSet<int>();
				foreach (int exclude in sr.ExcludedConditionIds)
				{
					excludes.Add(exclude);
				}

				foreach (QualitySpecificationElementMsg msg in sr.ConditionListSpecification
				                                                 .Elements)
				{
					if (! excludes.Contains(msg.Condition.ConditionId))
					{
						qcNames.Add(msg.Condition.Name);
					}
				}

				SubVerificationObserver?.CreatedSubverification(
					subVerification.Id,
					subVerification.TileEnvelope);
			}
		}

		private IList<WorkspaceInfo> GetWorkspaceInfos(
			IList<ReadOnlyFeatureClass> roFeatureClasses)
		{
			Dictionary<IWorkspace, HashSet<string>> wsDict =
				new Dictionary<IWorkspace, HashSet<string>>();
			foreach (var roFc in roFeatureClasses)
			{
				IFeatureClass fc = (IFeatureClass) roFc.BaseTable;
				IDataset ds = (IDataset) fc;
				IWorkspace ws = ds.Workspace;
				IList<string> fcNames = new List<string>();
				if (ds.FullName is IQueryName2 qn)
				{
					string tables = qn.QueryDef.Tables;
					foreach (var expression in tables.Split(','))
					{
						foreach (string tableName in expression.Split())
						{
							if (string.IsNullOrWhiteSpace(tableName))
							{
								continue;
							}

							if (((IWorkspace2) ws).get_NameExists(
								    esriDatasetType.esriDTFeatureClass, tableName))
							{
								fcNames.Add(tableName);
							}
						}
					}
				}
				else
				{
					fcNames.Add(ds.Name);
				}

				if (! wsDict.TryGetValue(ws, out HashSet<string> wsInfo))
				{
					wsInfo = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
					wsDict.Add(ws, wsInfo);
				}

				foreach (var fcName in fcNames)
				{
					wsInfo.Add(fcName);
				}
			}

			List<WorkspaceInfo> wsInfos = new List<WorkspaceInfo>();
			foreach (var pair in wsDict)
			{
				IWorkspace ws = pair.Key;
				if (pair.Value.Count == 0)
				{
					continue;
				}

				WorkspaceInfo wsInfo = new WorkspaceInfo(ws);

				foreach (string fcName in pair.Value)
				{
					wsInfo.TableNames.Add(fcName);
				}

				wsInfos.Add(wsInfo);
			}

			return wsInfos;
		}

		private class WorkspaceInfo : TaskWorkspace
		{
			public WorkspaceInfo(IWorkspace workspace)
				: base(workspace)
			{
				TableNames = new List<string>();
			}

			[NotNull]
			public List<string> TableNames { get; }
		}

		#endregion
	}
}
