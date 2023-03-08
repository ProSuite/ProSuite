using System;
using System.Diagnostics;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Testing;
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
			TestUtils.ConfigureUnitTestLogging();
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

			WKSEnvelope wksEnvelope = WksGeometryUtils.CreateWksEnvelope(
				roBuildings.Extent.XMin, roBuildings.Extent.YMin,
				roBuildings.Extent.XMax, roBuildings.Extent.YMax);

			TransformedFeatureClass featureClass = transformer1.GetTransformed();

			Assert.NotNull(featureClass.BackingDataset);
			var transformedBackingDataset =
				(TransformedBackingDataset) featureClass.BackingDataset;

			transformedBackingDataset.DataContainer = new UncachedDataContainer(wksEnvelope);

			var srTolerance =
				(ISpatialReferenceTolerance) ((IClone) featureClass.SpatialReference).Clone();

			srTolerance.XYTolerance = 0.0001;

			return featureClass;
		}
	}
}
