using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
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

		private readonly VerificationElements _verificationElements;

		private readonly double _tileSize;

		[CanBeNull] private IGeometry _testPerimeter;

		private TestContainer _executeContainer;

		private IReadOnlyRow _currentRow;

		public SingleThreadedTestRunner(
			[NotNull] VerificationElements verificationElements,
			double tileSize)
		{
			_verificationElements = verificationElements;
			_tileSize = tileSize;
		}

		/// <summary>
		/// Dataset lookup used to update QualityVerification. Can be null
		/// if <see cref="QualityVerification"/> is not set either.
		/// </summary>
		public IDatasetLookup DatasetLookup { get; set; }

		public event EventHandler<QaErrorEventArgs> QaError;

		public event EventHandler<VerificationProgressEventArgs> Progress;

		public TestAssembler TestAssembler { get; set; }

		public QualityVerification QualityVerification { get; set; }

		public ISubVerificationObserver AddObserver(VerificationReporter verificationReporter,
		                                            ISpatialReference spatialReference)
		{
			return null;
		}

		public void Execute(IEnumerable<ITest> tests,
		                    AreaOfInterest areaOfInterest,
		                    CancellationTokenSource cancellationTokenSource)
		{
			Assert.NotNull(TestAssembler, nameof(TestAssembler));

			StartVerification(QualityVerification);

			TimeSpan processorStartTime = ProcessUtils.TryGetUserProcessorTime();

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

		public IObjectSelection ObjectSelection { get; set; }

		public bool ForceFullScanForNonContainerTests { get; set; }

		public bool AllowEditing { get; set; }

		public bool FilterTableRowsUsingRelatedGeometry { get; set; }

		// TODO: extract more specific interface (ICustomTestRowFilter?), separate from stateless
		// method relevant for ICustomErrorFilter:
		[CanBeNull]
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

				IReadOnlyTable table = relGeomTest.Table;
				IList<ITest> testsForTable = relGeomTest.Tests;

				IList<IReadOnlyRow> rows;
				using (progressWatch.MakeTransaction(
					       Step.DataLoading, Step.DataLoaded, tableIndex, tableCount, table))
				{
					try
					{
						rows = GetRowsByRelatedGeometry(
							table, Assert.NotNull(relGeomTest.ObjectDataset), testsForTable[0],
							Assert.NotNull(relGeomTest.RelClassChains));
					}
					catch (Exception e)
					{
						_msg.Debug("Error getting rows by related geometry", e);

						throw new DataException(
							$"Error getting rows of {table.Name} by related geometry: {e.Message}{Environment.NewLine}" +
							$"Used in the following conditions: {StringUtils.Concatenate(testsForTable.Select(t => _verificationElements.GetQualityCondition(t).Name), ", ")}",
							e);
					}
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
							       Step.ITestProcessing, Step.ITestProcessed, testIndex, testCount,
							       test))
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

		private void Verify([NotNull] ITest test, [NotNull] IEnumerable<IReadOnlyRow> rows)
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
						? (IList<InvolvedRow>) new[] { new InvolvedRow(_currentRow) }
						: new List<InvolvedRow>();

				IGeometry geometry = null;
				var feature = _currentRow as IFeature;
				if (feature != null)
				{
					geometry = feature.ShapeCopy;
				}

				QualityCondition qualityCondition = _verificationElements.GetQualityCondition(test);
				string description =
					$"Error testing quality condition '{qualityCondition.Name}': {ExceptionUtils.FormatMessage(e)}";

				_msg.Error(description, e);

				var qaError = new QaError(test, description, involvedRows, geometry, null, null);

				ProcessQaError(qaError);

				_msg.Error(description, e);
				CancellationTokenSource.Cancel();
			}
		}

		[NotNull]
		private IList<IReadOnlyRow> GetRowsByRelatedGeometry(
			[NotNull] IReadOnlyTable table,
			[NotNull] IObjectDataset objectDataset,
			[NotNull] ITest testWithTable,
			[NotNull] IEnumerable<IList<IRelationshipClass>> relClassChains)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));
			Assert.ArgumentNotNull(testWithTable, nameof(testWithTable));

			HashSet<long> oids = ReferenceGeometryUtils.GetOidsByRelatedGeometry(
				table, relClassChains, Assert.NotNull(_testPerimeter), testWithTable);

			// Add oids of selected rows
			if (ObjectSelection != null)
			{
				oids.UnionWith(ObjectSelection.GetSelectedOIDs(objectDataset));
			}

			if (oids.Count == 0)
			{
				return new List<IReadOnlyRow>();
			}

			return new List<IReadOnlyRow>(TableFilterUtils.GetRowsInList(
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
				_msg.Debug(
					"TestContainerException occurred. It will be stored as qaError and the verification will be cancelled.",
					e);

				QualityCondition qualityCondition =
					_verificationElements.GetQualityCondition(e.Test);

				string description =
					$"Error testing {qualityCondition.Name}: {ExceptionUtils.FormatMessage(e)}";

				var involvedRows = new List<InvolvedRow>();
				IGeometry exceptionErrorGeometry = null;

				if (_currentRow != null)
				{
					if (_currentRow.HasOID && _currentRow.Table.HasOID)
						// ie TerrainRow (wrapper around terrain tile)
					{
						// second part: ESRI Bug for IQueryDefTables
						// which returns row.HasOID = true
						involvedRows = InvolvedRowUtils.GetInvolvedRows(_currentRow);
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
			TestVerification testVerification = _verificationElements.GetTestVerification(test);

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
				var feature = e.Row as IReadOnlyFeature;

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
							QualityCondition qualityCondition =
								_verificationElements.GetQualityCondition(test);
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
							QualityCondition qualityCondition =
								_verificationElements.GetQualityCondition(test);
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
			[NotNull] IReadOnlyRow row,
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

			IList<ITest> stoppedTests = _verificationElements.TestsByCondition[stoppedCondition];

			string description =
				TestExecutionUtils.GetStopInfoErrorDescription(stopInfo);

			foreach (ITest stoppedTest in stoppedTests)
			{
				// TODO add issue code
				var error = new QaError(stoppedTest, description,
				                        new[] { new InvolvedRow(row) },
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
			//       or better in an issue processor / filter

			var eventArgs = new QaErrorEventArgs(qaError);
			QaError?.Invoke(this, eventArgs);

			if (eventArgs.Cancel)
			{
				return false;
			}

			ITest test = qaError.Test;
			QualityConditionVerification conditionVerification =
				_verificationElements.GetQualityConditionVerification(test);
			QualityCondition qualityCondition =
				Assert.NotNull(conditionVerification.QualityCondition);

			TestExecutionUtils.ReportRowWithStopCondition(qaError, qualityCondition,
			                                              RowsWithStopConditions);

			return true;
		}

		#endregion

		private void EndVerification([CanBeNull] QualityVerification qualityVerification,
		                             TimeSpan processorStartTime)
		{
			if (qualityVerification == null)
			{
				return;
			}

			TimeSpan processorEndTime = ProcessUtils.TryGetUserProcessorTime();

			TimeSpan processorTime = processorEndTime - processorStartTime;

			qualityVerification.ProcessorTimeSeconds = processorTime.TotalSeconds;

			qualityVerification.EndDate = DateTime.Now;

			qualityVerification.Cancelled = Cancelled;
			qualityVerification.CalculateStatistics();
			qualityVerification.RowsWithStopConditions = RowsWithStopConditions.Count;

			if (DatasetLookup != null)
			{
				// TODO: For standalone service, consider implementing a basic DDX-free ISimpleDatasetLookup
				TestExecutionUtils.AssignExecutionTimes(
					qualityVerification, _verificationElements.TestVerifications,
					VerificationTimeStats, DatasetLookup);
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
	}
}
