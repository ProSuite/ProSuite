using System;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.AO.Test.TestSupport;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Testing;
using ProSuite.QA.Tests.Test.TestRunners;
using TestCategory = ProSuite.Commons.Test.TestCategory;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaSimpleGeometryTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanGetZigZagNonPlanarLineError()
		{
			// the source polyline visits the same points several times by going back and forth
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			var fc = new FeatureClassMock("Fc",
			                              esriGeometryType.esriGeometryPolyline,
			                              1,
			                              esriFeatureType.esriFTSimple, lv95);

			IPolyline sourcePolyline = GeometryFactory.CreatePolyline(
				lv95,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(30, 10));

			IFeature f = fc.CreateFeature(sourcePolyline);

			var test = new QaSimpleGeometry(ReadOnlyTableFactory.Create(fc), true);
			var runner = new QaTestRunner(test);
			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);

			test = new QaSimpleGeometry(ReadOnlyTableFactory.Create(fc), false);
			runner = new QaTestRunner(test) { KeepGeometry = true };

			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);

			Assert.AreEqual(2, ((IPointCollection) runner.ErrorGeometries[0]).PointCount);
		}

		[Test]
		[Category(TestCategory.FixMe)]
		public void Gen2847_NonZawarePolylineWithInteriorLoops()
		{
			string path = TestDataPreparer.FromDirectory()
			                              .GetPath("DKM25_HOEHENKURVE_OID_2178.xml");

			var nonZAwarePolyline = (IPolyline) GeometryUtils.FromXmlFile(path);

			var fc = new FeatureClassMock("Contours", esriGeometryType.esriGeometryPolyline,
			                              1,
			                              esriFeatureType.esriFTSimple,
			                              nonZAwarePolyline.SpatialReference, hasZ: false,
			                              hasM: false);

			IFeature f = fc.CreateFeature(nonZAwarePolyline);

			const double toleranceFactor = 1.0;
			var test =
				new QaSimpleGeometry(ReadOnlyTableFactory.Create(fc), false, toleranceFactor);
			var runner = new QaTestRunner(test) { KeepGeometry = true };

			runner.Execute(f);

			Assert.AreEqual(1, runner.Errors.Count);
			Assert.AreEqual(3, ((IPointCollection) runner.ErrorGeometries[0]).PointCount);
		}

		[Test]
		public void CanGetChangedDuplicateVerticesPolygonWithSameStartPoint()
		{
			// the source polyline visits the same points several times by going back and forth
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			var fc = new FeatureClassMock("Fc",
			                              esriGeometryType.esriGeometryPolygon,
			                              1,
			                              esriFeatureType.esriFTSimple, lv95);

			IPolyline polyline = GeometryFactory.CreatePolyline(
				lv95,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(20, 0),
				GeometryFactory.CreatePoint(30, 10));

			IPolygon sourcePolygon = GeometryFactory.CreatePolygon(polyline);

			IFeature f = fc.CreateFeature(sourcePolygon);

			var test = new QaSimpleGeometry(ReadOnlyTableFactory.Create(fc), false);
			var runner = new QaTestRunner(test) { KeepGeometry = true };

			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);

			Assert.AreEqual(2, ((IPointCollection) runner.ErrorGeometries[0]).PointCount);

			GeometryUtils.Simplify(sourcePolygon);

			runner = new QaTestRunner(test);

			runner.Execute(f);
			Assert.AreEqual(0, runner.Errors.Count);
		}

		[Test]
		[Category(TestCategory.FixMe)]
		public void CanGetChangedDuplicateVerticesPolylineSelfIntersecting()
		{
			// the source polyline visits the same points several times by going back and forth
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			var fc = new FeatureClassMock("Fc",
			                              esriGeometryType.esriGeometryPolyline,
			                              1,
			                              esriFeatureType.esriFTSimple, lv95);

			IPolyline polyline = GeometryFactory.CreatePolyline(
				lv95,
				GeometryFactory.CreatePoint(0, 0),
				GeometryFactory.CreatePoint(10, 0),
				GeometryFactory.CreatePoint(10, 10),
				GeometryFactory.CreatePoint(5, 0),
				GeometryFactory.CreatePoint(20, 10),
				GeometryFactory.CreatePoint(30, 10));

			IFeature f = fc.CreateFeature(polyline);

			var test = new QaSimpleGeometry(ReadOnlyTableFactory.Create(fc), false);
			var runner = new QaTestRunner(test) { KeepGeometry = true };

			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);

			IGeometry errorGeometry = runner.ErrorGeometries[0];
			Console.WriteLine(GeometryUtils.ToString(errorGeometry));
			Assert.AreEqual(2, ((IPointCollection) errorGeometry).PointCount);

			GeometryUtils.Simplify(polyline);

			runner = new QaTestRunner(test);

			// The error remains...
			runner.Execute(f);
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanReportSelfIntersectingMultipatchRings_ChateauDeDardagny()
		{
			// TLM_GEBAEUDE Chateau de Dardagny: two of the eight ring groups are figure-8
			// (bowtie) rings, i.e. a single ring with a 0-dimensional (crossing)
			// self-intersection. QaSimpleGeometry must report these as self-intersections.
			IFeature feature = CreateMultipatchFeature("chateau_de_dardagny.wkb");

			var test = new QaSimpleGeometry(
				ReadOnlyTableFactory.Create((IFeatureClass) feature.Class));
			var runner = new QaTestRunner(test) { KeepGeometry = true };

			runner.Execute(feature);

			int selfIntersectionCount = CountErrors(runner, "SimpleGeometry.SelfIntersection");
			int interiorRingCount = CountErrors(runner, "SimpleGeometry.InteriorRingNotInside");

			Console.WriteLine(@"SelfIntersection errors: {0}, InteriorRingNotInside errors: {1}",
			                  selfIntersectionCount, interiorRingCount);

			Assert.AreEqual(2, selfIntersectionCount,
			                "Expected the two bowtie rings to be reported as self-intersections");
			Assert.AreEqual(0, interiorRingCount,
			                "Did not expect any interior-ring errors for this feature");

			// The error geometry is the actual self-intersection location (the crossing
			// point(s)) as a multipoint - not the whole ring polyline.
			foreach (IGeometry errorGeometry in runner.ErrorGeometries)
			{
				Assert.AreEqual(esriGeometryType.esriGeometryMultipoint,
				                errorGeometry.GeometryType);

				int pointCount = ((IPointCollection) errorGeometry).PointCount;
				Assert.GreaterOrEqual(pointCount, 1, "Expected at least one crossing point");
				Assert.LessOrEqual(pointCount, 2,
				                   "Expected only the crossing point(s), not the whole ring");
			}
		}

		[Test]
		public void CanReportInteriorRingNotInsideMultipatch_Dennlerstrasse()
		{
			// TLM_GEBAEUDE Dennlerstrasse: one ring group carries two interior rings; one is a
			// real coplanar hole, the other is 2.69 m off the face's plane (a different face
			// mis-stored as an inner ring). QaSimpleGeometry must report the latter as an
			// interior ring that is not inside its exterior ring.
			IFeature feature = CreateMultipatchFeature("dennlerstrasse.wkb");

			var test = new QaSimpleGeometry(
				ReadOnlyTableFactory.Create((IFeatureClass) feature.Class));
			var runner = new QaTestRunner(test) { KeepGeometry = true };

			runner.Execute(feature);

			int interiorRingCount = CountErrors(runner, "SimpleGeometry.InteriorRingNotInside");
			int selfIntersectionCount = CountErrors(runner, "SimpleGeometry.SelfIntersection");

			Console.WriteLine(@"InteriorRingNotInside errors: {0}, SelfIntersection errors: {1}",
			                  interiorRingCount, selfIntersectionCount);

			Assert.AreEqual(1, interiorRingCount,
			                "Expected the non-coplanar interior ring to be reported");
			Assert.AreEqual(0, selfIntersectionCount,
			                "Did not expect any self-intersection errors for this feature");
		}

		[NotNull]
		private static IFeature CreateMultipatchFeature([NotNull] string wkbFileName)
		{
			string path = TestDataPreparer.FromDirectory().GetPath(wkbFileName);

			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);
			((ISpatialReferenceResolution) spatialReference).XYResolution[true] = 0.0001;
			((ISpatialReferenceTolerance) spatialReference).XYTolerance = 0.001;

			// Load through the Geom reader (which faithfully preserves interior rings) and
			// rebuild the ArcObjects multipatch from the ring groups. The ArcObjects
			// WkbGeometryReader flattens the MultiSurface's interior rings into separate
			// exterior rings, which would defeat the interior-ring test.
			var polyhedron = (Polyhedron) GeomUtils.FromWkbFile(path, out _);

			IMultiPatch prototype = new MultiPatchClass { SpatialReference = spatialReference };
			((IZAware) prototype).ZAware = true;

			IMultiPatch multipatch =
				GeometryConversionUtils.CreateMultipatch(polyhedron.RingGroups, prototype);
			multipatch.SpatialReference = spatialReference;

			var featureClassMock = new FeatureClassMock(
				"mock", esriGeometryType.esriGeometryMultiPatch, 1,
				esriFeatureType.esriFTSimple, spatialReference);

			return featureClassMock.CreateFeature(multipatch);
		}

		private static int CountErrors([NotNull] QaTestRunner runner,
		                               [NotNull] string issueCodeId)
		{
			return runner.Errors.Count(e => e.IssueCode != null && e.IssueCode.ID == issueCodeId);
		}

		[Test]
		public void CanReduceToleranceIfNormal()
		{
			const double toleranceFactor = 0.4;

			const double resolution = 0.0001;
			const double tolerance = resolution * 10;

			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			SpatialReferenceUtils.SetXYDomain(spatialReference,
			                                  0, 0, 5000000, 5000000,
			                                  resolution, tolerance);

			const bool allowResolutionChange = false;
			CanReduceTolerance(spatialReference, toleranceFactor, allowResolutionChange);
		}

		[Test]
		public void CanReduceToleranceIfTooSmall()
		{
			const double toleranceFactor = 0.1;

			const double resolution = 0.0001;
			const double tolerance = resolution * 2;

			ISpatialReference spatialReference =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			SpatialReferenceUtils.SetXYDomain(spatialReference,
			                                  0, 0, 5000000, 5000000,
			                                  resolution, tolerance);

			const bool allowResolutionChange = true;
			CanReduceTolerance(spatialReference, toleranceFactor, allowResolutionChange);
		}

		private static void CanReduceTolerance([NotNull] ISpatialReference spatialReference,
		                                       double toleranceFactor,
		                                       bool allowResolutionChange)
		{
			var origTolerance = (ISpatialReferenceTolerance) spatialReference;
			var origResolution = (ISpatialReferenceResolution) spatialReference;

			double xmin;
			double xmax;
			double ymin;
			double ymax;
			spatialReference.GetDomain(out xmin, out xmax, out ymin, out ymax);

			Console.WriteLine(@"Original tolerance: {0}", origTolerance.XYTolerance);
			Console.WriteLine(@"Original resolution: {0}",
			                  origResolution.XYResolution[true]);
			Console.WriteLine(@"Original domain: {0} {1} {2} {3}", xmin, ymin, xmax, ymax);

			ISpatialReference reduced = GetReducedToleranceSpatialReference(spatialReference,
				toleranceFactor);

			var reducedTolerance = (ISpatialReferenceTolerance) reduced;
			var reducedResolution = (ISpatialReferenceResolution) reduced;

			double xmin2;
			double xmax2;
			double ymin2;
			double ymax2;
			reduced.GetDomain(out xmin2, out xmax2, out ymin2, out ymax2);

			Console.WriteLine(@"Reduced tolerance: {0}", reducedTolerance.XYTolerance);
			Console.WriteLine(@"Reduced resolution: {0}",
			                  reducedResolution.XYResolution[true]);
			Console.WriteLine(@"Reduced domain: {0} {1} {2} {3}", xmin2, ymin2, xmax2, ymax2);

			Assert.AreEqual(origTolerance.XYTolerance * toleranceFactor,
			                reducedTolerance.XYTolerance);
			Assert.IsTrue(reducedTolerance.XYToleranceValid ==
			              esriSRToleranceEnum.esriSRToleranceOK);
			Assert.AreEqual(xmin, xmin2);
			Assert.AreEqual(ymin, ymin2);

			if (! allowResolutionChange)
			{
				Assert.AreEqual(origResolution.get_XYResolution(true),
				                reducedResolution.XYResolution[true]);
				Assert.AreEqual(xmax, xmax2);
				Assert.AreEqual(ymax, ymax2);
			}
		}

		[NotNull]
		private static ISpatialReference GetReducedToleranceSpatialReference(
			[NotNull] ISpatialReference spatialReference, double toleranceFactor)
		{
			var result = (ISpatialReference) ((IClone) spatialReference).Clone();

			var srefTolerance = (ISpatialReferenceTolerance) result;
			srefTolerance.XYTolerance = srefTolerance.XYTolerance * toleranceFactor;

			if (srefTolerance.XYToleranceValid == esriSRToleranceEnum.esriSRToleranceIsTooSmall)
			{
				var srefResolution = (ISpatialReferenceResolution) result;

				const bool standardUnits = true;
				double resolution = srefResolution.XYResolution[standardUnits];
				srefResolution.set_XYResolution(standardUnits, resolution * toleranceFactor);
			}

			return result;
		}
	}
}
