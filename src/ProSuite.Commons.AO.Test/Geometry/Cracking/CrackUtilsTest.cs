using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Cracking;
using ProSuite.Commons.Geom;
using IPnt = ProSuite.Commons.Geom.IPnt;

namespace ProSuite.Commons.AO.Test.Geometry.Cracking
{
	[TestFixture]
	public class CrackUtilsTest
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
		public void CanWeedPoints3d()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IRing ring = GeometryFactory.CreateRing(
				new[]
				{
					new WKSPointZ {X = 2600000, Y = 1200000, Z = 500},
					new WKSPointZ {X = 2600000, Y = 1200020, Z = 500},
					new WKSPointZ {X = 2600000.009, Y = 1200100, Z = 500},
					new WKSPointZ {X = 2600100, Y = 1200100, Z = 500},
					new WKSPointZ {X = 2600100, Y = 1200060, Z = 522.22},
					new WKSPointZ {X = 2600100, Y = 1200000, Z = 500},
					new WKSPointZ {X = 2600000, Y = 1200000, Z = 500}
				}, lv95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(ring);

			IPointCollection weededPoints2d =
				CrackUtils.GetWeedPoints(originalPoly, 0.01, true, null, false);

			Assert.AreEqual(2, weededPoints2d.PointCount);

			IPointCollection weededPoints3d =
				CrackUtils.GetWeedPoints(originalPoly, 0.01, false, null, false);

			Assert.AreEqual(1, weededPoints3d.PointCount);
		}

		[Test]
		public void CanWeedPoints2d()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IRing ring = GeometryFactory.CreateRing(
				new[]
				{
					new WKSPointZ {X = 2600000, Y = 1200000, Z = double.NaN},
					new WKSPointZ {X = 2600000, Y = 1200020, Z = double.NaN},
					new WKSPointZ {X = 2600000, Y = 1200100, Z = double.NaN},
					new WKSPointZ {X = 2600100, Y = 1200100, Z = double.NaN},
					new WKSPointZ {X = 2600100, Y = 1200060, Z = double.NaN},
					new WKSPointZ {X = 2600100, Y = 1200000, Z = double.NaN},
					new WKSPointZ {X = 2600000, Y = 1200000, Z = double.NaN}
				}, lv95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(ring);

			IPointCollection weededPoints2d =
				CrackUtils.GetWeedPoints(originalPoly, 0.01, true, null, false);

			Assert.AreEqual(2, weededPoints2d.PointCount);

			Assert.Throws<ArgumentException>(
				() => CrackUtils.GetWeedPoints(originalPoly, 0.01, false, null, false));
		}

		[Test]
		public void CanWeedPolygonStartPoint()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IRing ring = GeometryFactory.CreateRing(
				new[]
				{
					new WKSPointZ {X = 2600000, Y = 1200020, Z = 500},
					new WKSPointZ {X = 2600000, Y = 1200100, Z = 500},
					new WKSPointZ {X = 2600100, Y = 1200100, Z = 500},
					new WKSPointZ {X = 2600100, Y = 1200060, Z = 520},
					new WKSPointZ {X = 2600100, Y = 1200000, Z = 500},
					new WKSPointZ {X = 2600000, Y = 1200000, Z = 500},
					new WKSPointZ {X = 2600000, Y = 1200020, Z = 500},
				}, lv95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(ring);

			IPointCollection weededPoints2d =
				CrackUtils.GetWeedPoints(originalPoly, 0.01, true, null, false);

			Assert.AreEqual(2, weededPoints2d.PointCount);

			IPointCollection weededPoints3d =
				CrackUtils.GetWeedPoints(originalPoly, 0.01, false, null, false);

			Assert.AreEqual(1, weededPoints3d.PointCount);
		}

		[Test]
		public void CanWeedPolygonWithNonLinearSegments()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IPoint point1 = GeometryFactory.CreatePoint(2600000, 1200000, lv95);
			IPoint point2 = GeometryFactory.CreatePoint(2600010, 1200025, lv95);
			IPoint point3 = GeometryFactory.CreatePoint(2600000, 1200050, lv95);

			ICircularArc arc1 = GeometryFactory.CreateCircularArc(point1, point2, point3);

			IPoint point4 = GeometryFactory.CreatePoint(2600010, 1200075, lv95);
			IPoint point5 = GeometryFactory.CreatePoint(2600000, 1200100, lv95);

			ICircularArc arc2 = GeometryFactory.CreateCircularArc(point3, point4, point5);

			IRing ring = GeometryFactory.CreateEmptyRing(false, false, lv95);

			object missing = Type.Missing;
			((ISegmentCollection) ring).AddSegment((ISegment) arc1, ref missing, ref missing);
			((ISegmentCollection) ring).AddSegment((ISegment) arc2, ref missing, ref missing);

			IPoint point6 = GeometryFactory.CreatePoint(2600100, 1200100, lv95);
			IPoint point7 = GeometryFactory.CreatePoint(2600100, 1200060, lv95);
			IPoint point8 = GeometryFactory.CreatePoint(2600100, 1200000, lv95);
			IPoint point9 = GeometryFactory.CreatePoint(2600100, 1200000, lv95);

			((IPointCollection) ring).AddPoint(point6, ref missing, ref missing);
			((IPointCollection) ring).AddPoint(point7, ref missing, ref missing);
			((IPointCollection) ring).AddPoint(point8, ref missing, ref missing);
			((IPointCollection) ring).AddPoint(point9, ref missing, ref missing);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(ring);
			GeometryUtils.Simplify(originalPoly);

