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
	public class QaMpNonIntersectingRingFootprintsTest
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

			const bool allowIntersectionsForDifferentPointIds = true;
			var test = new QaMpNonIntersectingRingFootprints(
				ReadOnlyTableFactory.Create(featureClassMock),
				allowIntersectionsForDifferentPointIds);
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanReportNonUniquePointIds()
		{
			FeatureClassMock featureClassMock = CreateFeatureClassMock();

			var construction = new MultiPatchConstruction();

			const int id1 = 1;
			const int id2 = 2;

			construction.StartOuterRing(0, 0, 0, id1)
			            .Add(10, 0, 0, id2)
			            .Add(10, 10, 0, id2)
			            .Add(0, 10, 0, id2);

			IFeature row = featureClassMock.CreateFeature(construction.MultiPatch);

			const bool allowIntersectionsForDifferentPointIds = true;
			var test = new QaMpNonIntersectingRingFootprints(
				ReadOnlyTableFactory.Create(featureClassMock),
				allowIntersectionsForDifferentPointIds);
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			// only reported if allowIntersectionsForDifferentPointIds == true
			AssertUtils.OneError(runner,
			                     "MpNonIntersectingRingFootprints.PointIdNotUniqueWithinFace");
		}

		[Test]
		public void CanReportIntersectingFootPrints()
		{
			FeatureClassMock featureClassMock = CreateFeatureClassMock();

			var construction = new MultiPatchConstruction();

			const int id1 = 1;

			construction.StartOuterRing(0, 0, 0, id1)
			            .Add(10, 0, 0, id1)
			            .Add(10, 10, 0, id1)
			            .Add(0, 10, 0, id1)
			            .StartOuterRing(9, 0, 10, id1)
			            .Add(20, 0, 10, id1)
			            .Add(20, 10, 10, id1)
			            .Add(9, 10, 10, id1);

			IFeature row = featureClassMock.CreateFeature(construction.MultiPatch);

			const bool allowIntersectionsForDifferentPointIds = true;
			var test = new QaMpNonIntersectingRingFootprints(
				ReadOnlyTableFactory.Create(featureClassMock),
				allowIntersectionsForDifferentPointIds);
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			AssertUtils.OneError(runner,
			                     "MpNonIntersectingRingFootprints.RingFootprintsIntersect");
		}

		[Test]
		public void CanAllowIntersectingFootPrintsWithDifferentPointIds()
		{
			FeatureClassMock featureClassMock = CreateFeatureClassMock();

			var construction = new MultiPatchConstruction();

			const int id1 = 1;
			const int id2 = 2;

			construction.StartOuterRing(0, 0, 0, id1)
			            .Add(10, 0, 0, id1)
			            .Add(10, 10, 0, id1)
			            .Add(0, 10, 0, id1)
			            .StartOuterRing(9, 0, 10, id2)
			            .Add(20, 0, 10, id2)
			            .Add(20, 10, 10, id2)
			            .Add(9, 10, 10, id2);

			IFeature row = featureClassMock.CreateFeature(construction.MultiPatch);

			const bool allowIntersectionsForDifferentPointIds = true;
			var test = new QaMpNonIntersectingRingFootprints(
				ReadOnlyTableFactory.Create(featureClassMock),
				allowIntersectionsForDifferentPointIds);
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanReportIntersectingFootPrintsWithDifferentPointIds()
		{
			FeatureClassMock featureClassMock = CreateFeatureClassMock();

			var construction = new MultiPatchConstruction();

			const int id1 = 1;
			const int id2 = 2;

			construction.StartOuterRing(0, 0, 0, id1)
			            .Add(10, 0, 0, id1)
			            .Add(10, 10, 0, id1)
			            .Add(0, 10, 0, id1)
			            .StartOuterRing(9, 0, 10, id2)
			            .Add(20, 0, 10, id2)
			            .Add(20, 10, 10, id2)
			            .Add(9, 10, 10, id2);

			IFeature row = featureClassMock.CreateFeature(construction.MultiPatch);

			const bool allowIntersectionsForDifferentPointIds = false;
			// don't allow even for different point ids
			var test = new QaMpNonIntersectingRingFootprints(
				ReadOnlyTableFactory.Create(featureClassMock),
				allowIntersectionsForDifferentPointIds);
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			AssertUtils.OneError(runner,
			                     "MpNonIntersectingRingFootprints.RingFootprintsIntersect");
		}

		[Test]
		public void CanAllowMultipartFootPrintTouchingInPoint()
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

			const bool allowIntersectionsForDifferentPointIds = true;
			var test = new QaMpNonIntersectingRingFootprints(
				ReadOnlyTableFactory.Create(featureClassMock),
				allowIntersectionsForDifferentPointIds);
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			AssertUtils.NoError(runner);
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

			IFeature row1 = featureClassMock.CreateFeature(construction.MultiPatch);

			const bool allowIntersectionsForDifferentPointIds = true;
			var test = new QaMpNonIntersectingRingFootprints(
				ReadOnlyTableFactory.Create(featureClassMock),
				allowIntersectionsForDifferentPointIds);
			var runner = new QaTestRunner(test);
			runner.Execute(row1);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowIntersectingFootPrintsWithVerticalWall()
		{
			FeatureClassMock featureClassMock = CreateFeatureClassMock();

			var construction = new MultiPatchConstruction();

			// second ring is vertical wall, intersecting the first ring
			construction.StartOuterRing(0, 0, 0)
			            .Add(10, 0, 0)
			            .Add(10, 10, 0)
			            .Add(0, 10, 0)
			            .StartOuterRing(5, 5, 100)
			            .Add(15, 5, 100)
			            .Add(15, 5, 110)
			            .Add(5, 5, 110);

			IFeature row = featureClassMock.CreateFeature(construction.MultiPatch);

			const bool allowIntersectionsForDifferentPointIds = true;
			var test = new QaMpNonIntersectingRingFootprints(
				ReadOnlyTableFactory.Create(featureClassMock),
				allowIntersectionsForDifferentPointIds);
			var runner = new QaTestRunner(test);
			runner.Execute(row);

			AssertUtils.NoError(runner);
		}

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
