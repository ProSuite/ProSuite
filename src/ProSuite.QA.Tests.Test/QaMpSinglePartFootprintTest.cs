using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Security;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Testing;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.Test.Testing.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMpSinglePartFootprintTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.ConfigureUnitTestLogging();
			Commons.AO.Test.TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			Commons.AO.Test.TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanAllowSimpleMultiPatch()
		{
			FeatureClassMock featureClassMock = CreateFeatureClassMock();

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(0, 0, 0)
			            .Add(10, 0, 0)
			            .Add(10, 10, 0)
			            .Add(0, 10, 0);

			IFeature row = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpSinglePartFootprint(
				ReadOnlyTableFactory.Create(featureClassMock));
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanReportMultipartFootPrint()
		{
			FeatureClassMock featureClassMock = CreateFeatureClassMock();

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(0, 0, 0)
			            .Add(10, 0, 0)
			            .Add(10, 10, 0)
			            .Add(0, 10, 0)
			            .StartOuterRing(20, 0, 0)
			            .Add(30, 0, 0)
			            .Add(30, 10, 0)
			            .Add(20, 10, 0);

			IFeature row = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpSinglePartFootprint(
				ReadOnlyTableFactory.Create(featureClassMock));
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			AssertUtils.OneError(runner, "MpSinglePartFootprint.FootprintHasMultipleParts");
		}

		[Test]
		public void CanReportMultipartFootPrintVerticalWall()
		{
			FeatureClassMock featureClassMock = CreateFeatureClassMock();

			var construction = new MultiPatchConstruction();

			// second ring is vertical wall, disjoint from first ring
			construction.StartOuterRing(0, 0, 0)
			            .Add(10, 0, 0)
			            .Add(10, 10, 0)
			            .Add(0, 10, 0)
			            .StartOuterRing(20, 0, 100)
			            .Add(30, 0, 100)
			            .Add(30, 0, 110)
			            .Add(20, 0, 110);

			IFeature row = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpSinglePartFootprint(
				ReadOnlyTableFactory.Create(featureClassMock));
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			AssertUtils.OneError(runner, "MpSinglePartFootprint.FootprintHasMultipleParts");
		}

		[Test]
		public void CanReportMultipartFootPrintTouchingInPoint()
		{
			FeatureClassMock featureClassMock = CreateFeatureClassMock();

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(0, 0, 0)
			            .Add(10, 0, 0)
			            .Add(10, 10, 0)
			            .Add(0, 10, 0)
			            .StartOuterRing(10, 10, 0)
			            .Add(20, 10, 0)
			            .Add(20, 20, 0)
			            .Add(10, 20, 0);

			IFeature row = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpSinglePartFootprint(
				ReadOnlyTableFactory.Create(featureClassMock));
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			AssertUtils.OneError(runner, "MpSinglePartFootprint.FootprintHasMultipleParts");
		}

		[Test]
		public void CanAllowMultipartFootPrintTouchingInLine()
		{
			FeatureClassMock featureClassMock = CreateFeatureClassMock();

			var construction = new MultiPatchConstruction();

			construction.StartOuterRing(0, 0, 0)
			            .Add(10, 0, 0)
			            .Add(10, 10, 0)
			            .Add(0, 10, 0)
			            .StartOuterRing(10, 5, 100)
			            .Add(20, 5, 100)
			            .Add(20, 15, 100)
			            .Add(10, 15, 100);

			IFeature row = featureClassMock.CreateFeature(construction.MultiPatch);

			var test = new QaMpSinglePartFootprint(
				ReadOnlyTableFactory.Create(featureClassMock));
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			AssertUtils.NoError(runner);
		}

		[Test]
		[HandleProcessCorruptedStateExceptions]
		[SecurityCritical]
		[Category(TestCategory.Repro)]
		public void CanCheckRealWorldBuildingsTop5643()
		{
			string path = TestDataPreparer.ExtractZip("GebZueriberg.gdb.zip").GetPath();

			IFeatureWorkspace featureWorkspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);
			IFeatureClass buildings =
				DatasetUtils.OpenFeatureClass(featureWorkspace, "TLM_GEBAEUDE");

			var test = new QaMpSinglePartFootprint(
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
				Console.WriteLine("Finished successfully in {0}s", watch.Elapsed.TotalSeconds);
			}

			// 2 features contain duplicate interior rings, one of which has
			// positive orientation. AO-multipatch footprint does not report
			// an island in these cases.
			int expected = IntersectionUtils.UseCustomIntersect ? 32 : 30;
			Assert.AreEqual(expected, errorCount);
		}

		[Test]
		public void CanTestMinusculeMultipatchTop5939()
		{
			// This tests a work-around for a failure that started with 11.2 or 11.3 during the
			// buffer operation of non-simple geometries.
			string path = TestDataPreparer.ExtractZip("issue_tlmqa-219.gdb.zip").GetPath();

			IFeatureWorkspace featureWorkspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);
			IFeatureClass buildings =
				DatasetUtils.OpenFeatureClass(featureWorkspace, "tlm_gebaeude_errors");

			var test = new QaMpSinglePartFootprint(ReadOnlyTableFactory.Create(buildings));
			test.ResolutionFactor = 1;
			var runner = new QaTestRunner(test);

			Stopwatch watch = Stopwatch.StartNew();

			int errorCount;
			try
			{
				errorCount = runner.Execute();
			}
			finally
			{
				watch.Stop();
				Console.WriteLine("Finished successfully in {0}s", watch.Elapsed.TotalSeconds);
			}

			int expected = 0;
			Assert.AreEqual(expected, errorCount);
		}

		// TODO tests for behavior below tolerance/resolution

		[NotNull]
		private static FeatureClassMock CreateFeatureClassMock()
		{
			const bool defaultXyDomain = true;
			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, defaultXyDomain);
			((ISpatialReferenceResolution) spatialReference).XYResolution[true] = 0.0001;
			((ISpatialReferenceTolerance) spatialReference).XYTolerance = 0.001;

			return new FeatureClassMock("mock",
			                            esriGeometryType.esriGeometryMultiPatch,
			                            1,
			                            esriFeatureType.esriFTSimple,
			                            spatialReference);
		}
	}
}
