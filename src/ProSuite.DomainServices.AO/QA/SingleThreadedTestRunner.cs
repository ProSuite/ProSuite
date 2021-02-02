using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.DomainServices.AO.QA
{
	/// <summary>
	/// The default test runner that runs all tests in the same thread.
	/// </summary>
	public class SingleThreadedTestRunner : ITestRunner
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly Dictionary<ITest, TestVerification> _testVerifications;
		private readonly Dictionary<QualityCondition, IList<ITest>> _testsByCondition;

		private readonly double _tileSize;

		[CanBeNull] private IGeometry _testPerimeter;

		private TestContainer _executeContainer;

		private IRow _currentRow;

		[CLSCompliant(false)]
		public SingleThreadedTestRunner(
			Dictionary<ITest, TestVerification> testVerifications,
			Dictionary<QualityCondition, IList<ITest>> testsByCondition,
			double tileSize)
		{
			_testVerifications = testVerifications;
			_testsByCondition = testsByCondition;
			_tileSize = tileSize;
		}

		/// <summary>
		/// Dataset lookup used to update QualityVerification. Can be null
		/// if <see cref="QualityVerification"/> is not set either.
		/// </summary>
		[CLSCompliant(false)]
		public IDatasetLookup DatasetLookup { get; set; }

		public event EventHandler<QaErrorEventArgs> QaError;

		public event EventHandler<VerificationProgressEventArgs> Progress;

		public TestAssembler TestAssembler { get; set; }

		public QualityVerification QualityVerification { get; set; }

		[CLSCompliant(false)]
		public void Execute(IEnumerable<ITest> tests,
		                    AreaOfInterest areaOfInterest,
		                    CancellationTokenSource cancellationTokenSource)
		{
			Assert.NotNull(TestAssembler, nameof(TestAssembler));

			StartVerification(QualityVerification);

			TimeSpan processorStartTime = Process.GetCurrentProcess().UserProcessorTime;

			CancellationTokenSource = cancellationTokenSource;

			CancellationTokenSource.Token.Register(() => _executeContainer?.StopExecute());

			VerificationTimeStats = new VerificationTimeStats();

			_testPerimeter = areaOfInterest?.Geometry;

			IList<ITest> containerTests = TestAssembler.AssembleTests(
				tests, areaOfInterest, FilterTableRowsUsingRelatedGeometry,
				out IList<TestsWithRelatedGeometry> testsWithRelatedGeometry);

			var container = GetTestContainer(containerTests, AllowEditing, _tileSize,
			                                 ForceFullScanForNonContainerTests);

			RowsWithStopConditions.Clear();

			VerifyByRelatedGeometry(testsWithRelatedGeometry);

			if (! Cancelled)
			{
				Verify(container, areaOfInterest);
			}

			EndVerification(QualityVerification, processorStartTime);

			_msg.DebugFormat("Number of rows with stop conditions: {0}",
			                 RowsWithStopConditions.Count);
		}

		public RowsWithStopConditions RowsWithStopConditions { get; } =
			new RowsWithStopConditions();

		public string CancellationMessage { get; private set; }

		public bool Cancelled => CancellationTokenSource.IsCancellationRequested;

		[CLSCompliant(false)]
		public IObjectSelection ObjectSelection { get; set; }

		public bool ForceFullScanForNonContainerTests { get; set; }

		public bool AllowEditing { get; set; }

		public bool FilterTableRowsUsingRelatedGeometry { get; set; }

		// TODO: extract more specific interface (ICustomTestRowFilter?), separate from stateless
		// method relevant for ICustomErrorFilter:
		[CanBeNull]
		[CLSCompliant(false)]
		public ILocationBasedQualitySpecification LocationBasedQualitySpecification { get; set; }

		private CancellationTokenSource CancellationTokenSource { get; set; }
		private VerificationTimeStats VerificationTimeStats { get; set; }

		private TestContainer GetTestContainer(
			[NotNull] IEnumerable<ITest> tests,
			bool allowEditing, double tileSize, bool forceFullScanForNonContainerTests)
		{
			var container = new TestContainer();
			container.AllowEditing = allowEditing;
			container.TileSize = tileSize;
			container.ForceFullScanForNonContainerTests = forceFullScanForNonContainerTests;
			foreach (ITest test in tests)
			{
				container.AddTest(test);
			}

			return container;
		}

		private void VerifyByRelatedGeometry(
			[NotNull] IList<TestsWithRelatedGeometry> relGeomTests)
		{
			int tableIndex = -1;
			int tableCount = relGeomTests.Count;

			Stopwatch watch = _msg.DebugStartTiming();

			var progressWatch =
				new ProgressWatch(args => container_OnProgressChanged(this, args));

			foreach (TestsWithRelatedGeometry relGeomTest in relGeomTests)
			{
				tableIndex++;
				if (Cancelled)
				{
					return;
				}

				if (! relGeomTest.HasAnyAssociationsToFeatureClasses)
				{
					continue;
				}

				ITable table = relGeomTest.Table;
				IList<ITest> testsForTable = relGeomTest.Tests;

				IList<IRow> rows;
				using (progressWatch.MakeTransaction(
					Step.DataLoading, Step.DataLoaded, tableIndex, tableCount, table))
				{
					rows = GetRowsByRelatedGeometry(
						table, Assert.NotNull(relGeomTest.ObjectDataset), testsForTable[0],
						Assert.NotNull(relGeomTest.RelClassChains));
				}

				if (rows.Count == 0)
				{
					continue;
				}

				// there are rows found by related geometry

				var testIndex = 0;
				int testCount = testsForTable.Count;

				foreach (ITest test in testsForTable)
				{
					try
					{
						if (Cancelled)
						{
							return;
						}

						test.TestingRow += container_TestingRow;
						test.QaError += HandleError;

						using (progressWatch.MakeTransaction(
							Step.ITestProcessing, Step.ITestProcessed, testIndex, testCount, test))
						{
							Verify(test, rows);
						}

						testIndex++;
					}
					finally
					{
						test.TestingRow -= container_TestingRow;
						test.QaError -= HandleError;
					}
				}

				// TODO: Verify this:
				// Because the _lastInvolvedRowsReferenceGeometry is tested for reference-equality
				// in GetReferenceGeometry() and for each error a new InvolvedRows list is created,
				// this should not really have an effect:

				//_lastInvolvedRowsReferenceGeometry = null;
				//_lastReferenceGeometry = null;
			}

			_msg.DebugStopTiming(watch, "VerifyByRelatedGeometry()");
		}

		private void Verify([NotNull] ITest test, [NotNull] IEnumerable<IRow> rows)
		{
			Assert.ArgumentNotNull(test, nameof(test));
			Assert.ArgumentNotNull(rows, nameof(rows));

			try
			{
				ClearCurrentRow();

				test.Execute(rows);
			}
			catch (Exception e)
			{
				IList<InvolvedRow> involvedRows =
					_currentRow != null
						? (IList<InvolvedRow>) new[] {new InvolvedRow(_currentRow)}
						: new List<InvolvedRow>();

				IGeometry geometry = null;
				var feature = _currentRow as IFeature;
				if (feature != null)
				{
					geometry = feature.ShapeCopy;
				}

				QualityCondition qualityCondition = GetQualityCondition(test);
				string description = string.Format("Error testing quality condition '{0}': {1}",
				                                   qualityCondition.Name, e.Message);

				_msg.Error(description, e);

				var qaError = new QaError(test, description, involvedRows, geometry, null, null);

				ProcessQaError(qaError);

				_msg.Error(description, e);
				CancellationTokenSource.Cancel();
			}
		}

		[NotNull]
		private IList<IRow> GetRowsByRelatedGeometry(
			[NotNull] ITable table,
			[NotNull] IObjectDataset objectDataset,
			[NotNull] ITest testWithTable,
			[NotNull] IEnumerable<IList<IRelationshipClass>> relClassChains)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));
			Assert.ArgumentNotNull(testWithTable, nameof(testWithTable));

			HashSet<int> oids = ReferenceGeometryUtils.GetOidsByRelatedGeometry(
				table, relClassChains, Assert.NotNull(_testPerimeter), testWithTable);

			// Add oids of selected rows
			if (ObjectSelection != null)
			{
				oids.UnionWith(ObjectSelection.GetSelectedOIDs(objectDataset));
			}

			if (oids.Count == 0)
			{
				return new List<IRow>();
			}

			return new List<IRow>(GdbQueryUtils.GetRowsInList(
				                      table, table.OIDFieldName, oids,
				                      recycle: false));
		}

		private void Verify([NotNull] TestContainer container,
		                    [CanBeNull] AreaOfInterest areaOfInterest)
		{
			Assert.ArgumentNotNull(container, nameof(container));

			ClearCurrentRow();

			// add handlers
			container.TestingRow += container_TestingRow;
			container.QaError += HandleError;
			container.ProgressChanged += container_OnProgressChanged;

			try
			{
				_executeContainer = container;

				// TODO let the container know about the selected features 
				// (filter TestRows by inclusion in selection list)

				TestExecutionUtils.Execute(container, areaOfInterest);
			}
			catch (TestContainerException e)
			{
				QualityCondition qualityCondition = GetQualityCondition(e.Test);

				string description = string.Format("Error testing {0}: {1}",
				                                   qualityCondition.Name,
				                                   e.Message);

				var involvedRows = new InvolvedRow[] { };
				IGeometry exceptionErrorGeometry = null;

				if (_currentRow != null)
				{
					if (_currentRow.HasOID && _currentRow.Table.HasOID)
						// ie TerrainRow (wrapper around terrain tile)
					{
						// second part: ESRI Bug for IQueryDefTables
						// which returns row.HasOID = true
						involvedRows = new[] {new InvolvedRow(_currentRow)};
					}

					try
					{
						var feature = _currentRow as IFeature;

						IGeometry shape = feature?.Shape;
						if (shape != null && ! shape.IsEmpty)
						{
							exceptionErrorGeometry = feature.ShapeCopy;
						}
					}
					catch (Exception exception)
					{
						_msg.Error(
							"Error trying to get geometry from feature involved in failed test",
							exception);
					}
				}

				if (exceptionErrorGeometry == null)
				{
					exceptionErrorGeometry = e.Box;
				}

				var qaError = new QaError(e.Test, description, involvedRows,
				                          exceptionErrorGeometry, null, null);

				ProcessQaError(qaError);

				CancellationTokenSource.Cancel();
				CancellationMessage = description;
				_msg.Error(description, e);
			}
			finally
			{
				// remove handlers
				container.ProgressChanged -= container_OnProgressChanged;
				container.QaError -= HandleError;
				container.TestingRow -= container_TestingRow;
			}
		}

		private void ClearCurrentRow()
		{
			_currentRow = null;

			LocationBasedQualitySpecification?.ResetCurrentFeature();
		}

		/// <summary>
		/// Handles the TestingRow event of the container.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
		/// <remarks>Must be called within dom tx</remarks>
		private void container_TestingRow(object sender, RowEventArgs e)
		{
			if (Cancelled)
			{
				e.Cancel = true;
				return;
			}

			StopInfo stopInfo = RowsWithStopConditions.GetStopInfo(e.Row);

			var test = (ITest) sender;
			TestVerification testVerification = GetTestVerification(test);

			if (stopInfo != null)
			{
				if (! stopInfo.Reported)
				{
					stopInfo.Reported = TryReportStopInfo(
						stopInfo, e.Row,
						testVerification.QualityConditionVerification);
				}

				// cancel further testing on this row
				e.Cancel = true;
				return;
			}

			_currentRow = e.Row;

			if (LocationBasedQualitySpecification != null)
			{
				var feature = e.Row as IFeature;

				if (feature != null &&
				    ! LocationBasedQualitySpecification.IsFeatureToBeTested(
					    feature, e.Recycled, e.RecycleUnique,
					    testVerification.QualityCondition, e.IgnoreTestArea))
				{
					e.Cancel = true;
				}
			}
		}

		private void container_OnProgressChanged(object sender, ProgressArgs args)
		{
			if (Cancelled && _executeContainer != null)
			{
				_executeContainer.StopExecute();
			}

			VerificationTimeStats.Update(args);

			if (args.IsInfoOnly)
			{
				if (args.CurrentStep == Step.DataLoading ||
				    args.CurrentStep == Step.ITestProcessing ||
				    args.CurrentStep == Step.TestRowCreated ||
				    args.CurrentStep == Step.TileCompleting)
				{
					ClearCurrentRow();

					if (_executeContainer == null ||
					    args.CurrentStep == Step.ITestProcessing)
					{
						if (args.CurrentStep == Step.ITestProcessing)
						{
							var test = (ITest) args.Tag;
							QualityCondition qualityCondition = GetQualityCondition(test);
							args = new ProgressArgs(args.CurrentStep, args.Current, args.Total,
							                        qualityCondition);
						}

						OnProgress(new VerificationProgressEventArgs(
							           VerificationProgressType.ProcessNonCache, args));
					}
					else
					{
						if (args.CurrentStep == Step.TileCompleting)
						{
							var test = (ITest) args.Tag;
							QualityCondition qualityCondition = GetQualityCondition(test);
							args = new ProgressArgs(args.CurrentStep, args.Current, args.Total,
							                        qualityCondition);
						}

						OnProgress(new VerificationProgressEventArgs(
							           VerificationProgressType.ProcessContainer, args));
					}
				}
			}
			else
			{
				int current = args.Current;
				if (args.CurrentStep == Step.TileProcessing)
				{
					LocationBasedQualitySpecification?.SetCurrentTile(args.CurrentEnvelope);
					current--; // TODO revise
				}
				else if (args.CurrentStep == Step.TileProcessed)
				{
					LocationBasedQualitySpecification?.SetCurrentTile(null);
					GC.Collect();
				}

				OnProgress(
					new VerificationProgressEventArgs(args.CurrentStep, current, args.Total,
					                                  args.CurrentEnvelope, args.AllBox));
			}
		}

		private void OnProgress([NotNull] VerificationProgressEventArgs args)
		{
			TestExecutionUtils.LogProgress(args);

			if (Progress != null)
			{
				if (args.CurrentBox != null && _testPerimeter != null)
				{
					args.SetSpatialReference(_testPerimeter.SpatialReference);
				}

				Progress(this, args);
			}
		}

		private bool TryReportStopInfo(
			[NotNull] StopInfo stopInfo,
			[NotNull] IRow row,
			[NotNull] QualityConditionVerification qualityConditionVerification)
		{
			Assert.ArgumentNotNull(stopInfo, nameof(stopInfo));
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentNotNull(qualityConditionVerification,
			                       nameof(qualityConditionVerification));

			qualityConditionVerification.StopCondition = stopInfo.QualityCondition;
			QualityCondition stoppedCondition = qualityConditionVerification.QualityCondition;
			Assert.NotNull(stoppedCondition, "stoppedCondition");

			// TODO gather all stopped conditions for the row, report at end
			// https://issuetracker02.eggits.net/browse/COM-248
			IGeometry errorGeom = TestUtils.GetShapeCopy(row);

			IList<ITest> stoppedTests = _testsByCondition[stoppedCondition];

			string description =
				TestExecutionUtils.GetStopInfoErrorDescription(stopInfo);

			foreach (ITest stoppedTest in stoppedTests)
			{
				// TODO add issue code
				var error = new QaError(stoppedTest, description,
				                        new[] {new InvolvedRow(row)},
				                        errorGeom, null, null);
				bool reported = ProcessQaError(error);

				if (reported)
				{
					return true;
				}
			}

			return false;
		}

		#region Error processing

		private void HandleError(object sender, QaErrorEventArgs e)
		{
			if (! ProcessQaError(e.QaError))
			{
				e.Cancel = true;
			}
		}

		private bool ProcessQaError([NotNull] QaError qaError)
		{
			Assert.ArgumentNotNull(qaError, nameof(qaError));

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.DebugFormat("Issue found: {0}", qaError);
			}

			// TODO: Consider checking basic relevance (inside test perimeter?) here

			var eventArgs = new QaErrorEventArgs(qaError);
			QaError?.Invoke(this, eventArgs);

			if (eventArgs.Cancel)
			{
				return false;
			}

			ITest test = qaError.Test;
			QualityConditionVerification conditionVerification =
				GetQualityConditionVerification(test);
			QualityCondition qualityCondition = conditionVerification.QualityCondition;
			Assert.NotNull(qualityCondition, "no quality condition for verification");

			StopInfo stopInfo = null;
			if (conditionVerification.StopOnError)
			{
				stopInfo = new StopInfo(qualityCondition, qaError.Description);

				foreach (InvolvedRow involvedRow in qaError.InvolvedRows)
				{
					RowsWithStopConditions.Add(involvedRow.TableName,
					                           involvedRow.OID, stopInfo);
				}
			}

			if (! conditionVerification.AllowErrors)
			{
				conditionVerification.Fulfilled = false;

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

		#endregion

		private void EndVerification(QualityVerification qualityVerification,
		                             TimeSpan startTime)
		{
			if (qualityVerification == null)
			{
				return;
			}

			TimeSpan endTime = Process.GetCurrentProcess().UserProcessorTime;

			TimeSpan t = endTime - startTime;

			qualityVerification.ProcessorTimeSeconds = t.TotalSeconds;

			qualityVerification.EndDate = DateTime.Now;

			qualityVerification.Cancelled = Cancelled;
			qualityVerification.CalculateStatistics();
			qualityVerification.RowsWithStopConditions = RowsWithStopConditions.Count;

			TestExecutionUtils.AssignExecutionTimes(
				qualityVerification, _testVerifications, VerificationTimeStats,
				Assert.NotNull(DatasetLookup));
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

		[NotNull]
		private TestVerification GetTestVerification([NotNull] ITest test)
		{
			if (! _testVerifications.TryGetValue(test, out TestVerification result))
			{
				throw new ArgumentException(
					$@"No quality condition found for test instance of type {test.GetType()}",
					nameof(test));
			}

			return result;
		}

		[NotNull]
		private QualityConditionVerification GetQualityConditionVerification(
			[NotNull] ITest test)
		{
			TestVerification testVerification = GetTestVerification(test);

			return testVerification.QualityConditionVerification;
		}

		[NotNull]
		private QualityCondition GetQualityCondition([NotNull] ITest test)
		{
			return Assert.NotNull(GetQualityConditionVerification(test).QualityCondition,
			                      "no quality condition for test");
		}
	}
}
