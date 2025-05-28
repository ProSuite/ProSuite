using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	public class QaIsCoveredByOtherTest
	{
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("QaIsCoveredByOtherTest");
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanDetectUncoveredArea()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanDetectUncoveredArea",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 201)
				                 .LineTo(200, 201)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				ReadOnlyTableFactory.Create(coveringClass),
				ReadOnlyTableFactory.Create(coveredClass));
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner, "CoveredByOther.NotFullyCovered");
		}

		[Test]
		public void CanDetectUncoveredAreaWithManyCovering()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanDetectUncoveredAreaWithManyCovering",
			                            out coveringClass,
			                            out coveredClass);

			StoreGeometrySlices(coveringClass);

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 201)
				                 .LineTo(200, 201)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				ReadOnlyTableFactory.Create(coveringClass),
				ReadOnlyTableFactory.Create(coveredClass));
			var runner = new QaContainerTestRunner(10000, test);

			Stopwatch watch = Stopwatch.StartNew();
			runner.Execute(verificationEnvelope);
			watch.Stop();

			AssertUtils.OneError(runner, "CoveredByOther.NotFullyCovered");
			Console.WriteLine($"Executed in {watch.ElapsedMilliseconds}ms");
		}

		[Test]
		public void CanCheckCoveredAreaWithManyCovering()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanCheckCoveredAreaWithManyCovering",
			                            out coveringClass,
			                            out coveredClass);

			StoreGeometrySlices(coveringClass);

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				ReadOnlyTableFactory.Create(coveringClass),
				ReadOnlyTableFactory.Create(coveredClass));
			var runner = new QaContainerTestRunner(10000, test);

			Stopwatch watch = Stopwatch.StartNew();
			runner.Execute(verificationEnvelope);
			watch.Stop();

			AssertUtils.NoError(runner);
			Console.WriteLine($"Executed in {watch.ElapsedMilliseconds}ms");
		}

		[Test]
		public void CanDetectUncoveredInteriorVertices()
		{
			var testName = MethodBase.GetCurrentMethod()?.Name;

			IFeatureClass coveringClass = CreateFeatureClass(
				string.Format("{0}_covering", testName),
				esriGeometryType.esriGeometryPolyline);
			IFeatureClass coveredClass = CreateFeatureClass(
				string.Format("{0}_covered", testName),
				esriGeometryType.esriGeometryPolyline);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartLine(200, 100)
				                 .LineTo(200, 200)
				                 .Curve;
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartLine(100, 100)
				                 .LineTo(200, 100) // interior vertex, covered
				                 .LineTo(300, 100) // interior vertex, not covered
				                 .LineTo(400, 100)
				                 .Curve;
			coveredRow.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) },
				new GeometryComponent[] { },
				new[] { ReadOnlyTableFactory.Create(coveredClass) },
				new[] { GeometryComponent.InteriorVertices },
				null, 0, new List<IReadOnlyFeatureClass>());

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner, "CoveredByOther.NotFullyCovered");
		}

		[Test]
		public void CanDetectInteriorVerticesNotCoveredByInteriorVertices()
		{
			var testName = MethodBase.GetCurrentMethod().Name;

			IFeatureClass coveringClass = CreateFeatureClass(
				string.Format("{0}_covering", testName),
				esriGeometryType.esriGeometryPolyline);
			IFeatureClass coveredClass = CreateFeatureClass(
				string.Format("{0}_covered", testName),
				esriGeometryType.esriGeometryPolyline);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartLine(100, 100)
				                 .LineTo(200, 100) // interior vertex
				                 .LineTo(300, 100)
				                 .Curve;
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartLine(100, 100)
				                 .LineTo(200, 100) // interior vertex, covered
				                 .LineTo(300, 100) // interior vertex, not covered
				                 .LineTo(400, 100)
				                 .Curve;
			coveredRow.Store();

			IEnvelope verificationEnvelope =
				GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) },
				new[] { GeometryComponent.InteriorVertices },
				new[] { ReadOnlyTableFactory.Create(coveredClass) },
				new[] { GeometryComponent.InteriorVertices },
				null, 0, new List<IReadOnlyFeatureClass>());

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner, "CoveredByOther.NotFullyCovered");
		}

		[Test]
		public void CanIgnoreUncoveredFeatureOutsideAreaOfInterest()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			IFeatureClass areaOfInterestClass;
			CreatePolygonFeatureClasses("CanIgnoreUncoveredFeatureOutsideAreaOfInterest",
			                            out coveringClass,
			                            out coveredClass,
			                            out areaOfInterestClass,
			                            createAreaOfInterestClass: true);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                                  200, 200);
			coveringRow.Store();

			// covered: no error
			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape = GeometryFactory.CreatePolygon(110, 110,
			                                                 190, 190);
			coveredRow.Store();

			// not covered, but outside AOI: no error
			IFeature unCoveredRowOutsideAOI = coveredClass.CreateFeature();
			unCoveredRowOutsideAOI.Shape = GeometryFactory.CreatePolygon(0, 0,
				10, 10);
			unCoveredRowOutsideAOI.Store();

			IFeature aoiRow = areaOfInterestClass.CreateFeature();
			aoiRow.Shape = GeometryFactory.CreatePolygon(50, 50,
			                                             250, 250);
			aoiRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0,
				500, 500);

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) }, new GeometryComponent[] { },
				new[] { ReadOnlyTableFactory.Create(coveredClass) }, new GeometryComponent[] { },
				new string[] { }, 0,
				new[] { ReadOnlyTableFactory.Create(areaOfInterestClass) });
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanIgnoreUncoveredPointOutsideAreaOfInterest()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			IFeatureClass areaOfInterestClass;
			CreatePolygonFeatureClasses("CanIgnoreUncoveredPointOutsideAreaOfInterest",
			                            out coveringClass,
			                            out coveredClass,
			                            out areaOfInterestClass,
			                            createAreaOfInterestClass: true);

			coveredClass =
				CreateFeatureClass("CanIgnoreUncoveredPointOutsideAreaOfInterest_point",
				                   esriGeometryType.esriGeometryPoint);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                                  200, 200);
			coveringRow.Store();

			// covered: no error
			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape = GeometryFactory.CreatePoint(110, 110);
			coveredRow.Store();

			// not covered, but outside AOI: no error
			IFeature coveredRowUncoveredOutsideAOI = coveredClass.CreateFeature();
			coveredRowUncoveredOutsideAOI.Shape = GeometryFactory.CreatePoint(220, 220);
			coveredRowUncoveredOutsideAOI.Store();

			// not covered, inside AOI: error
			IFeature coveredRowUncoveredInsideAOI = coveredClass.CreateFeature();
			coveredRowUncoveredInsideAOI.Shape = GeometryFactory.CreatePoint(205, 205);
			coveredRowUncoveredInsideAOI.Store();

			IFeature aoiRow = areaOfInterestClass.CreateFeature();
			aoiRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                             210, 210);
			aoiRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0,
				500, 500);

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) }, new GeometryComponent[] { },
				new[] { ReadOnlyTableFactory.Create(coveredClass) }, new GeometryComponent[] { },
				new string[] { }, 0,
				new[] { ReadOnlyTableFactory.Create(areaOfInterestClass) });
			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };
			runner.Execute(verificationEnvelope);

			QaError error = AssertUtils.OneError(runner, "CoveredByOther.NotCoveredByAnyFeature");
			Assert.NotNull(error.Geometry);
			var point = (IPoint) error.Geometry;
			Assert.AreEqual(205, point.X, 0.001);
			Assert.AreEqual(205, point.Y, 0.001);
		}

		[Test]
		public void CanIgnoreUncoveredAreaOutsideAreaOfInterest()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			IFeatureClass areaOfInterestClass;
			CreatePolygonFeatureClasses("CanIgnoreUncoveredAreaOutsideAreaOfInterest",
			                            out coveringClass,
			                            out coveredClass,
			                            out areaOfInterestClass,
			                            createAreaOfInterestClass: true);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                                  200, 200);
			coveringRow.Store();

			// covered: no error
			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape = GeometryFactory.CreatePolygon(110, 110,
			                                                 190, 190);
			coveredRow.Store();

			// not covered, but outside AOI: no error
			IFeature uncoveredRowOutsideAOI = coveredClass.CreateFeature();
			uncoveredRowOutsideAOI.Shape = GeometryFactory.CreatePolygon(90, 90,
				210, 210);
			uncoveredRowOutsideAOI.Store();

			IFeature aoiRow = areaOfInterestClass.CreateFeature();
			aoiRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                             200, 200);
			aoiRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0,
				500, 500);

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) }, new GeometryComponent[] { },
				new[] { ReadOnlyTableFactory.Create(coveredClass) }, new GeometryComponent[] { },
				new string[] { }, 0,
				new[] { ReadOnlyTableFactory.Create(areaOfInterestClass) });
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanReduceUncoveredAreaToPartWithinAreaOfInterest()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			IFeatureClass areaOfInterestClass;
			CreatePolygonFeatureClasses("CanReduceUncoveredAreaToPartWithinAreaOfInterest",
			                            out coveringClass,
			                            out coveredClass,
			                            out areaOfInterestClass,
			                            createAreaOfInterestClass: true);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                                  200, 200);
			coveringRow.Store();

			// covered: no error
			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape = GeometryFactory.CreatePolygon(110, 110,
			                                                 190, 190);
			coveredRow.Store();

			// partly covered, partly inside AOI: reduced error
			IFeature uncoveredRowPartlyInsideAOI = coveredClass.CreateFeature();
			uncoveredRowPartlyInsideAOI.Shape = GeometryFactory.CreatePolygon(90, 90,
				220, 200);
			uncoveredRowPartlyInsideAOI.Store();

			// not covered, but outside AOI, touching it: no error
			IFeature uncoveredOutsideAOITouching = coveredClass.CreateFeature();
			uncoveredOutsideAOITouching.Shape = GeometryFactory.CreatePolygon(210, 100,
				300, 200);
			uncoveredOutsideAOITouching.Store();

			IFeature aoiRow = areaOfInterestClass.CreateFeature();
			aoiRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                             210, 200);
			aoiRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0,
				500, 500);

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) }, new GeometryComponent[] { },
				new[] { ReadOnlyTableFactory.Create(coveredClass) }, new GeometryComponent[] { },
				new string[] { }, 0,
				new[] { ReadOnlyTableFactory.Create(areaOfInterestClass) });
			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };
			runner.Execute(verificationEnvelope);

			QaError error = AssertUtils.OneError(runner, "CoveredByOther.NotFullyCovered");

			Assert.NotNull(error.Geometry);
			Assert.AreEqual(1000, ((IArea) error.Geometry).Area, 0.001);
		}

		[Test]
		public void CanReduceUncoveredLineToPartWithinAreaOfInterest()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			IFeatureClass areaOfInterestClass;
			CreatePolygonFeatureClasses("CanReduceUncoveredLineToPartWithinAreaOfInterest",
			                            out coveringClass,
			                            out coveredClass,
			                            out areaOfInterestClass,
			                            createAreaOfInterestClass: true);

			coveredClass =
				CreateFeatureClass("CanReduceUncoveredLineToPartWithinAreaOfInterest_line",
				                   esriGeometryType.esriGeometryPolyline);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                                  200, 200);
			coveringRow.Store();

			// covered: no error
			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape = GeometryFactory.CreatePolyline(110, 110,
			                                                  190, 110);
			coveredRow.Store();

			// covered: no error
			IFeature covered = coveredClass.CreateFeature();
			covered.Shape = GeometryFactory.CreatePolyline(100, 100, 200, 100);
			covered.Store();

			// not covered, but outside AOI: no error
			IFeature uncoveredOutsideAOI = coveredClass.CreateFeature();
			uncoveredOutsideAOI.Shape = GeometryFactory.CreatePolyline(300, 100, 400, 100);
			uncoveredOutsideAOI.Store();

			// not covered, but outside AOI, touching it with an end point: no error
			IFeature uncoveredOutsideAOITouching = coveredClass.CreateFeature();
			uncoveredOutsideAOITouching.Shape = GeometryFactory.CreatePolyline(210, 100,
				300, 100);
			uncoveredOutsideAOITouching.Store();

			// partly covered, partly inside AOI: reduced error
			IFeature uncoveredRowPartlyInsideAOI = coveredClass.CreateFeature();
			uncoveredRowPartlyInsideAOI.Shape = GeometryFactory.CreatePolyline(90, 150,
				220, 150);
			uncoveredRowPartlyInsideAOI.Store();

			IFeature aoiRow = areaOfInterestClass.CreateFeature();
			aoiRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                             210, 200);
			aoiRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0,
				500, 500);

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) }, new GeometryComponent[] { },
				new[] { ReadOnlyTableFactory.Create(coveredClass) }, new GeometryComponent[] { },
				new string[] { }, 0,
				new[] { ReadOnlyTableFactory.Create(areaOfInterestClass) });
			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };
			runner.Execute(verificationEnvelope);

			QaError error = AssertUtils.OneError(runner, "CoveredByOther.NotFullyCovered");

			Assert.NotNull(error.Geometry);
			Assert.AreEqual(10, ((ICurve) error.Geometry).Length, 0.001);
		}

		[Test]
		public void CanReduceUncoveredLineToPartWithinMultipleAreasOfInterest()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			IFeatureClass areaOfInterestClass;
			CreatePolygonFeatureClasses(
				"CanReduceUncoveredLineToPartWithinMultipleAreasOfInterest",
				out coveringClass,
				out coveredClass,
				out areaOfInterestClass,
				createAreaOfInterestClass: true);

			coveredClass =
				CreateFeatureClass(
					"CanReduceUncoveredLineToPartWithinMultipleAreasOfInterest_line",
					esriGeometryType.esriGeometryPolyline);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                                  200, 200);
			coveringRow.Store();

			// partly covered, partly inside AOI polygons: reduced error
			IFeature uncoveredRowPartlyInsideAOI = coveredClass.CreateFeature();
			uncoveredRowPartlyInsideAOI.Shape = GeometryFactory.CreatePolyline(90, 150,
				300, 150);
			uncoveredRowPartlyInsideAOI.Store();

			IFeature aoiRow1 = areaOfInterestClass.CreateFeature();
			aoiRow1.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                              210, 200);
			aoiRow1.Store();

			IFeature aoiRow2 = areaOfInterestClass.CreateFeature();
			aoiRow2.Shape = GeometryFactory.CreatePolygon(220, 100,
			                                              230, 200);
			aoiRow2.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0,
				500, 500);

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) }, new GeometryComponent[] { },
				new[] { ReadOnlyTableFactory.Create(coveredClass) }, new GeometryComponent[] { },
				new string[] { }, 0,
				new[] { ReadOnlyTableFactory.Create(areaOfInterestClass) });
			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };
			runner.Execute(verificationEnvelope);

			IList<QaError> errors = runner.Errors;

			Assert.AreEqual(2, runner.Errors.Count);

			Assert.NotNull(errors[0].Geometry);
			Assert.NotNull(errors[1].Geometry);

			Assert.AreEqual(10, ((ICurve) errors[0].Geometry).Length, 0.001);
			Assert.AreEqual(10, ((ICurve) errors[1].Geometry).Length, 0.001);
		}

		[Test]
		public void CanReduceUncoveredMultiPatchToPartWithinAreaOfInterest()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			IFeatureClass areaOfInterestClass;
			CreatePolygonFeatureClasses(
				"CanReduceUncoveredMultiPatchToPartWithinAreaOfInterest",
				out coveringClass,
				out coveredClass,
				out areaOfInterestClass,
				createAreaOfInterestClass: true);

			coveredClass =
				CreateFeatureClass(
					"CanReduceUncoveredMultiPatchToPartWithinAreaOfInterest_multipatch",
					esriGeometryType.esriGeometryMultiPatch,
					zAware: true);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                                  200, 200);
			coveringRow.Store();

			// covered: no error
			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape = CreateMultiPatch(110, 110, 190, 190, 0, 10);
			coveredRow.Store();

			// not covered, but outside AOI: no error
			IFeature uncoveredOutsideAOI = coveredClass.CreateFeature();
			uncoveredOutsideAOI.Shape = CreateMultiPatch(300, 100, 400, 100, 0, 10);
			uncoveredOutsideAOI.Store();

			// not covered, but outside AOI, touching it: no error
			IFeature uncoveredOutsideAOITouching = coveredClass.CreateFeature();
			uncoveredOutsideAOITouching.Shape = CreateMultiPatch(210, 100, 300, 200, 0, 10);
			uncoveredOutsideAOITouching.Store();

			// partly covered, partly inside AOI: reduced error
			IFeature uncoveredRowPartlyInsideAOI = coveredClass.CreateFeature();
			uncoveredRowPartlyInsideAOI.Shape = CreateMultiPatch(90, 90, 220, 200, 0, 10);
			uncoveredRowPartlyInsideAOI.Store();

			IFeature aoiRow = areaOfInterestClass.CreateFeature();
			aoiRow.Shape = GeometryFactory.CreatePolygon(100, 100,
			                                             210, 200);
			aoiRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0,
				500, 500);

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) }, new GeometryComponent[] { },
				new[] { ReadOnlyTableFactory.Create(coveredClass) }, new GeometryComponent[] { },
				new string[] { }, 0,
				new[] { ReadOnlyTableFactory.Create(areaOfInterestClass) });
			var runner = new QaContainerTestRunner(10000, test) { KeepGeometry = true };
			runner.Execute(verificationEnvelope);

			QaError error = AssertUtils.OneError(runner, "CoveredByOther.NotFullyCovered");

			Assert.NotNull(error.Geometry);
			Assert.AreEqual(1000, ((IArea) error.Geometry).Area, 0.001);
		}

		[NotNull]
		private static IMultiPatch CreateMultiPatch(double xmin, double ymin,
		                                            double xmax, double ymax,
		                                            double zmin, double extrusion)
		{
			IPolygon footprint = GeometryFactory.CreatePolygon(xmin, ymin, xmax, ymax);
			GeometryUtils.MakeZAware(footprint);
			GeometryUtils.ApplyConstantZ(footprint, zmin);

			return GeometryFactory.CreateMultiPatch(footprint, extrusion);
		}

		[Test]
		public void CanDetectPolygonNotCoveredByAnyFeature()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanDetectPolygonNotCoveredByAnyFeature",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 201)
				                 .LineTo(200, 201)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				ReadOnlyTableFactory.Create(coveringClass),
				ReadOnlyTableFactory.Create(coveredClass));
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner, "CoveredByOther.NotCoveredByAnyFeature");
		}

		[Test]
		public void CanDetectPolygonNotCoveredByAnyFeaturePartlyOutsideExtent()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses(
				"CanDetectPolygonNotCoveredByAnyFeaturePartlyOutsideExtent",
				out coveringClass,
				out coveredClass);

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 201)
				                 .LineTo(200, 201)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 150, 500);

			var test = new QaIsCoveredByOther(
				ReadOnlyTableFactory.Create(coveringClass),
				ReadOnlyTableFactory.Create(coveredClass));
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner,
			                     "CoveredByOther.NotCoveredByAnyFeature.PartlyOutsideVerifiedExtent");
		}

		[Test]
		public void CanAllowUncoveredAreaByTolerance()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanAllowUncoveredAreaByTolerance",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 201)
				                 .LineTo(200, 201)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				           ReadOnlyTableFactory.Create(coveringClass),
				           ReadOnlyTableFactory.Create(coveredClass))
			           {
				           CoveringClassTolerances = new[] { 1d }
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanUseCoveringFeatureOutsideExtentButWithinTolerance()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanUseCoveringFeatureOutsideExtentButWithinTolerance",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartPoly(101, 101)
				                 .LineTo(101, 201)
				                 .LineTo(201, 201)
				                 .LineTo(201, 101)
				                 .ClosePolygon();
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 100, 100);

			var test = new QaIsCoveredByOther(
				           ReadOnlyTableFactory.Create(coveringClass),
				           ReadOnlyTableFactory.Create(coveredClass))
			           {
				           CoveringClassTolerances = new[] { 2d }
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanUsingInvalidNumberOfTolerancesThrowsException()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanThrowExceptionUsingInvalidNumberOfTolerances",
			                            out coveringClass,
			                            out coveredClass);

			Assert.Throws<ArgumentException>(
				() => new QaIsCoveredByOther(
					      ReadOnlyTableFactory.Create(coveringClass),
					      ReadOnlyTableFactory.Create(coveredClass))
				      {
					      CoveringClassTolerances = new[] { 2d, 3d, 1.234 }
				      });
		}

		[Test]
		public void CanAllowUncoveredAreaByPercentage()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanAllowUncoveredAreaByPercentage",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 201)
				                 .LineTo(200, 201)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			const double allowedUncoveredPercentage = 1;
			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) },
				new GeometryComponent[] { },
				new[] { ReadOnlyTableFactory.Create(coveredClass) },
				new GeometryComponent[] { },
				(string) null,
				allowedUncoveredPercentage);

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanDetectInsufficientlyCoveredAreaByPercentage()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanDetectInsufficientlyCoveredAreaByPercentage",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 201)
				                 .LineTo(200, 201)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			const double allowedUncoveredPercentage = 0.1;
			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) },
				new GeometryComponent[] { },
				new[] { ReadOnlyTableFactory.Create(coveredClass) },
				new GeometryComponent[] { },
				(string) null,
				allowedUncoveredPercentage);

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.OneError(runner, "CoveredByOther.NotSufficientlyCovered");
		}

		[Test]
		public void CanAllowEqualPolygons()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanAllowEqualPolygons",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape = coveringRow.ShapeCopy;
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				ReadOnlyTableFactory.Create(coveringClass),
				ReadOnlyTableFactory.Create(coveredClass));
			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowTouchingUncoveredAreaByTolerance()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanAllowTouchingUncoveredAreaByTolerance",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(100, 200)
				                 .LineTo(100, 300)
				                 .LineTo(200, 300)
				                 .LineTo(200, 200)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				           ReadOnlyTableFactory.Create(coveringClass),
				           ReadOnlyTableFactory.Create(coveredClass))
			           {
				           CoveringClassTolerances = new[] { 100d }
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanProcessEmptyGeometeries()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanProcessEmptyGeometeries",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();

			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();

			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				ReadOnlyTableFactory.Create(coveringClass),
				ReadOnlyTableFactory.Create(coveredClass));

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowNotTouchingUncoveredAreaWithinTolerance()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanUseCoveringFeatureNotTouchingButWithinTolerance",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(201, 100)
				                 .LineTo(201, 200)
				                 .LineTo(203, 200)
				                 .LineTo(203, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				           ReadOnlyTableFactory.Create(coveringClass),
				           ReadOnlyTableFactory.Create(coveredClass))
			           {
				           CoveringClassTolerances = new[] { 5d }
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowNotTouchingUncoveredAreaWithinToleranceInOtherTile()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses(
				"CanAllowNotTouchingUncoveredAreaWithinToleranceInOtherTile",
				out coveringClass,
				out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(201, 100)
				                 .LineTo(201, 200)
				                 .LineTo(203, 200)
				                 .LineTo(203, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				           ReadOnlyTableFactory.Create(coveringClass),
				           ReadOnlyTableFactory.Create(coveredClass))
			           {
				           CoveringClassTolerances = new[] { 5d }
			           };

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowEndPointCoverageForCrossTileLines()
		{
			const string testName = "CanAllowEndPointCoverageForCrossTileLines";

			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001,
			                                  0.001);

			const bool coveringZAware = false;
			const bool coveredZAware = false;
			const bool mAware = false;
			IFields coveringFields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(
					"Shape",
					esriGeometryType.esriGeometryPolyline,
					sref, 1000, coveringZAware, mAware));
			IFields coveredFields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(
					"Shape",
					esriGeometryType.esriGeometryPoint,
					sref, 1000, coveredZAware, mAware));

			IFeatureClass coveringClass = DatasetUtils.CreateSimpleFeatureClass(
				_testWs, string.Format("{0}_covering", testName), coveringFields);

			IFeatureClass coveredClass = DatasetUtils.CreateSimpleFeatureClass(
				_testWs, string.Format("{0}_covered", testName), coveredFields);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape = CurveConstruction.StartLine(100, 100)
			                                     .LineTo(300, 100)
			                                     .Curve;
			coveringRow.Store();

			IFeature coveredRow1 = coveredClass.CreateFeature();
			coveredRow1.Shape = GeometryFactory.CreatePoint(100, 100);
			coveredRow1.Store();

			IFeature coveredRow2 = coveredClass.CreateFeature();
			coveredRow2.Shape = GeometryFactory.CreatePoint(300, 100);
			coveredRow2.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) },
				new[] { GeometryComponent.LineEndPoints },
				new[] { ReadOnlyTableFactory.Create(coveredClass) },
				new[] { GeometryComponent.EntireGeometry },
				(string) null, 0);

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowTouchingUncoveredAreaWithinToleranceInOtherTile()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses(
				"CanAllowTouchingUncoveredAreaWithinToleranceInOtherTile",
				out coveringClass,
				out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(200, 100)
				                 .LineTo(200, 200)
				                 .LineTo(203, 200)
				                 .LineTo(203, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				           ReadOnlyTableFactory.Create(coveringClass),
				           ReadOnlyTableFactory.Create(coveredClass))
			           {
				           CoveringClassTolerances = new[] { 3d }
			           };

			var runner = new QaContainerTestRunner(200, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowMultipleCoveringInMultipleTiles()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanAllowMultipleCoveringInMultipleTiles",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveringRow1 = coveringClass.CreateFeature();
			coveringRow1.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 150)
				                 .LineTo(150, 150)
				                 .LineTo(150, 100)
				                 .ClosePolygon();
			coveringRow1.Store();

			IFeature coveringRow2 = coveringClass.CreateFeature();
			coveringRow2.Shape =
				CurveConstruction.StartPoly(0, 150)
				                 .LineTo(0, 300)
				                 .LineTo(150, 300)
				                 .LineTo(150, 150)
				                 .ClosePolygon();
			coveringRow2.Store();

			IFeature coveringRow3 = coveringClass.CreateFeature();
			coveringRow3.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(100, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveringRow3.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(150, 100)
				                 .LineTo(150, 200)
				                 .LineTo(200, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon();
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				ReadOnlyTableFactory.Create(coveringClass),
				ReadOnlyTableFactory.Create(coveredClass));

			var runner = new QaContainerTestRunner(150, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanEvaluateSelfIntersectingPolygon()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses("CanEvaluateSelfIntersectingPolygon",
			                            out coveringClass,
			                            out coveredClass);

			IFeature coveringRow = coveringClass.CreateFeature();
			coveringRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(200, 200)
				                 .LineTo(100, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon(true);
			coveringRow.Store();

			IFeature coveredRow = coveredClass.CreateFeature();
			coveredRow.Shape =
				CurveConstruction.StartPoly(100, 100)
				                 .LineTo(200, 200)
				                 .LineTo(100, 200)
				                 .LineTo(200, 100)
				                 .ClosePolygon(true);
			coveredRow.Store();

			IEnvelope verificationEnvelope = GeometryFactory.CreateEnvelope(0, 0, 500, 500);

			var test = new QaIsCoveredByOther(
				           ReadOnlyTableFactory.Create(coveringClass),
				           ReadOnlyTableFactory.Create(coveredClass))
			           {
				           CoveringClassTolerances = new[] { 1d }
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute(verificationEnvelope);

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowClosedLine()
		{
			IFeatureClass lineClass = CreateFeatureClass("CanAllowClosedLine_line",
			                                             esriGeometryType.esriGeometryPolyline);

			IFeature lineFeature = lineClass.CreateFeature();
			lineFeature.Shape = CurveConstruction.StartLine(0, 0)
			                                     .LineTo(100, 0)
			                                     .LineTo(100, 100)
			                                     .LineTo(0, 0)
			                                     .Curve;
			lineFeature.Store();

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(lineClass) },
				new[] { GeometryComponent.LineEndPoints },
				new[] { ReadOnlyTableFactory.Create(lineClass) },
				new[] { GeometryComponent.LineEndPoints },
				null);

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowClosedLineWithinTolerance()
		{
			IFeatureClass lineClass =
				CreateFeatureClass("CanAllowClosedLineWithinTolerance_line",
				                   esriGeometryType.esriGeometryPolyline);

			IFeature lineFeature = lineClass.CreateFeature();
			lineFeature.Shape = CurveConstruction.StartLine(0, 0)
			                                     .LineTo(100, 0)
			                                     .LineTo(100, 100)
			                                     .LineTo(1, 0)
			                                     .Curve;
			lineFeature.Store();

			var test = new QaIsCoveredByOther(
				           new[] { ReadOnlyTableFactory.Create(lineClass) },
				           new[] { GeometryComponent.LineEndPoints },
				           new[] { ReadOnlyTableFactory.Create(lineClass) },
				           new[] { GeometryComponent.LineEndPoints },
				           null)
			           {
				           CoveringClassTolerances = new[] { 2d }.ToList()
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanAllowMissingPointOnClosedLineWithinTolerance()
		{
			IFeatureClass lineClass =
				CreateFeatureClass("CanAllowMissingPointOnClosedLineWithinTolerance_line",
				                   esriGeometryType.esriGeometryPolyline);
			IFeatureClass pointClass =
				CreateFeatureClass("CanAllowMissingPointOnClosedLineWithinTolerance_point",
				                   esriGeometryType.esriGeometryPoint);

			// nearly closed line, no point near end points --> error
			IFeature nearlyClosedLine1 = lineClass.CreateFeature();
			nearlyClosedLine1.Shape = CurveConstruction.StartLine(0, 0)
			                                           .LineTo(100, 0)
			                                           .LineTo(100, 100)
			                                           .LineTo(1, 0)
			                                           .Curve;
			nearlyClosedLine1.Store();

			// nearly closed line, point near end points --> no error
			IFeature nearlyClosedLine2 = lineClass.CreateFeature();
			nearlyClosedLine1.Shape = CurveConstruction.StartLine(500, 0)
			                                           .LineTo(600, 0)
			                                           .LineTo(600, 100)
			                                           .LineTo(501, 0)
			                                           .Curve;
			nearlyClosedLine2.Store();

			IFeature point = pointClass.CreateFeature();
			point.Shape = GeometryFactory.CreatePoint(500.5, 0);

			IFeature closedLine = lineClass.CreateFeature();
			nearlyClosedLine1.Shape = CurveConstruction.StartLine(1000, 0)
			                                           .LineTo(1100, 0)
			                                           .LineTo(1100, 100)
			                                           .LineTo(1000, 0)
			                                           .Curve;
			closedLine.Store();

			var test = new QaIsCoveredByOther(
				           new[]
				           {
					           ReadOnlyTableFactory.Create(lineClass),
					           ReadOnlyTableFactory.Create(pointClass)
				           },
				           new[]
				           {
					           GeometryComponent.LineEndPoints,
					           GeometryComponent.EntireGeometry
				           },
				           new[] { ReadOnlyTableFactory.Create(lineClass) },
				           new[] { GeometryComponent.LineEndPoints },
				           null)
			           {
				           // use no tolerance for covering line end points, but tol=2 for covering points
				           CoveringClassTolerances = new[] { 0d, 2d }.ToList()
			           };

			var runner = new QaContainerTestRunner(10000, test);
			runner.Execute();

			QaError error = AssertUtils.OneError(runner, "CoveredByOther.NotCoveredByAnyFeature");

			Assert.AreEqual(1, error.InvolvedRows.Count);
			Assert.AreEqual(nearlyClosedLine1.OID, error.InvolvedRows[0].OID);
		}

		[Test]
		public void CanDetectUncoveredEndPoint_IgnoringCoveredEndpointOutsideTestExtent()
		{
			IFeatureClass lineClass = CreateFeatureClass(
				"CanDetectUncoveredEndPoint_IgnoringCoveredEndpointOutsideTestExtent_lines",
				esriGeometryType.esriGeometryPolyline);
			IFeatureClass pointClass = CreateFeatureClass(
				"CanDetectUncoveredEndPoint_IgnoringCoveredEndpointOutsideTestExtent_points",
				esriGeometryType.esriGeometryPoint);

			IFeature line = lineClass.CreateFeature();
			line.Shape = CurveConstruction.StartLine(0, 0)
			                              .LineTo(100, 100)
			                              .Curve;
			line.Store();

			IFeature point = pointClass.CreateFeature();
			point.Shape = GeometryFactory.CreatePoint(0, 0);
			point.Store();

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(pointClass) },
				new[] { GeometryComponent.EntireGeometry },
				new[] { ReadOnlyTableFactory.Create(lineClass) },
				new[] { GeometryComponent.LineEndPoints },
				null);

			var runner = new QaContainerTestRunner(1000, test) { KeepGeometry = true };
			IEnvelope testExtent = GeometryFactory.CreateEnvelope(50, 50, 150, 150);
			runner.Execute(testExtent);

			QaError error = AssertUtils.OneError(runner,
			                                     "CoveredByOther.NotCoveredByAnyFeature.PartlyOutsideVerifiedExtent");

			Assert.AreEqual(1, runner.ErrorGeometries.Count);
			Assert.AreEqual(1, GeometryUtils.GetPartCount(runner.ErrorGeometries[0]));
		}

		[Test]
		public void CanDetectUncoveredRing_IgnoringCoveredRingOutsideTestExtent()
		{
			IFeatureClass coveringClass;
			IFeatureClass coveredClass;
			CreatePolygonFeatureClasses(
				"CanDetectUncoveredRing_IgnoringCoveredRingOutsideTestExtent",
				out coveringClass, out coveredClass);

			IPolygon polyOutsideTestExtent = GeometryFactory.CreatePolygon(0, 0, 10, 10);
			IPolygon polyInsideTestExtent = GeometryFactory.CreatePolygon(100, 100, 110, 110);

			IGeometry polygonUnion = GeometryUtils.Union(polyOutsideTestExtent,
			                                             polyInsideTestExtent);

			IFeature coveredFeature = coveredClass.CreateFeature();
			coveredFeature.Shape = polygonUnion;
			coveredFeature.Store();

			IFeature coveringFeature = coveringClass.CreateFeature();
			coveringFeature.Shape = polyOutsideTestExtent;
			coveringFeature.Store();

			// ring inside the test extent is not covered --> reported as error

			var test = new QaIsCoveredByOther(
				new[] { ReadOnlyTableFactory.Create(coveringClass) },
				new[] { GeometryComponent.EntireGeometry },
				new[] { ReadOnlyTableFactory.Create(coveredClass) },
				new[] { GeometryComponent.EntireGeometry },
				null);

			var runner = new QaContainerTestRunner(1000, test) { KeepGeometry = true };
			IEnvelope testExtent = GeometryFactory.CreateEnvelope(50, 50, 150, 150);
			runner.Execute(testExtent);

			QaError error = AssertUtils.OneError(runner,
			                                     "CoveredByOther.NotCoveredByAnyFeature.PartlyOutsideVerifiedExtent");

			Assert.AreEqual(1, runner.ErrorGeometries.Count);
			Assert.AreEqual(1, GeometryUtils.GetPartCount(runner.ErrorGeometries[0]));
		}

		[Test]
		[Ignore("uses local data")]
		public void TestTop5360()
		{
			IFeatureWorkspace ws = WorkspaceUtils.OpenFileGdbFeatureWorkspace(
				@"C:\Topgis\BugData\IsCoveredByOther_TOP-5360\20201126_0545_RC2030-12-31.gdb");
			IFeatureClass errorClass =
				CreateFeatureClass("errors", esriGeometryType.esriGeometryPolygon, zAware: true);
			QaIsCoveredByOther test =
				new QaIsCoveredByOther(
					new[]
					{
						ReadOnlyTableFactory.Create(ws.OpenFeatureClass("GC_SURFACES")),
						ReadOnlyTableFactory.Create(ws.OpenFeatureClass("GC_UNCO_DESPOSIT")),
						ReadOnlyTableFactory.Create(ws.OpenFeatureClass("GC_BEDROCK"))
					},
					new[]
					{
						ReadOnlyTableFactory.Create(ws.OpenFeatureClass("GC_MAPSHEET"))
					});
			test.SetConstraint(0, "KIND = 12701002 OR KIND = 12701003");
			var runner = new QaContainerTestRunner(5000, test);

			runner.Execute(
				GeometryFactory.CreateEnvelope(2599421.855, 1255458.608, 2606113.181, 1261014.869));

			Assert.IsTrue(runner.Errors.Count < 2);
		}

		[Test]
		[Ignore("uses local data")]
		public void TestDkm25()
		{
			IFeatureWorkspace ws1 = WorkspaceUtils.OpenFileGdbFeatureWorkspace(
				@"C:\temp\QaIsCoveredByOther\106_00mm_dkm25_anno_annomasks.gdb");
			IFeatureWorkspace ws2 = WorkspaceUtils.OpenFileGdbFeatureWorkspace(
				@"C:\temp\QaIsCoveredByOther\108_00mm_dkm25_anno_annomasks.gdb");

			QaIsCoveredByOther test =
				new QaIsCoveredByOther(
					new[]
					{
						ReadOnlyTableFactory.Create(
							ws1.OpenFeatureClass("DKM25_ANNOBLAU_MASK")),
					},
					new[]
					{
						ReadOnlyTableFactory.Create(
							ws2.OpenFeatureClass("DKM25_ANNOBLAU_MASK"))
					});
			test.ValidUncoveredGeometryConstraint = "$SliverRatio < 50 OR $Area > 10";

			var runner = new QaContainerTestRunner(10000, test);

			runner.Execute();
		}

		private static void StoreGeometrySlices(IFeatureClass coveringClass)
		{
			int sliceCount = 3000;
			double sliceWidth = 100d / sliceCount;

			for (int i = 0; i < sliceCount; i++)
			{
				double xMin = 100 + i * sliceWidth;
				double xMax = 100 + (i + 1) * sliceWidth;

				IFeature coveringRow = coveringClass.CreateFeature();
				coveringRow.Shape =
					CurveConstruction.StartPoly(xMin, 100)
					                 .LineTo(xMin, 200)
					                 .LineTo(xMax, 200)
					                 .LineTo(xMax, 100)
					                 .ClosePolygon();
				coveringRow.Store();
			}
		}

		private void CreatePolygonFeatureClasses([NotNull] string testName,
		                                         [NotNull] out IFeatureClass coveringClass,
		                                         [NotNull] out IFeatureClass coveredClass,
		                                         bool coveringZAware = false,
		                                         bool coveredZAware = false)
		{
			CreatePolygonFeatureClasses(testName,
			                            out coveringClass,
			                            out coveredClass,
			                            out IFeatureClass _,
			                            coveringZAware, coveredZAware);
		}

		[ContractAnnotation(
			"createAreaOfInterestClass: true => areaOfInterestClass: notnull")]
		private void CreatePolygonFeatureClasses(
			[NotNull] string testName,
			[NotNull] out IFeatureClass coveringClass,
			[NotNull] out IFeatureClass coveredClass,
			[CanBeNull] out IFeatureClass areaOfInterestClass,
			bool coveringZAware = false,
			bool coveredZAware = false,
			bool createAreaOfInterestClass = false)
		{
			coveringClass = CreateFeatureClass(
				string.Format("{0}_covering", testName),
				esriGeometryType.esriGeometryPolygon, coveringZAware);

			coveredClass = CreateFeatureClass(
				string.Format("{0}_covered", testName),
				esriGeometryType.esriGeometryPolygon, coveredZAware);

			areaOfInterestClass = createAreaOfInterestClass
				                      ? CreateFeatureClass(string.Format("{0}_aoi", testName),
				                                           esriGeometryType.esriGeometryPolygon)
				                      : null;
		}

		[NotNull]
		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType geometryType,
		                                         bool zAware = false)
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001,
			                                  0.001);

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField("Shape",
				                            geometryType,
				                            sref, 1000, zAware));

			return DatasetUtils.CreateSimpleFeatureClass(_testWs, name, fields);
		}
	}
}
