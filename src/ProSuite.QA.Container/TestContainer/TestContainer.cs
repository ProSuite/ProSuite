using System;
using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.QA.Container.TestContainer
{
	public class TestContainer : ITestContainer
	{
		private readonly IGeometryEngine _geometryEngine;
		private readonly IList<ITest> _tests;

		private bool _allowEditing;
		private int _cancelledErrorCount;

		private IList<ContainerTest> _containerTests;
		private QaErrorAdministrator _errorAdministrator;

		private int _errorEventCount;
		private IPolygon _executePolygon;
		private IEnvelope _executeBox;
		private bool _keepErrorGeometry;
		private bool _forceFullScanForNonContainerTests;

		private ISpatialReference _spatialReference;

		private bool _stopExecute;
		private double _tileSize = 10000;
		private int _totalErrorCount;
		private OverlappingFeatures _currentCache;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private ProgressWatch _progressWatch;

		#region Constructors

		public TestContainer()
		{
			_tests = new List<ITest>();

			_errorAdministrator = new QaErrorAdministrator();
			_geometryEngine = new GeometryEngine();

			_allowEditing = false;
		}

		#endregion

		public IEnumerable<ITest> Tests => _tests;

		public int TestCount => _tests.Count;

		public bool AllowEditing
		{
			get { return _allowEditing; }
			set { _allowEditing = value; }
		}

		public bool ForceFullScanForNonContainerTests
		{
			get { return _forceFullScanForNonContainerTests; }
			set { _forceFullScanForNonContainerTests = value; }
		}

		/// <summary>
		/// if true: Keeps the geometry of the error object after it has been reported in the QaError - event
		/// if false : reduced the geometry to its bounding box after it has been reported in the QaError - event
		/// </summary>
		public bool KeepErrorGeometry
		{
			get { return _keepErrorGeometry; }
			set { _keepErrorGeometry = value; }
		}

		public double TileSize
		{
			get { return _tileSize; }
			set { _tileSize = value; }
		}

		public int GetCurrentCachedPointCount()
		{
			return _currentCache?.CurrentCachedPointCount ?? 0;
		}

		#region ITestContainer Members

		/// <summary>
		/// Get or sets maximum number of points that are cached between tiles.
		/// A value &lt; 0 indicates unlimited cache
		/// </summary>
		public int MaxCachedPointCount { get; set; } = 500000;

		/// <summary>
		/// Calculate statistics so that progressChange is more acurate 
		/// </summary>
		[PublicAPI]
		public bool CalculateRowCounts { get; set; }

		[PublicAPI]
		public bool FilterExpressionsUseDbSyntax { get; set; }

		IList<ContainerTest> ITestContainer.ContainerTests => _containerTests;

		IGeometryEngine ITestContainer.GeometryEngine => _geometryEngine;

		ISpatialReference ITestContainer.SpatialReference => _spatialReference;

		void ITestContainer.SubscribeTestEvents(ContainerTest containerTest)
		{
			containerTest.QaError += test_QaError;
			containerTest.TestingRow += test_TestingRow;
		}

		void ITestContainer.UnsubscribeTestEvents(ContainerTest containerTest)
		{
			containerTest.TestingRow -= test_TestingRow;
			containerTest.QaError -= test_QaError;
		}

		void ITestProgress.OnProgressChanged(Step step, int current, int total, object tag)
		{
			OnProgressChanged(step, current, total, tag);
		}

		void ITestContainer.BeginTile(IEnvelope tileEnvelope, IEnvelope testRunEnvelope)
		{
			if (_containerTests == null)
			{
				return;
			}

			var parameters = new BeginTileParameters(tileEnvelope, testRunEnvelope);

			foreach (ContainerTest test in _containerTests)
			{
				try
				{
					test.BeginTile(parameters);
				}
				catch (Exception e)
				{
					throw new TestContainerException(test, tileEnvelope, e);
				}
			}
		}

		void ITestContainer.CompleteTile(TileState state,
		                                 IEnvelope tileEnvelope,
		                                 IEnvelope testRunEnvelope,
		                                 OverlappingFeatures overlappingFeatures)
		{
			Assert.ArgumentNotNull(tileEnvelope, nameof(tileEnvelope));

			_currentCache = overlappingFeatures;

			var parameters = new TileInfo(state, tileEnvelope, testRunEnvelope);

			if (_containerTests == null)
			{
				return;
			}

			var testIndex = 0;
			int testCount = _containerTests.Count;

			foreach (ContainerTest test in _containerTests)
			{
				try
				{
					using (UseProgressWatch(
						Step.TileCompleting, Step.TileCompleted, testIndex, testCount, test))
					{
						int origErrorEventCount = _errorEventCount;

						int testErrorCount = test.CompleteTile(parameters);

						int testErrorEventCount = _errorEventCount - origErrorEventCount;
						_totalErrorCount += testErrorCount;

						if (_totalErrorCount != _errorEventCount)
						{
							Assert.Fail(
								"Test '{0}' has inconsistent error count during tile completion: " +
								"returned count is {1:N0}, raised errors count is {2:N0}",
								test.GetType(), testErrorCount, testErrorEventCount);
						}

						_totalErrorCount = _errorEventCount;
					}

					testIndex++;
				}
				catch (Exception e)
				{
					throw new TestContainerException(test, tileEnvelope, e);
				}
			}
		}

		void ITestContainer.ClearErrors(double xMax, double yMax)
		{
			_errorAdministrator.Clear(xMax, yMax);
		}

		void ITestProgress.OnProgressChanged(Step step, int current, int total,
		                                     IEnvelope currentEnvelope, IEnvelope allBox)
		{
			if (ProgressChanged == null)
			{
				return;
			}

			var args = new ProgressArgs(step, current, total, currentEnvelope, allBox);
			ProgressChanged(this, args);
		}

		bool ITestContainer.Cancelled => _stopExecute;

		#endregion

		public void AddTest([NotNull] ITest test)
		{
			Assert.ArgumentNotNull(test, nameof(test));

			_tests.Add(test);
		}

		public event EventHandler<QaErrorEventArgs> QaError;

		public event EventHandler<RowEventArgs> TestingRow;

		public event ProgressHandler ProgressChanged;

		public int Execute([NotNull] IEnvelope boundingBox)
		{
			Assert.ArgumentNotNull(boundingBox, nameof(boundingBox));

			_spatialReference = GetSpatialReference();

			GeometryUtils.EnsureSpatialReference(boundingBox, _spatialReference,
			                                     out _executeBox);
			_executePolygon = null;

			return ExecuteCore();
		}

		public int Execute([NotNull] IPolygon polygon)
		{
			Assert.ArgumentNotNull(polygon, nameof(polygon));

			_spatialReference = GetSpatialReference();

			IPolygon polygonCopy = GeometryFactory.Clone(polygon);

			GeometryUtils.EnsureSpatialReference(polygonCopy, _spatialReference);

			// TODO consider weeding by a tolerance factor?
			// GeometryUtils.Weed(polygonCopy, 0.1);

			GeometryUtils.Simplify(polygonCopy, true);

			((ISpatialIndex) polygonCopy).AllowIndexing = true;

			_executePolygon = polygonCopy;

			_executeBox = polygonCopy.Envelope;

			return ExecuteCore();
		}

		public int Execute()
		{
			_spatialReference = GetSpatialReference();

			_executeBox = null;
			_executePolygon = null;

			return ExecuteCore();
		}

		// TODO not called. Remove? 
		public int Execute([NotNull] IList<ISelectionSet> selectionsList)
		{
			Assert.ArgumentNotNull(selectionsList, nameof(selectionsList));

			foreach (ITest test in _tests)
			{
				test.QaError += test_QaError;
			}

			try
			{
				_errorAdministrator = new QaErrorAdministrator();

				var errorCount = 0;

				foreach (ITest test in _tests)
				{
					var editingTest = test as IEditing;
					var origAllowEditing = false;
					if (editingTest != null)
					{
						origAllowEditing = editingTest.AllowEditing;
						editingTest.AllowEditing = _allowEditing;
					}

					try
					{
						foreach (ITable table in test.InvolvedTables)
						{
							foreach (ISelectionSet selectionSet in selectionsList)
							{
								if (selectionSet.Target != table)
								{
									continue;
								}

								const bool recycle = false;
								errorCount +=
									test.Execute(new EnumCursor(selectionSet, null, recycle));
							}
						}
					}
					finally
					{
						if (editingTest != null)
						{
							editingTest.AllowEditing = origAllowEditing;
						}
					}

					_errorAdministrator.Clear();
				}

				return errorCount;
			}
			finally
			{
				foreach (ITest test in _tests)
				{
					test.QaError -= test_QaError;
				}
			}
		}

		public void StopExecute()
		{
			_stopExecute = true;
		}

		private void OnProgressChanged(Step step, int current, int total, object tag)
		{
			if (ProgressChanged == null)
			{
				return;
			}

			var args = new ProgressArgs(step, current, total, tag);
			ProgressChanged(this, args);
		}

		[NotNull]
		private IEnumerable<TestRow> GetTestRows()
		{
			using (TestRowEnum enumerator = new TestRowEnum(
				this, _executeBox, _executePolygon, _tileSize))
			{
				foreach (TestRow testRow in enumerator.EnumTestRows())
				{
					yield return testRow;
				}
			}
		}

		private void OnQaError(QaErrorEventArgs e)
		{
			if (QaError != null)
			{
				QaError(this, e);
			}
		}

		private void OnProgressChanged(Step step, int current, int total, string info)
		{
			if (ProgressChanged == null)
			{
				return;
			}

			var args = new ProgressArgs(step, current, total, info);
			ProgressChanged(this, args);
		}

		private void OnProgressChanged(ProgressArgs args)
		{
			if (ProgressChanged == null)
			{
				return;
			}

			ProgressChanged(this, args);
		}

		private int ExecuteCore()
		{
			_stopExecute = false;
			// prepare
			_totalErrorCount = 0;
			_errorEventCount = 0;
			_cancelledErrorCount = 0;
			_errorAdministrator.Clear();

			IList<ITest> nonContainerTests;
			TestUtils.ClassifyTests(_tests, _allowEditing, out _containerTests,
			                        out nonContainerTests);

			// execute non-container tests
			Execute(nonContainerTests);

			if (_stopExecute)
			{
				return _totalErrorCount - _cancelledErrorCount;
			}

			_errorEventCount = _totalErrorCount;

			var failedTests = new List<ContainerTest>();

			// iterate
			foreach (TestRow testRow in GetTestRows())
			{
				// TODO when there is a selection of rows (passed using a 
				// new property or method overload), ignore test rows that are not in the selection? 
				// - currently all features in the selection box (as defined in the verification service)
				//   are tested, and errors from features not in the selection are ignored

				ContainerTest precedingContainerTest = null;
				var occurrence = 0;
				var executedTestIndex = 0;
				int applicableTestCount = testRow.ApplicableTests.Count;

				// TODO: drop occurance, use new class Class_with_ContainerTest_and_InvolvedTableIndex instead of containerTest
				foreach (ContainerTest containerTest in testRow.ApplicableTests)
				{
					if (failedTests.Contains(containerTest))
					{
						continue;
					}

					using (UseProgressWatch(Step.RowProcessing, Step.RowProcessed,
					                        executedTestIndex, applicableTestCount, containerTest))
					{
						if (precedingContainerTest != null &&
						    precedingContainerTest == containerTest)
						{
							// row will be passed to same test again, in a different role
							occurrence++;
						}
						else
						{
							occurrence = 0;
							precedingContainerTest = containerTest;
						}

						bool rowApplicable;
						int origErrorEventCount = _errorEventCount;
						int testErrorCount = TestRow(testRow, occurrence,
						                             containerTest, failedTests, out rowApplicable);
						if (! rowApplicable)
						{
							continue;
						}

						int testErrorEventCount = _errorEventCount - origErrorEventCount;

						_totalErrorCount += testErrorCount;

						if (_totalErrorCount != _errorEventCount)
						{
							Assert.Fail(
								"Test '{0}' has inconsistent error count for row {1}: " +
								"returned count is {2:N0}, raised errors count is {3:N0}",
								containerTest.GetType(),
								testRow.DataReference.GetLongDescription(),
								testErrorCount, testErrorEventCount);
						}

						if (_stopExecute)
						{
							return _totalErrorCount - _cancelledErrorCount;
						}
					}

					executedTestIndex++;
				}
			}

			Assert.AreEqual(_totalErrorCount, _errorEventCount,
			                "more errors reported ({0:N0}) than thrown ({1:N0})",
			                _totalErrorCount, _errorEventCount);

			OnProgressChanged(Step.Completed, 0, 1,
			                  string.Format("{0:N0} errors found",
			                                _totalErrorCount - _cancelledErrorCount));

			return _totalErrorCount - _cancelledErrorCount;
		}

		IDisposable ITestProgress.UseProgressWatch(Step startStep, Step endStep,
		                                           int current, int total, object tag)
		{
			return UseProgressWatch(startStep, endStep, current, total, tag);
		}

		private IDisposable UseProgressWatch(Step startStep, Step endStep,
		                                     int current, int total, object tag)
		{
			if (_progressWatch == null)
			{
				_progressWatch = new ProgressWatch(OnProgressChanged);
			}

			return _progressWatch.MakeTransaction(startStep, endStep, current, total, tag);
		}

		private void ReportErrorForFailedTest([NotNull] ITest test,
		                                      [CanBeNull] IDataReference dataReference,
		                                      [NotNull] string message)
		{
			var involvedRows = new List<InvolvedRow>();
			IGeometry errorGeometry = null;

			IRow row = null;
			var rowReference = dataReference as RowReference;
			if (rowReference != null)
			{
				row = rowReference.Row;
			}

			if (row != null)
			{
				involvedRows.Add(new InvolvedRow(row));
				errorGeometry = TestUtils.GetShapeCopy(row);
			}

			const bool assertionFailed = true;
			var qaError = new QaError(test, message, involvedRows, errorGeometry,
			                          null, null, assertionFailed);

			OnQaError(new QaErrorEventArgs(qaError));
		}

		[CanBeNull]
		public ISpatialReference GetSpatialReference()
		{
			var spatialReferences = new List<ISpatialReference>();

			foreach (ITest test in _tests)
			{
				spatialReferences.AddRange(TestUtils.GetSpatialReferences(test));
			}

			return TestUtils.GetUniqueSpatialReference(
				spatialReferences,
				requireEqualVerticalCoordinateSystems: false);
		}

		private void Execute([NotNull] ICollection<ITest> nonContainerTests)
		{
			Assert.ArgumentNotNull(nonContainerTests, nameof(nonContainerTests));

			var testIndex = 0;
			int testCount = nonContainerTests.Count;
			foreach (ITest nonContainerTest in nonContainerTests)
			{
				// Execute ITest
				nonContainerTest.QaError += test_QaError;
				nonContainerTest.TestingRow += test_TestingRow;

				try
				{
					using (
						UseProgressWatch(Step.ITestProcessing, Step.ITestProcessed, testIndex,
						                 testCount, nonContainerTest))
					{
						_totalErrorCount += ExecuteNonContainerTest(nonContainerTest);

						_errorAdministrator.Clear();
					}
				}
				catch (TestException exp)
				{
					_msg.Error(string.Format("Non-container test execution failed: {0}",
					                         exp.Message), exp);

					ReportErrorForFailedTest(exp.Test, null,
					                         string.Format("Test execution failed: {0}",
					                                       exp.Message));
				}
				catch (TestRowException exp)
				{
					_msg.Error(string.Format("Non-container test execution failed for row: {0}",
					                         exp.Message), exp);

					ReportErrorForFailedTest(exp.Test, new RowReference(exp.Row, recycled: false),
					                         string.Format("Test execution failed: {0}",
					                                       exp.Message));
				}
				catch (Exception exp)
				{
					_msg.Error(string.Format("Non-container test execution failed: {0}",
					                         exp.Message), exp);

					throw new TestContainerException(nonContainerTest, exp);
				}
				finally
				{
					testIndex++;
					nonContainerTest.QaError -= test_QaError;
					nonContainerTest.TestingRow -= test_TestingRow;
				}

				if (_stopExecute)
				{
					return;
				}
			}
		}

		private int ExecuteNonContainerTest([NotNull] ITest nonContainerTest)
		{
			Assert.ArgumentNotNull(nonContainerTest, nameof(nonContainerTest));

			if (_forceFullScanForNonContainerTests || _executeBox == null)
			{
				return nonContainerTest.Execute();
			}

			return _executePolygon == null
				       ? nonContainerTest.Execute(_executeBox)
				       : nonContainerTest.Execute(_executePolygon);
		}

		private int TestRow([NotNull] TestRow testRow, int occurance,
		                    [NotNull] ContainerTest containerTest,
		                    [NotNull] ICollection<ContainerTest> failedTests,
		                    out bool applicable)
		{
			applicable = true;

			IDataReference dataReference = testRow.DataReference;
			try
			{
				return dataReference.Execute(containerTest, occurance, out applicable);
			}
			catch (TestException e)
			{
				_msg.Error(string.Format("Container test execution failed: {0}",
				                         e.Message), e);

				failedTests.Add(containerTest);

				ReportErrorForFailedTest(containerTest, dataReference,
				                         string.Format("Test failed: {0}",
				                                       e.Message));
				return 0;
			}
			catch (TestRowException e)
			{
				_msg.Error(string.Format("Container test execution failed: {0}",
				                         e.Message), e);

				ReportErrorForFailedTest(containerTest, dataReference,
				                         string.Format("Test failed for row: {0}",
				                                       e.Message));
				return 0;
			}
			catch (Exception e)
			{
				_msg.Error(string.Format("Container test execution failed: {0}",
				                         e.Message), e);

				var rowReference = dataReference as RowReference;
				if (rowReference != null)
				{
					throw new TestContainerException(containerTest, rowReference.Row, e);
				}

				throw new TestContainerException(containerTest, dataReference.Extent, e);
			}
		}

		#region event handlers

		private void test_QaError(object sender, QaErrorEventArgs errorEventArgs)
		{
			_errorEventCount++;

			bool cancel = _errorAdministrator.IsDuplicate(errorEventArgs.QaError);

			if (! cancel)
			{
				OnQaError(errorEventArgs);
				cancel = errorEventArgs.Cancel;
			}

			if (! cancel)
			{
				_errorAdministrator.Add(errorEventArgs.QaError, isKnonwnNotDuplicate: true);
			}
			else
			{
				_cancelledErrorCount++;
			}

			if (! _keepErrorGeometry)
			{
				errorEventArgs.QaError.ReduceGeometry();
			}
		}

		private void test_TestingRow(object sender, RowEventArgs args)
		{
			if (TestingRow == null)
			{
				args.Cancel = false;
			}
			else
			{
				TestingRow(sender, args);
			}
		}

		#endregion
	}
}
