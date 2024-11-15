using System;
using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Testing;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Test.Transformer;
using ProSuite.QA.Tests.Transformers;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMinAreaTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanCheckRealWorldMultipatchesTop5659()
		{
			string path = TestDataPreparer.ExtractZip("GebkoerperSmallAreas.gdb.zip").GetPath();

			IFeatureWorkspace featureWorkspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);
			IFeatureClass buildings =
				DatasetUtils.OpenFeatureClass(featureWorkspace, "geometry_issue");

			int errorCount = GetMinAreaErrorCount(buildings, 0.2, false);

			int expected = 113;
			Assert.AreEqual(expected, errorCount);
		}

		[Test]
		public void CanCheckMinAreaVerticalWallsPerPartTop5692()
		{
			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithDissolvableRingsAndInteriorWalls.xml");
			var test = new QaMinArea(
				ReadOnlyTableFactory.Create((IFeatureClass) mockFeature.Class), 0.5, false);

			var runner = new QaTestRunner(test);

			int errorCount = runner.Execute(new List<IRow> { mockFeature });

			Assert.AreEqual(0, errorCount);

			test = new QaMinArea(
				ReadOnlyTableFactory.Create((IFeatureClass) mockFeature.Class), 0.5, true);

			runner = new QaTestRunner(test);

			errorCount = runner.Execute(new List<IRow> { mockFeature });

			// Finds the almost-vertical ring:
			Assert.AreEqual(1, errorCount);
		}

		[Test]
		[Category(Commons.Test.TestCategory.FixMe)]
		public void CanCheckRealWorldMultipatchesViaFootprintTop5659()
		{
			string path = TestDataPreparer.ExtractZip("GebkoerperSmallAreas.gdb.zip").GetPath();

			IFeatureWorkspace featureWorkspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);
			IFeatureClass buildings =
				DatasetUtils.OpenFeatureClass(featureWorkspace, "geometry_issue");

			ReadOnlyFeatureClass roBuildings = ReadOnlyTableFactory.Create(buildings);
			TransformedFeatureClass footprintClass = GetFootprintClass(roBuildings);

			int errorCount = GetMinAreaErrorCount(footprintClass, 0.2, true);

			// Just a few are actually small,
			int expected = 12;
			Assert.AreEqual(expected, errorCount);

			// most footprints are empty (empty is NOT area 0!, but ignored)
			errorCount = GetNonEmptyGeometryErrorCount(footprintClass);
			Assert.AreEqual(112, errorCount);
		}

		private static int GetMinAreaErrorCount(IFeatureClass buildings, double limit, bool perPart)
		{
			var test = new QaMinArea(
				ReadOnlyTableFactory.Create(buildings), limit, perPart);

			var runner = new QaTestRunner(test);

			Stopwatch watch = Stopwatch.StartNew();

			int errorCount = 0;
			try
			{
				errorCount = runner.Execute();
			}
			finally
			{
				watch.Stop();
				Console.WriteLine("Tested MinArea in {0}s", watch.Elapsed.TotalSeconds);
			}

			return errorCount;
		}

		private static int GetNonEmptyGeometryErrorCount(IFeatureClass buildings)
		{
			var test = new QaNonEmptyGeometry(
				ReadOnlyTableFactory.Create(buildings));

			var runner = new QaTestRunner(test);

			Stopwatch watch = Stopwatch.StartNew();

			int errorCount = 0;
			try
			{
				errorCount = runner.Execute();
			}
			finally
			{
				watch.Stop();
				Console.WriteLine("Tested NonEmptyGeometry in {0}s", watch.Elapsed.TotalSeconds);
			}

			return errorCount;
		}

		private static TransformedFeatureClass GetFootprintClass(IReadOnlyFeatureClass roBuildings)
		{
			var transformer1 = new TrFootprint(roBuildings);

			TransformedFeatureClass featureClass = transformer1.GetTransformed();

			Assert.NotNull(featureClass.BackingDataset);
			var transformedBackingDataset =
				(TransformedBackingDataset) featureClass.BackingDataset;

			transformedBackingDataset.DataContainer = new UncachedDataContainer(roBuildings.Extent);

			var srTolerance =
				(ISpatialReferenceTolerance) ((IClone) featureClass.SpatialReference).Clone();

			srTolerance.XYTolerance = 0.0001;

			return featureClass;
		}
	}
}
