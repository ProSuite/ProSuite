using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaMpSinglePartFootprintTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
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

		// TODO tests for behavior below tolerance/resolution

		[NotNull]
		private static FeatureClassMock CreateFeatureClassMock()
		{
			const bool defaultXyDomain = true;
			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, defaultXyDomain);
			((ISpatialReferenceResolution) spatialReference).XYResolution[true] = 0.0001;
			((ISpatialReferenceTolerance) spatialReference).XYTolerance = 0.001;

			const bool hasZ = true;
			const bool hasM = false;
			return new FeatureClassMock(1, "mock",
			                            esriGeometryType.esriGeometryMultiPatch,
			                            esriFeatureType.esriFTSimple,
			                            spatialReference,
			                            hasZ,
			                            hasM);
		}
	}
}
