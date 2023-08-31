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
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.DomainServices.AO.QA;
using ProSuite.DomainServices.AO.QA.Standalone;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.Microservices.Server.AO.QA
{
	/// <summary>
	/// Dispatcher for test or condition groups created by a <see cref="TestAssembler"/>
	/// to be run in parallel in different processes.
	/// </summary>
	public partial class DistributedTestRunner : ITestRunner
	{
		private class IssueKey
		{
			private readonly ITest _test;
			private readonly ISpatialReference _issueSpatialReference;

			private IssueMsg _issueMsg;

			public IssueKey([NotNull] IssueMsg issueMsg,
			                [NotNull] ITest test,
			                [CanBeNull] ISpatialReference issueSpatialReference)
			{
				_issueMsg = issueMsg;
				ConditionId = _issueMsg.ConditionId;
				Description = _issueMsg.Description;

				_test = test;
				_issueSpatialReference = issueSpatialReference;
			}

			public int ConditionId { get; }
			public string Description { get; }
			public List<InvolvedRow> InvolvedRows => EnsureInvolvedRows();
			private List<InvolvedRow> _involvedRows;

			private List<InvolvedRow> EnsureInvolvedRows()
			{
				if (_involvedRows != null)
				{
					return _involvedRows;
				}

				_involvedRows = GetSortedInvolvedRows(_issueMsg.InvolvedTables);
				TryClearIssueMsg();
				return _involvedRows;
			}

			public QaError QaError => EnsureQaError();
			private QaError _qaError;

			private QaError EnsureQaError()
			{
				if (_qaError != null)
				{
					return _qaError;
				}

				_qaError = GetQaError();
				TryClearIssueMsg();
				return _qaError;
			}

			public bool EnsureKeyData()
			{
				EnsureInvolvedRows();
				EnsureQaError();
				return TryClearIssueMsg();
			}

			private bool TryClearIssueMsg()
			{
				if (_issueMsg == null)
				{
					return false;
				}

				if (_qaError != null && _involvedRows != null)
				{
					_issueMsg = null;
					return true;
				}

				return false;
			}

			private List<InvolvedRow> GetSortedInvolvedRows(IList<InvolvedTableMsg> involvedTables)
			{
				InvolvedRows involvedRows = new InvolvedRows();
				foreach (InvolvedTableMsg involvedTable in involvedTables)
				{
					foreach (long oid in involvedTable.ObjectIds)
					{
						involvedRows.Add(
							new InvolvedRow(involvedTable.TableName, Convert.ToInt32(oid)));
					}
				}

				TestUtils.SortInvolvedRows(involvedRows);
				return involvedRows;
			}

			private List<InvolvedRow> GetSortedInvolvedRows(string legacyInvolvedRows)
			{
				InvolvedRows involvedRows = RowParser.Parse(legacyInvolvedRows);
				TestUtils.SortInvolvedRows(involvedRows);
				return involvedRows;
			}

			private QaError GetQaError()
			{
				QaError error = new QaError(
					_test, _issueMsg.Description, InvolvedRows,
					ProtobufGeometryUtils.FromShapeMsg(_issueMsg.IssueGeometry,
					                                   _issueSpatialReference), null, null);
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

				if (x.ConditionId != y.ConditionId)
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
				return obj.ConditionId ^ 29 * obj.Description.GetHashCode();
			}
		}

		private class SubVerification
		{
			public SubVerification([NotNull] VerificationRequest subRequest,
			                       [NotNull] QualityConditionGroup qualityConditionGroup)
			{
				Assert.ArgumentNotNull(subRequest, nameof(subRequest));
				Assert.ArgumentNotNull(qualityConditionGroup, nameof(qualityConditionGroup));

				SubRequest = subRequest;
				SubResponse = new SubResponse();
				QualityConditionGroup = qualityConditionGroup;
			}

			public VerificationRequest SubRequest { get; }
			public SubResponse SubResponse { get; }
			public IEnvelope TileEnvelope { get; set; }
			public QualityConditionGroup QualityConditionGroup { get; }
			public bool Completed { get; set; }

			public long? InvolvedBaseRowsCount { get; set; }

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
				QualityCondition qc = GetQualityCondition(conditionId);

				ITest test = QualityConditionGroup.QualityConditions[qc].First();
				return test;
			}

			public QualityCondition GetQualityCondition(int conditionId)
			{
				_idConditions = _idConditions ?? GetIdConditions();

				return _idConditions[conditionId];
			}

			public bool ContainsCondition(int conditionId)
			{
				_idConditions = _idConditions ?? GetIdConditions();
				return _idConditions.ContainsKey(conditionId);
			}

			public bool IsFullyProcessed(IssueKey issue, [NotNull] BoxTree<SubVerification> boxTree)
			{
				_idConditions = _idConditions ?? GetIdConditions();
				if (! _idConditions.ContainsKey(issue.ConditionId))
				{
					return false;
				}

				if (! (issue.QaError.InvolvedExtent is WKSEnvelope b))
				{
					return false;
				}

				Box searchBox = new Box(new Pnt2D(b.XMin, b.YMin), new Pnt2D(b.XMax, b.YMax));
				foreach (BoxTree<SubVerification>.TileEntry entry in boxTree.Search(searchBox))
				{
					if (entry.Value.Completed == false)
					{
						return false;
					}
				}

				// TODO: Check extent of issue with processed area
				return true;
			}

			#region Overrides of Object

			public override string ToString()
			{
				return
					$"{QualityConditionGroup.ExecType} sub-verification with {QualityConditionGroup.QualityConditions.Count} " +
					$"condition(s) in envelope {GeometryUtils.ToString(TileEnvelope, true)}";
			}

			#endregion
		}

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static int _currentModelId = -100;

		private readonly IList<IQualityVerificationClient> _workersClients;

		private readonly VerificationRequest _originalRequest;

		private readonly IDictionary<Task<bool>, SubVerification> _tasks =
			new ConcurrentDictionary<Task<bool>, SubVerification>();

		private readonly IDictionary<SubVerification, IQualityVerificationClient>
			_subveriClientsDict =
				new ConcurrentDictionary<SubVerification, IQualityVerificationClient>();

		private readonly HashSet<IQualityVerificationClient> _workingClients =
			new HashSet<IQualityVerificationClient>();

		private readonly HashSet<IQualityVerificationClient> _failedClients =
			new HashSet<IQualityVerificationClient>();

		private BoxTree<SubVerification> _subVerificationsTree;

		public DistributedTestRunner(
			[NotNull] IList<IQualityVerificationClient> workersClients,
			[NotNull] VerificationRequest originalRequest)
		{
			Assert.NotNull(workersClients, nameof(workersClients));
			Assert.NotNull(originalRequest, nameof(originalRequest));
			Assert.ArgumentCondition(originalRequest.MaxParallelProcessing > 1,
			                         "maxParallelDesired must be greater 1");

			_workersClients = workersClients;
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
						primaryModel?.SpatialReferenceDescriptor?.SpatialReference;
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

		public QualityVerification QualityVerification { get; set; }

		// TODO: Simplify to the number of rows and adapt report builder interface
		public RowsWithStopConditions RowsWithStopConditions { get; } =
			new RowsWithStopConditions();

		public string CancellationMessage { get; private set; }

		public bool Cancelled => CancellationTokenSource.IsCancellationRequested;

		public bool SendModelsWithRequest { get; set; }

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

		public void Execute(IEnumerable<ITest> tests, AreaOfInterest areaOfInterest,
		                    CancellationTokenSource cancellationTokenSource)
		{
			Assert.NotNull(QualitySpecification, "QualitySpecification has not been initialized.");
			Assert.NotNull(TestAssembler, "TestAssembler has not been initialized.");

			StartVerification(QualityVerification);

			if (SendModelsWithRequest)
			{
				InitializeModelsToSend(QualitySpecification);
			}

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

			// TODO: Consider a BlockingCollection or some other way to limit through-put
			//       or even the consumer/producer pattern?

			OverallProgressTotal = subVerifications.Count;

			Stack<SubVerification> unhandledSubverifications =
				new Stack<SubVerification>(subVerifications.Reverse());
			IDictionary<Task, SubVerification> started =
				StartSubverifications(unhandledSubverifications);
			if (started.Count <= 0)
			{
				throw new InvalidOperationException("Could not start any subverification");
			}

			Task countTask = null;
			if (Debugger.IsAttached)
			{
				Thread.Sleep(10);

				IList<ReadOnlyFeatureClass> baseFcs = GetParallelBaseFeatureClasses(qcGroups);
				IList<WorkspaceInfo> wsInfos = GetWorkspaceInfos(baseFcs);
				//CountData(subVerifications, wsInfos);
				countTask = Task.Run(() => CountData(subVerifications, wsInfos));
			}

			int failureCount = 0;
			int successCount = 0;
			while (_tasks.Count > 0 || unhandledSubverifications.Count > 0)
			{
				if (TryTakeCompletedRun(_tasks, out Task<bool> task,
				                        out SubVerification completed))
				{
					IQualityVerificationClient finishedClient = _subveriClientsDict[completed];
					_workingClients.Remove(finishedClient);

					string failureMessage =
						ProcessFinalResult(task, completed, finishedClient,
						                   cancelWhenFaulted: false);

					if (failureMessage != null)
					{
						_msg.WarnFormat("{0}{1}Failed verification: {2}", failureMessage,
						                Environment.NewLine, completed);
						failureCount++;
						// TODO: Communicate error to client?!
					}
					else
					{
						_msg.InfoFormat("Finished verification: {0} at {1}", completed,
						                finishedClient.GetAddress());
						successCount++;
					}

					if (task.Status == TaskStatus.Faulted)
					{
						_msg.Warn($"Task {task.Id} failed, trying rerun");

						SubVerification retry =
							new SubVerification(completed.SubRequest,
							                    completed.QualityConditionGroup)
							{
								TileEnvelope = completed.TileEnvelope
							};

						unhandledSubverifications.Push(retry);
					}

					StartSubverifications(unhandledSubverifications);
					if (task.Status != TaskStatus.Faulted)
					{
						CompleteSubverification(completed);
					}

					if (_tasks.Count == 0)
					{
						EndVerification(QualityVerification);
						return;
					}

					_msg.InfoFormat("Remaining verification tasks: {0}", _tasks.Count);
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
				}

				_msg.InfoFormat(
					"Finished distributed verification with {0} failures and {1} successful sub-verifications",
					failureCount, successCount);

				if (countTask?.IsCompleted == true)
				{
					countTask = null;
				}

				Thread.Sleep(100);
			}

			EndVerification(QualityVerification);
		}

		/// <summary>
		/// The data model to be used by the server instead of re-harvesting the (entire) schema.
		/// </summary>
		private SchemaMsg KnownModels { get; set; }

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

				ISpatialReference spatialReference = null;

				if (ddxModel is Model model)
				{
					spatialReference = model.SpatialReferenceDescriptor.SpatialReference;
				}

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
		private IDictionary<Task, SubVerification> StartSubverifications(
			[NotNull] Stack<SubVerification> subVerifications)
		{
			IDictionary<Task, SubVerification> startedVerifications =
				new ConcurrentDictionary<Task, SubVerification>();
			while (true)
			{
				if (subVerifications.Count == 0)
				{
					return startedVerifications;
				}

				IQualityVerificationClient client = GetWorkerClient();
				if (client == null)
				{
					return startedVerifications;
				}

				SubVerification next = subVerifications.Pop();
				Task<bool> newTask = IniTask(next, client);
				_tasks.Add(newTask, next);
				startedVerifications.Add(newTask, next);
			}
		}

		private IQualityVerificationClient GetWorkerClient()
		{
			if (_workersClients.Count == 1)
			{
				return _workersClients[0];
			}

			if (_workersClients.Count <= _workingClients.Count)
			{
				return null;
			}

			foreach (IQualityVerificationClient client in _workersClients)
			{
				if (! _workingClients.Contains(client)
				    && ! _failedClients.Contains(client))
				{
					if (client.CanAcceptCalls(allowFailOver: false))
					{
						return client;
					}

					_failedClients.Add(client);
				}
			}

			return null;
		}

		private Task<bool> IniTask([NotNull] SubVerification subVerification,
		                           [NotNull] IQualityVerificationClient verificationClient)
		{
			VerificationRequest subRequest = subVerification.SubRequest;

			SubResponse subResponse = subVerification.SubResponse;

			// Check if there is a free client (and allow failing over if necessary):
			if (! verificationClient.CanAcceptCalls(true))
			{
				// TODO: Do something else? Use a different worker?
				return Task.FromResult(false);
			}

			_subveriClientsDict.Add(subVerification, verificationClient);
			_workingClients.Add(verificationClient);

			Task<bool> task;

			// Sends schema as protobuf:
			if (KnownModels != null)
				task = Task.Run(
					async () =>
						await VerifySchemaAsync(
							Assert.NotNull(verificationClient.QaGrpcClient),
							subRequest, subResponse, CancellationTokenSource, KnownModels),
					CancellationTokenSource.Token);
			else
			{
				// Re-harvest model in worker or use DDX access, if necessary:
				task = Task.Run(
					async () =>
						await VerifyAsync(Assert.NotNull(verificationClient.QaGrpcClient),
						                  subRequest,
						                  subResponse, CancellationTokenSource),
					CancellationTokenSource.Token);
			}

			// Process the messages even though the foreground thread is blocking/busy processing results
			task.ConfigureAwait(false);
			return task;
		}

		private string ProcessFinalResult(
			[NotNull] Task<bool> task,
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
				QualityVerification.Cancelled = true;
				resultMessage =
					$"Failure in worker {client.GetAddress()}: {subResponse.CancellationMessage}";
			}

			QualityVerificationMsg verificationMsg = subResponse.VerificationMsg;

			if (verificationMsg != null)
			{
				// Failures in tests that get reported as issues (typically a configuration error):
				AddVerification(verificationMsg, QualityVerification);
			}

			DrainIssues(subVerification);

			return resultMessage;
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

			bool drained = false;
			int drainedCount = 0;
			Stopwatch w = Stopwatch.StartNew();
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

				// TODO: improve logic to remove issues from KnownIssues (handle it when subVerification finishes)
				{
					ITest test = verification.GetFirstTest(issueMsg.ConditionId);
					IssueKey key = new IssueKey(issueMsg, test, IssueSpatialReference);
					if (! KnownIssues.ContainsKey(key))
					{
						key.EnsureKeyData();
						KnownIssues.Add(key, key);
					}
					else
					{
						add = false;
					}
				}

				if (add)
				{
					QaError error = ReCreateQaError(issueMsg, verification);

					QualityCondition qualityCondition =
						verification.GetQualityCondition(issueMsg.ConditionId);

					ProcessQaError(error, qualityCondition);
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
					// TODO: Remove int conversion after Server11 merge
					var involvedRow =
						new InvolvedRow(involvedTable.TableName, Convert.ToInt32(objectId));

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
			IEnvelope tile = forSubVerification.TileEnvelope;
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
		                                              [NotNull] IEnvelope tile)
		{
			if (_subVerificationsTree == null)
			{
				return new List<IssueKey>();
			}

			Box.BoxComparer cmp = new Box.BoxComparer();
			Box tileBox = ProxyUtils.CreateBox(tile);
			foreach (BoxTree<SubVerification>.TileEntry tileEntry
			         in _subVerificationsTree.Search(tileBox))
			{
				if (cmp.Equals(tileEntry.Box, tileBox))
				{
					tileEntry.Value.Completed = true;
				}
			}

			IDictionary<IssueKey, IssueKey> knownIssues = KnownIssues;
			List<IssueKey> fullyProcessed = new List<IssueKey>();
			foreach (var issue in knownIssues.Keys)
			{
				if (forSubVerification.IsFullyProcessed(issue, _subVerificationsTree))
				{
					fullyProcessed.Add(issue);
				}
			}

			return fullyProcessed;
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

			if (Cancelled)
			{
				// Cancelled by caller (there is also the possibility that a worker has failed)
				qualityVerification.Cancelled = true;
			}

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
						x => ProxyUtils.CreateBox(Assert.NotNull(x.TileEnvelope)), 4);

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
				subVerification.TileEnvelope = tileBox;
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

		private static async Task<bool> VerifySchemaAsync(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient rpcClient,
			[NotNull] VerificationRequest request,
			[NotNull] SubResponse subResponse,
			[NotNull] CancellationTokenSource cancellationSource,
			[NotNull] SchemaMsg schemaMsg)
		{
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
					else
					{
						HandleResponseMsg(responseMsg.Response, subResponse);
					}
				}
			}

			return true;
		}

		private static void HandleResponseMsg([NotNull] VerificationResponse responseMsg,
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

			var eventArgs = new QaErrorEventArgs(qaError);
			QaError?.Invoke(this, eventArgs);

			if (eventArgs.Cancel)
			{
				return false;
			}

			Assert.NotNull(qualityCondition, "no quality condition for verification");

			StopInfo stopInfo = null;
			if (qualityCondition.StopOnError)
			{
				stopInfo = new StopInfo(qualityCondition, qaError.Description);

				foreach (InvolvedRow involvedRow in qaError.InvolvedRows)
				{
					RowsWithStopConditions.Add(involvedRow.TableName,
					                           involvedRow.OID, stopInfo);
				}
			}

			if (! qualityCondition.AllowErrors)
			{
				if (stopInfo != null)
				{
					// it's a stop condition, and it is a 'hard' condition, and the error is 
					// relevant --> consider the stop situation as sufficiently reported 
					// (no reporting in case of stopped tests required)
					stopInfo.Reported = true;
				}
			}

			return true;
		}
	}
}
