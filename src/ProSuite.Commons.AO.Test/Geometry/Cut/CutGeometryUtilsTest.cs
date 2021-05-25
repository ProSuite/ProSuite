using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Cut;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Geometry;

namespace ProSuite.Commons.AO.Test.Geometry.Cut
{
	[TestFixture]
	public class CutGeometryUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanCutPolygonWithZSourcePlane()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 100, 100, lv95));

			IPolygon innerRingPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 10, 10, lv95));

			Plane3D plane = Plane3D.FitPlane(new List<Pnt3D>
			                                 {
				                                 new Pnt3D(2600000, 1200000, 500),
				                                 new Pnt3D(2600100, 1200100, 580),
				                                 new Pnt3D(2600000, 1200100, 550)
			                                 });

			((IGeometryCollection) originalPoly).AddGeometryCollection(
				(IGeometryCollection) innerRingPoly);

			GeometryUtils.Simplify(originalPoly);

			ChangeAlongZUtils.AssignZ((IPointCollection) originalPoly, plane);

			// The non-z-aware cut line cuts north of the inner ring -> the inner ring should be assigned to the southern result
			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600000 - 100, 1200020),
				GeometryFactory.CreatePoint(2600000 - 40, 1200020),
				GeometryFactory.CreatePoint(2600000 - 40, 1200040),
				GeometryFactory.CreatePoint(2600000 + 40, 1200040),
				GeometryFactory.CreatePoint(2600000 + 40, 1200020),
				GeometryFactory.CreatePoint(2600000 + 100, 1200020));
			cutLine.SpatialReference = lv95;

			bool customIntersectOrig = IntersectionUtils.UseCustomIntersect;

			try
			{
				IntersectionUtils.UseCustomIntersect = false;

				const ChangeAlongZSource zSource = ChangeAlongZSource.SourcePlane;
				var resultsAo =
					CutGeometryUtils.TryCut(originalPoly, cutLine, zSource);

				IntersectionUtils.UseCustomIntersect = true;
				var resultsGeom =
					CutGeometryUtils.TryCut(originalPoly, cutLine, zSource);

				Assert.NotNull(resultsAo);
				Assert.NotNull(resultsGeom);

				EnsureCutResult(resultsAo, originalPoly, plane, 3);
				EnsureCutResult(resultsGeom, originalPoly, plane, 3);

				// NOTE: The results have different start/end points, therefore GeometryUtils.AreEqual is false
				Assert.True(GeometryUtils.AreEqualInXY(resultsAo[0], resultsGeom[0]));
				Assert.True(GeometryUtils.AreEqualInXY(resultsAo[1], resultsGeom[1]));

				Assert.AreEqual(0,
				                IntersectionUtils.GetZOnlyDifferenceLines(
					                                 (IPolycurve) resultsAo[0],
					                                 (IPolycurve) resultsGeom[0], 0.001)
				                                 .Length);

				Assert.AreEqual(0,
				                IntersectionUtils.GetZOnlyDifferenceLines(
					                                 (IPolycurve) resultsAo[1],
					                                 (IPolycurve) resultsGeom[1], 0.001)
				                                 .Length);
			}
			finally
			{
				IntersectionUtils.UseCustomIntersect = customIntersectOrig;
			}
		}

		[Test]
		public void CanCutPolygonWithZSourceInterpolate()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 100, 100, lv95));

			IPolygon innerRingPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 10, 10, lv95));

			Plane3D plane = Plane3D.FitPlane(new List<Pnt3D>
			                                 {
				                                 new Pnt3D(2600000, 1200000, 500),
				                                 new Pnt3D(2600050, 1200100, 550),
				                                 new Pnt3D(2600000, 1200100, 580)
			                                 });

			((IGeometryCollection) originalPoly).AddGeometryCollection(
				(IGeometryCollection) innerRingPoly);

			GeometryUtils.Simplify(originalPoly);

			ChangeAlongZUtils.AssignZ((IPointCollection) originalPoly, plane);

			// The non-z-aware cut line cuts north of the inner ring -> the inner ring should be assigned to the southern result
			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600000 - 100, 1200020),
				GeometryFactory.CreatePoint(2600000 - 40, 1200020),
				GeometryFactory.CreatePoint(2600000 - 40, 1200040),
				GeometryFactory.CreatePoint(2600000 + 40, 1200040),
				GeometryFactory.CreatePoint(2600000 + 40, 1200020),
				GeometryFactory.CreatePoint(2600000 + 100, 1200020));
			cutLine.SpatialReference = lv95;

			bool customIntersectOrig = IntersectionUtils.UseCustomIntersect;

			try
			{
				IntersectionUtils.UseCustomIntersect = false;

				const ChangeAlongZSource zSource = ChangeAlongZSource.InterpolatedSource;

				var resultsAo =
					CutGeometryUtils.TryCut(originalPoly, cutLine, zSource);

				IntersectionUtils.UseCustomIntersect = true;
				var resultsGeom =
					CutGeometryUtils.TryCut(originalPoly, cutLine, zSource);

				Assert.NotNull(resultsAo);
				Assert.NotNull(resultsGeom);

				EnsureCutResult(resultsAo, originalPoly, null, 3);
				EnsureCutResult(resultsGeom, originalPoly, null, 3);

				// NOTE: The results have different start/end points, therefore GeometryUtils.AreEqual is false
				Assert.True(GeometryUtils.AreEqualInXY(resultsAo[0], resultsGeom[0]));
				Assert.True(GeometryUtils.AreEqualInXY(resultsAo[1], resultsGeom[1]));

				Assert.AreEqual(0,
				                IntersectionUtils.GetZOnlyDifferenceLines(
					                                 (IPolycurve) resultsAo[0],
					                                 (IPolycurve) resultsGeom[0], 0.001)
				                                 .Length);

				Assert.AreEqual(0,
				                IntersectionUtils.GetZOnlyDifferenceLines(
					                                 (IPolycurve) resultsAo[1],
					                                 (IPolycurve) resultsGeom[1], 0.001)
				                                 .Length);
			}
			finally
			{
				IntersectionUtils.UseCustomIntersect = customIntersectOrig;
			}
		}

		[Test]
		public void CannotCutPolygonWithCutLineWithinTolerance()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 100, 100, lv95));

			IPolygon innerRingPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 10, 10, lv95));

			((IGeometryCollection) originalPoly).AddGeometryCollection(
				(IGeometryCollection) innerRingPoly);

			GeometryUtils.Simplify(originalPoly);

			// The cut line runs almost (within tolerance) along the inner ring
			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600000 + 5, 1200000 - 5),
				GeometryFactory.CreatePoint(2600000 + 5.001, 1200000),
				GeometryFactory.CreatePoint(2600000 + 5, 1200000 + 5));
			cutLine.SpatialReference = lv95;

			bool customIntersectOrig = IntersectionUtils.UseCustomIntersect;

			try
			{
				IntersectionUtils.UseCustomIntersect = false;

				const ChangeAlongZSource zSource = ChangeAlongZSource.InterpolatedSource;

				var resultsAo =
					CutGeometryUtils.TryCut(originalPoly, cutLine, zSource);

				IntersectionUtils.UseCustomIntersect = true;
				var resultsGeom =
					CutGeometryUtils.TryCut(originalPoly, cutLine, zSource);

				Assert.Null(resultsAo);
				Assert.Null(resultsGeom);
			}
			finally
			{
				IntersectionUtils.UseCustomIntersect = customIntersectOrig;
			}
		}

		[Test]
		public void CannotCutMultipartPolygonWithCutLineWithinTolerance()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 100, 100, lv95));

			IPolygon innerRingPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 10, 10, lv95));

			IPolygon secondPart = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2700000, 1200000, 500, 100, 100, lv95));

			((IGeometryCollection) originalPoly).AddGeometryCollection(
				(IGeometryCollection) innerRingPoly);

			((IGeometryCollection) originalPoly).AddGeometryCollection(
				(IGeometryCollection) secondPart);

			GeometryUtils.Simplify(originalPoly);

			// The cut line runs along the second ring (and diverts off to the outside)
			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2700000 + 50, 1200000 - 30),
				GeometryFactory.CreatePoint(2700000 + 50, 1200000),
				GeometryFactory.CreatePoint(2700000 + 130, 1200000 + 50));

			cutLine.SpatialReference = lv95;

			bool customIntersectOrig = IntersectionUtils.UseCustomIntersect;

			try
			{
				IntersectionUtils.UseCustomIntersect = false;

				const ChangeAlongZSource zSource = ChangeAlongZSource.InterpolatedSource;

				var resultsAo =
					CutGeometryUtils.TryCut(originalPoly, cutLine, zSource);

				IntersectionUtils.UseCustomIntersect = true;
				var resultsGeom =
					CutGeometryUtils.TryCut(originalPoly, cutLine, zSource);

				Assert.Null(resultsAo);
				Assert.Null(resultsGeom);
			}
			finally
			{
				IntersectionUtils.UseCustomIntersect = customIntersectOrig;
			}
		}

		[Test]
		public void CannotCutPolygonWithDisjointCutLine()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 100, 100, lv95));

			IPolygon innerRingPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 10, 10, lv95));

			((IGeometryCollection) originalPoly).AddGeometryCollection(
				(IGeometryCollection) innerRingPoly);

			GeometryUtils.Simplify(originalPoly);

			// The cut line runs almost (within tolerance) along the inner ring
			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600000 + 1000, 1200000),
				GeometryFactory.CreatePoint(2600000 + 1000, 1200000),
				GeometryFactory.CreatePoint(2600000 + 1000, 1200000));

			cutLine.SpatialReference = lv95;

			bool customIntersectOrig = IntersectionUtils.UseCustomIntersect;

			try
			{
				IntersectionUtils.UseCustomIntersect = false;

				const ChangeAlongZSource zSource = ChangeAlongZSource.InterpolatedSource;

				var resultsAo =
					CutGeometryUtils.TryCut(originalPoly, cutLine, zSource);

				IntersectionUtils.UseCustomIntersect = true;
				var resultsGeom =
					CutGeometryUtils.TryCut(originalPoly, cutLine, zSource);

				Assert.Null(resultsAo);
				Assert.Null(resultsGeom);
			}
			finally
			{
				IntersectionUtils.UseCustomIntersect = customIntersectOrig;
			}
		}

		private static void EnsureCutResult(IList<IGeometry> results,
		                                    IPolygon originalPoly,
		                                    Plane3D plane,
		                                    int expectedResultPartCount)
		{
			double areaSum = 0;
			var partCount = 0;
			foreach (IGeometry result in results)
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				areaSum +=
					GeometryUtils.GetParts((IGeometryCollection) result)
					             .Sum(part => ((IArea) part).Area);

				partCount += GeometryUtils.GetParts((IGeometryCollection) result).Count();

				if (plane != null)
				{
					foreach (IPoint point in GeometryUtils.GetPoints(
						(IPointCollection) result))
					{
						Assert.AreEqual(plane.GetZ(point.X, point.Y), point.Z, 0.001);
					}
				}
			}

			Assert.AreEqual(expectedResultPartCount, partCount);
			Assert.IsTrue(
				MathUtils.AreEqual(((IArea) originalPoly).Area, areaSum));
		}
	}
}