			IPointCollection weededPointsWithoutArcs =
				CrackUtils.GetWeedPoints(originalPoly, 0.01, true, null, true);

			Assert.AreEqual(1, weededPointsWithoutArcs.PointCount);

			IPointCollection weededPointsWithArcs =
				CrackUtils.GetWeedPoints(originalPoly, 0.01, false, null, false);

			Assert.AreEqual(318, weededPointsWithArcs.PointCount);

			IFeature mockFeature = TestUtils.CreateMockFeature(originalPoly, 0.01, 0.001);

			FeatureVertexInfo vertexInfo = new FeatureVertexInfo(mockFeature, null)
			                               {
				                               LinearizeSegments = true,
				                               PointsToDelete = weededPointsWithArcs
			                               };

			var resultGeometries = new Dictionary<IFeature, IGeometry>();
			CrackUtils.AddRemovePoints(new List<FeatureVertexInfo> {vertexInfo}, resultGeometries,
			                           null, null);

			IPolycurve resultGeometry = resultGeometries[mockFeature] as IPolycurve;

			Assert.IsNotNull(resultGeometry);
			const int expectedRemaining = 291;
			Assert.AreEqual(expectedRemaining, GeometryUtils.GetPointCount(resultGeometry));
			Assert.IsFalse(GeometryUtils.HasNonLinearSegments(resultGeometry));

			MultiPolycurve result = GeometryConversionUtils.CreateMultiPolycurve(resultGeometry);

			Assert.AreEqual(expectedRemaining, result.PointCount);
		}

		[Test]
		public void CanWeedDeterministically()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IPoint point1 = GeometryFactory.CreatePoint(2600000, 1200000, lv95);
			IPoint point2 = GeometryFactory.CreatePoint(2600010, 1200025, lv95);
			IPoint point3 = GeometryFactory.CreatePoint(2600000, 1200050, lv95);

			ICircularArc arc1 = GeometryFactory.CreateCircularArc(point1, point2, point3);

			IPoint point4 = GeometryFactory.CreatePoint(2600010, 1200075, lv95);
			IPoint point5 = GeometryFactory.CreatePoint(2600000, 1200100, lv95);

			ICircularArc arc2 = GeometryFactory.CreateCircularArc(point3, point4, point5);

			IPath path = GeometryFactory.CreateEmptyPath(false, false, lv95);

			object missing = Type.Missing;
			((ISegmentCollection) path).AddSegment((ISegment) arc1, ref missing, ref missing);
			((ISegmentCollection) path).AddSegment((ISegment) arc2, ref missing, ref missing);

			IPoint point6 = GeometryFactory.CreatePoint(2600100, 1200100, lv95);
			IPoint point7 = GeometryFactory.CreatePoint(2600100, 1200060, lv95);
			IPoint point8 = GeometryFactory.CreatePoint(2600100, 1200000, lv95);

			((IPointCollection) path).AddPoint(point6, ref missing, ref missing);
			((IPointCollection) path).AddPoint(point7, ref missing, ref missing);
			((IPointCollection) path).AddPoint(point8, ref missing, ref missing);

			IPolyline originalPolyline = GeometryFactory.CreatePolyline(path);
			GeometryUtils.Simplify(originalPolyline);

			IPointCollection weededPointsWithoutArcs =
				CrackUtils.GetWeedPoints(originalPolyline, 0.01, true, null, true);

			Assert.AreEqual(1, weededPointsWithoutArcs.PointCount);

			IPointCollection weededPointsWithArcs =
				CrackUtils.GetWeedPoints(originalPolyline, 0.01, false, null, false, lv95);

			const int expectedWeededPoints = 318;
			Assert.AreEqual(expectedWeededPoints, weededPointsWithArcs.PointCount);

			originalPolyline.ReverseOrientation();

			IPointCollection weededPointsWithArcsReversed =
				CrackUtils.GetWeedPoints(originalPolyline, 0.01, false, null, false, lv95);

			Assert.AreEqual(expectedWeededPoints, weededPointsWithArcsReversed.PointCount);

			Multipoint<IPnt> weedPnts =
				GeometryConversionUtils.CreateMultipoint((IMultipoint) weededPointsWithArcs);
			Multipoint<IPnt> weedPntsReversed =
				GeometryConversionUtils.CreateMultipoint(
					(IMultipoint) weededPointsWithArcsReversed);

			var differentWeedPoints =
				GeomTopoOpUtils.GetDifferencePoints(weedPnts, weedPntsReversed, 0.001, true)
				               .ToList();

			Assert.AreEqual(0, differentWeedPoints.Count);

			IFeature mockFeature = TestUtils.CreateMockFeature(originalPolyline, 0.01, 0.001);

			FeatureVertexInfo vertexInfo = new FeatureVertexInfo(mockFeature, null)
			                               {
				                               LinearizeSegments = true,
				                               PointsToDelete = weededPointsWithArcs
			                               };

			var resultGeometries = new Dictionary<IFeature, IGeometry>();
			CrackUtils.AddRemovePoints(new List<FeatureVertexInfo> {vertexInfo}, resultGeometries,
			                           null, null);

			IPolycurve resultGeometry = resultGeometries[mockFeature] as IPolycurve;

			Assert.IsNotNull(resultGeometry);
			const int expectedRemaining = 286;
			Assert.AreEqual(expectedRemaining, GeometryUtils.GetPointCount(resultGeometry));
			Assert.IsFalse(GeometryUtils.HasNonLinearSegments(resultGeometry));

			MultiPolycurve result = GeometryConversionUtils.CreateMultiPolycurve(resultGeometry);

			Assert.AreEqual(expectedRemaining, result.PointCount);
		}
	}
}
