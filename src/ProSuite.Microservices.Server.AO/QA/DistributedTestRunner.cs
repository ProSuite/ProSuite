using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.DomainServices.AO.QA;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.Microservices.Server.AO.QA
{
	/// <summary>
	/// Dispatcher for test or condition groups created by a <see cref="TestAssembler"/>
	/// to be run in parallel in different processes.
	/// </summary>
	public class DistributedTestRunner : ITestRunner
	{
		private class IssueKey
		{
			private readonly ITest _test;

			public IssueKey([NotNull] IssueMsg issueMsg, [NotNull] ITest test)
			{
				IssueMsg = issueMsg;
				_test = test;
			}

			public IssueMsg IssueMsg { get; }

			public List<InvolvedRow> InvolvedRows =>
				_involvedRows ??
				(_involvedRows = GetSortedInvolvedRows(IssueMsg.LegacyInvolvedRows));

			private List<InvolvedRow> _involvedRows;

			public QaError QaError =>
				_qaError ?? (_qaError = GetQaError());

			private QaError _qaError;

			private List<InvolvedRow> GetSortedInvolvedRows(string legayInvolvedRows)
			{
				InvolvedRows involvedRows = RowParser.Parse(legayInvolvedRows);
				TestUtils.SortInvolvedRows(involvedRows);
				return involvedRows;
			}

			private QaError GetQaError()
			{
				QaError error = new QaError(
					_test, IssueMsg.Description, InvolvedRows,
					ProtobufGeometryUtils.FromShapeMsg(IssueMsg.IssueGeometry), null, null);
				error.ReduceGeometry();
				return error;
			}
		}

		private class IssueKeyComparer : IEqualityComparer<IssueKey>
		{
			public bool Equals(IssueKey x, IssueKey y)
			{
				if (x == y)
					return true;

				if (x == null || y == null)
					return false;

				if (x.IssueMsg.ConditionId != y.IssueMsg.ConditionId)
					return false;

				if (TestUtils.CompareSortedInvolvedRows(x.InvolvedRows, y.InvolvedRows,
				                                        validateRowCount: true) != 0)
				{
					return false;
				}

				if (TestUtils.CompareQaErrors(x.QaError, y.QaError,
				                              compareIndividualInvolvedRows: true) != 0)
				{
					return false;
				}

				return true;
			}

			public int GetHashCode(IssueKey obj)
			{
				return obj.IssueMsg.ConditionId ^ 29 * obj.IssueMsg.Description.GetHashCode();
			}
		}

		private class SubVerification
		{
			private IDictionary<IssueKey, IssueMsg> _knownIssues;

			public SubVerification(VerificationRequest subRequest)
			{
				SubRequest = subRequest;
				SubResponse = new SubResponse();
			}

			public VerificationRequest SubRequest { get; }
			public SubResponse SubResponse { get; }
			public IEnvelope TileEnvelope { get; set; }
			public QualityConditionGroup QualityConditionGroup { get; set; }

			public IDictionary<IssueKey, IssueMsg> KnownIssues =>
				_knownIssues ??
				(_knownIssues =
					 new ConcurrentDictionary<IssueKey, IssueMsg>(new IssueKeyComparer()));

			private Dictionary<int, QualityCondition> _idConditions;

			private Dictionary<int, QualityCondition> GetIdConditions()
			{
				Dictionary<int, QualityCondition> idConditions =
					new Dictionary<int, QualityCondition>();
				foreach (QualityCondition qualityCondition in QualityConditionGroup
				                                              .QualityConditions.Keys)
				{
					idConditions[qualityCondition.Id] = qualityCondition;
				}

				return idConditions;
			}

			public ITest GetFirstTest(int conditionId)
			{
				_idConditions = _idConditions ?? GetIdConditions();
				QualityCondition qc = _idConditions[conditionId];
				ITest test = QualityConditionGroup.QualityConditions[qc].First();
				return test;
			}

			public bool IsFullyProcessed(IssueKey issue)
			{
				// TODO: Check extent of issue with processed area
				return false;
			}
		}

		public enum TileParallelHandlingEnum
		{
			HalfOfMaxParallel,
			OnePerTile
		}

		public enum NonContainerHandlingEnum
		{
			OneSubverificationForAll,
			OneSubverificationPerQualityCondition
		}

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly QualityVerificationGrpc.QualityVerificationGrpcClient _qaClient;
		private readonly VerificationRequest _originalRequest;
		private readonly ConcurrentBag<IssueMsg> _issueCollection;

		private readonly IDictionary<Task<bool>, SubVerification> _tasks =
			new ConcurrentDictionary<Task<bool>, SubVerification>();

		public TileParallelHandlingEnum TileParallelHandling { get; set; } =
			TileParallelHandlingEnum.HalfOfMaxParallel;

		public NonContainerHandlingEnum NonContainerHandling { get; set; } =
			NonContainerHandlingEnum.OneSubverificationForAll;

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
				                                          _originalRequest.MaxParallelProcessing <=
				                                          1);

			IList<SubVerification> subVerifications = CreateSubVerifications(
				_originalRequest, QualitySpecification,
				qcGroups,
				_originalRequest.MaxParallelProcessing);
			// TODO: Create a structure to check which tileParallel verifications are completed

			foreach (var subVerification in subVerifications)
			{
				VerificationRequest subRequest = subVerification.SubRequest;

				// TODO: Refactor (see also QualityVerificationGrpcImpl line ~ 627
				subRequest.Parameters.IssueFileGdbPath += "__";

				SubResponse subResponse = subVerification.SubResponse;

				Task<bool> task = Task.Run(
					async () =>
						await VerifyAsync(_qaClient, subRequest, subResponse,
						                  CancellationTokenSource),
					CancellationTokenSource.Token);

				// Process the messages even though the foreground thread is blocking/busy processing results
				task.ConfigureAwait(false);
				_tasks.Add(task, subVerification);

				Thread.Sleep(50);
			}

			while (_tasks.Count > 0)
			{
				if (TryTakeCompletedRun(_tasks, out Task<bool> task,
				                        out SubVerification completed))
				{
					ProcessFinalResult(task, completed);

					_msg.InfoFormat("Remaining work unit verifications: {0}", _tasks.Count);
				}
				else
				{
					// Process intermediate errors (might need de-duplication or other manipulation in the future)

					bool reportProgress = false;
					foreach (SubVerification subVerification in _tasks.Values)
					{
						if (DrainIssues(subVerification))
						{
							reportProgress = true;
						}

						if (UpdateOverallProgress(subVerification))
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

		private void ProcessFinalResult(Task<bool> task, SubVerification subVerification)
		{
			SubResponse subResponse = subVerification.SubResponse;
			if (task.IsFaulted)
			{
				_msg.WarnFormat("Sub-verification has faulted: {0}", subResponse);

				CancellationTokenSource.Cancel();
				CancellationMessage = task.Exception?.InnerException?.Message;
			}

			if (! string.IsNullOrEmpty(subResponse.CancellationMessage))
			{
				CancellationMessage = subResponse.CancellationMessage;
			}

			QualityVerificationMsg verificationMsg = subResponse.VerificationMsg;

			if (verificationMsg != null)
			{
				AddVerification(verificationMsg, QualityVerification);
			}

			DrainIssues(subVerification);

			ProcessTileCompletion(subVerification);
		}

		private bool DrainIssues([CanBeNull] SubVerification fromSubVerification,
		                         int max = int.MaxValue)
		{
			if (fromSubVerification == null)
			{
				return false;
			}

			SubVerification verification = fromSubVerification;

			bool drained = false;
			int drainedCount = 0;
			while (drainedCount < max &&
			       verification.SubResponse.Issues.TryTake(out IssueMsg issueMsg))
			{
				drainedCount++;

				// TODO: Consider adding the issue to some IssueProcessor that
				//       keeps track of what has been found and performs
				//       - de-duplication
				//       - potentially re-assembling of partial issues on tile boundaries
				//       - adds the final issues to a 'outbox' colleciton that will be
				//         sent on the next progress

				bool add = true;

				if (verification.TileEnvelope != null)
				{
					ITest test = verification.GetFirstTest(issueMsg.ConditionId);
					IssueKey key = new IssueKey(issueMsg, test);
					if (! verification.KnownIssues.ContainsKey(key))
					{
						verification.KnownIssues.Add(key, issueMsg);
					}
					else
					{
						add = false;
					}
				}

				if (add)
				{
					// This is the global issue collection that will be sent on progress:
					_issueCollection.Add(issueMsg);
				}

				drained = true;
			}

			return drained;
		}

		private void ProcessTileCompletion(SubVerification forSubVerification)
		{
			IEnvelope tile = forSubVerification.TileEnvelope;
			if (tile == null)
			{
				return;
			}

			IDictionary<IssueKey, IssueMsg> knownIssues = forSubVerification.KnownIssues;
			List<IssueKey> fullyProcessed = new List<IssueKey>();
			foreach (var issue in knownIssues.Keys)
			{
				if (forSubVerification.IsFullyProcessed(issue))
				{
					fullyProcessed.Add(issue);
				}
			}

			foreach (IssueKey issue in fullyProcessed)
			{
				knownIssues.Remove(issue);
			}
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

		private bool UpdateOverallProgress(SubVerification subVerification)
		{
			// The 'slowest' wins:

			if (subVerification.SubResponse.ProgressTotal == 0)
			{
				return false;
			}

			SubResponse slowest = GetSlowestResponse(_tasks.Select(x => x.Value.SubResponse));

			OverallProgressCurrent = slowest.ProgressCurrent;
			OverallProgressTotal = slowest.ProgressTotal;

			return subVerification.SubResponse == slowest;
		}

		private static SubResponse GetSlowestResponse(IEnumerable<SubResponse> subResponses)
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
			IDictionary<Task<bool>, SubVerification> tasks,
			out Task<bool> task,
			out SubVerification subVerification)
		{
			KeyValuePair<Task<bool>, SubVerification> keyValuePair =
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
						       qualityConditionGroups[0].QualityConditions.Keys)
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

			List<SubVerification> subVerifications = new List<SubVerification>(maxParallel);
			if (nonContainerGroups.Count > 0)
			{
				foreach (QualityConditionGroup nonContainerGroup in nonContainerGroups)
				{
					unhandledQualityConditions.Remove(nonContainerGroup);
					if (nonContainerGroup.QualityConditions.Count <= 0)
					{
						continue;
					}

					if (NonContainerHandling ==
					    NonContainerHandlingEnum.OneSubverificationForAll)
					{
						subVerifications.Add(
							CreateVerification(
								originalRequest, specification,
								nonContainerGroup.QualityConditions.Keys));
					}
					else if (NonContainerHandling ==
					         NonContainerHandlingEnum
						         .OneSubverificationPerQualityCondition)
					{
						foreach (QualityCondition qc in
						         nonContainerGroup.QualityConditions.Keys)
						{
							subVerifications.Add(
								CreateVerification(
									originalRequest, specification,
									new[] { qc }));
						}
					}
					else
					{
						throw new NotImplementedException($"unhandled {NonContainerHandling}");
					}
				}
			}

			// TileParallel
			List<QualityConditionGroup> parallelGroups =
				unhandledQualityConditions
					.Where(x => x.ExecType == QualityConditionExecType.TileParallel)
					.ToList();
			Assert.True(parallelGroups.Count <= 1,
			            $"Expected <= 1 tile parallel group, got {parallelGroups.Count}");

			foreach (QualityConditionGroup parallelGroup in parallelGroups)
			{
				if (parallelGroup.QualityConditions.Count <= 0)
				{
					continue;
				}

				int nMaxTileParallel;
				if (TileParallelHandling == TileParallelHandlingEnum.HalfOfMaxParallel)
				{
					nMaxTileParallel = (maxParallel - subVerifications.Count) / 2;
				}
				else if (TileParallelHandling == TileParallelHandlingEnum.OnePerTile)
				{
					nMaxTileParallel = -1;
				}
				else
				{
					throw new NotImplementedException($"unhandled {TileParallelHandling}");
				}

				IList<SubVerification> tileParallelVerifications =
					CreateTileParallelSubverifications(
						originalRequest, specification, parallelGroup, nMaxTileParallel);

				subVerifications.AddRange(tileParallelVerifications);
				unhandledQualityConditions.Remove(parallelGroup);
			}

			// Remaining
			int nVerifications = Math.Max(maxParallel / 2, maxParallel - subVerifications.Count);
			List<List<QualityCondition>> qcsPerSubverification = new List<List<QualityCondition>>();
			int iVerification = 0;
			foreach (QualityConditionGroup conditionGroup in unhandledQualityConditions)
			{
				foreach (QualityCondition qc in conditionGroup.QualityConditions.Keys)
				{
					while (qcsPerSubverification.Count <= iVerification)
					{
						qcsPerSubverification.Add(new List<QualityCondition>());
					}

					qcsPerSubverification[iVerification].Add(qc);
					iVerification++;
					if (iVerification >= nVerifications)
					{
						iVerification = 0;
					}
				}
			}

			foreach (IList<QualityCondition> qualityConditions in qcsPerSubverification)
			{
				subVerifications.Add(
					CreateVerification(
						originalRequest, specification, qualityConditions));
			}

			return subVerifications;
		}

		private static SubVerification CreateVerification(
			[NotNull] VerificationRequest originalRequest,
			[NotNull] QualitySpecification specification,
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			var subRequest = new VerificationRequest(originalRequest);

			subRequest.MaxParallelProcessing = 1;

			var requestedConditionIds = new HashSet<int>(qualityConditions.Select(c => c.Id));

			foreach (QualitySpecificationElement element in specification.Elements)
			{
				QualityCondition condition = element.QualityCondition;

				if (! requestedConditionIds.Contains(condition.Id))
				{
					subRequest.Specification.ExcludedConditionIds.Add(condition.Id);
				}
			}

			return new SubVerification(subRequest);
		}

		private IList<SubVerification> CreateTileParallelSubverifications(
			[NotNull] VerificationRequest originalRequest,
			[NotNull] QualitySpecification specification,
			[NotNull] QualityConditionGroup qualityConditionGroup,
			int maxTiles)
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
			TileEnum tileEnum = new TileEnum(tests, executeEnvelope,
			                                 originalRequest.Parameters.TileSize,
			                                 executeEnvelope?.SpatialReference);

			IEnumerable<IEnvelope> tileBoxEnum;
			if (maxTiles <= 0 || maxTiles >= tileEnum.GetTotalTileCount())
			{
				tileBoxEnum = tileEnum.EnumTiles().Select(x => x.FilterEnvelope);
			}
			else
			{
				tileBoxEnum = EnumTileEnvelopes(tileEnum.GetTestRunEnvelope(), maxTiles);
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

				var subRequest = new VerificationRequest(originalRequest);
				subRequest.Parameters.Perimeter = ProtobufGeometryUtils.ToShapeMsg(filter);
				subRequest.MaxParallelProcessing = 1;
				subRequest.Specification.ExcludedConditionIds.AddRange(excludedConditionIds);

				SubVerification subVerification = new SubVerification(subRequest);
				subVerification.TileEnvelope = tileBox;
				subVerification.QualityConditionGroup = qualityConditionGroup;
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
