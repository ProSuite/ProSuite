using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.Commons.AO.Test.Geometry.ChangeAlong
{
	[TestFixture]
	public class ReshapeInfoTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanDeterminePolygonReshapeSideInsideOnly()
		{
			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IGeometry geometryToReshape =
				GeometryFactory.CreatePolygon(100, 100, 200, 200, spatialReference);

			IGeometry reshapeLine =
				GeometryFactory.CreatePolyline(spatialReference,
				                               GeometryFactory.CreatePoint(175, 100),
				                               GeometryFactory.CreatePoint(175, 200));

			ReshapeInfo reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                                  reshapeLine, false);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Left, 7500, 1, 1);

			const bool useNonDefaultSide = true;
			reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                      reshapeLine, useNonDefaultSide);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Right, 2500, 1, 1);
		}

		[Test]
		public void CanDeterminePolygonReshapeSideOutsideOnly()
		{
			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IGeometry geometryToReshape =
				GeometryFactory.CreatePolygon(100, 100, 200, 200, spatialReference);

			IGeometry reshapeLine =
				GeometryFactory.CreatePolyline(spatialReference,
				                               GeometryFactory.CreatePoint(150, 200),
				                               GeometryFactory.CreatePoint(150, 250),
				                               GeometryFactory.CreatePoint(200, 250),
				                               GeometryFactory.CreatePoint(200, 200));

			ReshapeInfo reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                                  reshapeLine, false);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Right, 12500, 1, 1);

			const bool useNonDefaultSide = true;
			reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                      reshapeLine, useNonDefaultSide);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Right, 12500, 1, 1);
		}

		[Test]
		public void CanDeterminePolygonReshapeSideInsideOnlySeveralParts()
		{
			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IGeometry geometryToReshape =
				GeometryFactory.CreatePolygon(100, 100, 200, 200, spatialReference);

			IGeometry reshapeLine =
				GeometryFactory.CreatePolyline(spatialReference,
				                               GeometryFactory.CreatePoint(100, 200),
				                               GeometryFactory.CreatePoint(150, 100),
				                               GeometryFactory.CreatePoint(200, 200));

			ReshapeInfo reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                                  reshapeLine, false);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Left, 5000, 1, 1);

			const bool useNonDefaultSide = true;
			reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                      reshapeLine, useNonDefaultSide);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Right, 5000, 2, 2);
		}

		[Test]
		public void CanDeterminePolygonReshapeSideInsideOnlySeveralParts_OnIsland()
		{
			// currently implemented solution: always return single part result
			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IGeometry geometryToReshape =
				GeometryFactory.CreatePolygon(100, 100, 200, 200, spatialReference);

			MakeInnerRing(geometryToReshape);

			IGeometry reshapeLine =
				GeometryFactory.CreatePolyline(spatialReference,
				                               GeometryFactory.CreatePoint(100, 200),
				                               GeometryFactory.CreatePoint(150, 100),
				                               GeometryFactory.CreatePoint(200, 200));

			ReshapeInfo reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                                  reshapeLine, false);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Right, 1000000 - 5000, 2, 1);

			// and the inner ring is single part, without boundary loop
			IRing innerRing = GetInnerRing(reshapeInfo);
			IPolygon innerRingPoly = GeometryFactory.CreatePolygon(innerRing, spatialReference);
			GeometryUtils.Simplify(innerRingPoly);

			Assert.AreEqual(1, ((IGeometryCollection) innerRingPoly).GeometryCount);

			const bool useNonDefaultSide = true;
			reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                      reshapeLine, useNonDefaultSide);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Left, 1000000 - 5000, 2, 1);

			// now the inner ring is single part but has a boundary loop
			innerRing = GetInnerRing(reshapeInfo);
			innerRingPoly = GeometryFactory.CreatePolygon(innerRing, spatialReference);
			GeometryUtils.Simplify(innerRingPoly);

			Assert.AreEqual(2, ((IGeometryCollection) innerRingPoly).GeometryCount);
		}

		[Test]
		public void CanDeterminePolygonReshapeSideInsideAndOutsideOnePartWithCutBack()
		{
			//   _                   _
			//  / \                 / \
			//--\--\-----/--        \  \-----/
			//   \      /      ->    \      / 
			//    \____/              \____/

			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IGeometry geometryToReshape =
				GeometryFactory.CreatePolygon(100, 100, 200, 200, spatialReference);

			IGeometry reshapeLine =
				GeometryFactory.CreatePolyline(spatialReference,
				                               GeometryFactory.CreatePoint(125, 200),
				                               GeometryFactory.CreatePoint(125, 150),
				                               GeometryFactory.CreatePoint(175, 150),
				                               GeometryFactory.CreatePoint(175, 250),
				                               GeometryFactory.CreatePoint(150, 250),
				                               GeometryFactory.CreatePoint(150, 200));

			// one quarter plus 50 * 25 = 3750
			ReshapeInfo reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                                  reshapeLine, false);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Left, 3750, 1, 1);

			const bool useNonDefaultSide = true;
			reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                      reshapeLine, useNonDefaultSide);

			// standard inside-outside, not allowing non-default
			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Left, 3750, 1, 1);
		}

		[Test]
		public void CanDeterminePolygonReshapeSideInsideAndOutsideOnePartWithBoundaryLoop()
		{
			//     _
			//    / \
			//   /   \
			//--/--\--\-----
			//      \__\
			// The original polygon is below the horizontal dashed-line: ---

			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IGeometry geometryToReshape =
				GeometryFactory.CreatePolygon(100, 100, 200, 200, spatialReference);

			IGeometry reshapeLine =
				GeometryFactory.CreatePolyline(spatialReference,
				                               GeometryFactory.CreatePoint(125, 200),
				                               GeometryFactory.CreatePoint(125, 250),
				                               GeometryFactory.CreatePoint(175, 250),
				                               GeometryFactory.CreatePoint(175, 150),
				                               GeometryFactory.CreatePoint(150, 150),
				                               GeometryFactory.CreatePoint(150, 200));

			// plus one quarter minus 50 * 25 = 12500 - 1250 = 11250
			ReshapeInfo reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                                  reshapeLine, false);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Right, 11250, 1, 1);

			// has boundary loop:
			MakeInnerRing(reshapeInfo.GeometryToReshape);
			GeometryUtils.Simplify(reshapeInfo.GeometryToReshape);
			Assert.AreEqual(3,
			                ((IGeometryCollection) reshapeInfo.GeometryToReshape).GeometryCount);
			Assert.AreEqual(2, ((IPolygon) reshapeInfo.GeometryToReshape).ExteriorRingCount);

			const bool useNonDefaultSide = true;
			reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                      reshapeLine, useNonDefaultSide);

			// non-boundary loop variant:
			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Left, 3750, 1, 1);

			MakeInnerRing(reshapeInfo.GeometryToReshape);
			GeometryUtils.Simplify(reshapeInfo.GeometryToReshape);
			Assert.AreEqual(2,
			                ((IGeometryCollection) reshapeInfo.GeometryToReshape).GeometryCount);
			Assert.AreEqual(1, ((IPolygon) reshapeInfo.GeometryToReshape).ExteriorRingCount);
		}

		[Test]
		public void CanDeterminePolygonReshapeSideOutsideOnlyCuttingOffIsland()
		{
			//   ________
			//  /  _____ |
			// /__/ h  / |
			//---------------
			// The original polygon is below the reshape line (dashed-line ---)

			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IGeometry geometryToReshape =
				GeometryFactory.CreatePolygon(100, 100, 200, 200, spatialReference);

			IGeometry reshapeLine =
				GeometryFactory.CreatePolyline(spatialReference,
				                               GeometryFactory.CreatePoint(200, 200),
				                               GeometryFactory.CreatePoint(200, 250),
				                               GeometryFactory.CreatePoint(125, 250),
				                               GeometryFactory.CreatePoint(125, 200),
				                               GeometryFactory.CreatePoint(150, 200),
				                               GeometryFactory.CreatePoint(150, 225),
				                               GeometryFactory.CreatePoint(175, 225),
				                               GeometryFactory.CreatePoint(175, 200));

			// plus 50 * 75 minus 25 * 25 = 3750 - 625 = 13125
			ReshapeInfo reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                                  reshapeLine, false);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Left, 13125, 2, 1);

			const bool useNonDefaultSide = true;
			reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                      reshapeLine, useNonDefaultSide);

			// not allowing non-default
			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Left, 13125, 2, 1);
		}

		[Test]
		public void CanDeterminePolygonReshapeSideOutsideOnlyCuttingOffIsland_OnIsland()
		{
			// The below situation is an island ring, being reshaped to the outside of the island (i.e. into the actual polygon)
			//   ________
			//  /  _____ |
			// /__/ h  / |
			//---------------
			// The original polygon is below the reshape line (dashed-line ---)

			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IGeometry geometryToReshape =
				GeometryFactory.CreatePolygon(100, 100, 200, 200, spatialReference);

			MakeInnerRing(geometryToReshape);

			IGeometry reshapeLine =
				GeometryFactory.CreatePolyline(spatialReference,
				                               GeometryFactory.CreatePoint(200, 200),
				                               GeometryFactory.CreatePoint(200, 250),
				                               GeometryFactory.CreatePoint(125, 250),
				                               GeometryFactory.CreatePoint(125, 200),
				                               GeometryFactory.CreatePoint(150, 200),
				                               GeometryFactory.CreatePoint(150, 225),
				                               GeometryFactory.CreatePoint(175, 225),
				                               GeometryFactory.CreatePoint(175, 200));

			// plus 50 * 75 minus 25 * 25 = 3750 - 625 = 13125
			ReshapeInfo reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                                  reshapeLine, false);

			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Right, 1000000 - 13125, 3, 2);

			const bool useNonDefaultSide = true;
			reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                      reshapeLine, useNonDefaultSide);

			// not allowing non-default
			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Right, 1000000 - 13125, 3, 2);
		}

		[Test]
		public void CanReshapeNonLinearGeometry()
		{
			ISpatialReference spatialReference = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IPoint centerPoint = GeometryFactory.CreatePoint(150, 150);
			IGeometry geometryToReshape =
				GeometryFactory.CreateCircleArcPolygon(centerPoint, 50, false);
			geometryToReshape.SpatialReference = spatialReference;

			double circleArea = ((IArea) geometryToReshape).Area;

			IGeometry reshapeLine =
				GeometryFactory.CreatePolyline(spatialReference,
				                               GeometryFactory.CreatePoint(175, 100),
				                               GeometryFactory.CreatePoint(175, 200));

			ReshapeInfo reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                                  reshapeLine, false);

			CutSubcurve cutReshapePath = reshapeInfo.CutReshapePath;
			Assert.NotNull(cutReshapePath);

			// s = 2* sqrt(r^2-(r-h)^2) = 2 * sqrt(2rh-h^2)
			Assert.AreEqual(86.60, Math.Round(cutReshapePath.Path.Length, 2));

			const double area = 6318.5222074138192;
			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Left, area, 1, 1);

			const bool useNonDefaultSide = true;
			reshapeInfo = Reshape(GeometryFactory.Clone(geometryToReshape),
			                      reshapeLine, useNonDefaultSide);

			double otherSide = circleArea - area;
			ExpectResult(reshapeInfo, RingReshapeSideOfLine.Right, otherSide, 1, 1);
		}

		[Test]
		public void CanValidateReshapeLine()
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IPath reshapePath = GeometryFactory.CreatePath(
				GeometryFactory.CreatePoint(2600000.12, 1200000.12, sr),
				GeometryFactory.CreatePoint(2600000.121, 1200000.121, sr));

			IPolyline polyline = GeometryFactory.CreatePolyline(2600000, 1200000, 2600001,
			                                                    1200001);
			var reshapeInfo = new ReshapeInfo(polyline, reshapePath, null);

			// It says it's closed but it's just a bit short
			Assert.IsTrue(reshapePath.IsClosed);
			Assert.IsFalse(reshapeInfo.ValidateReshapePath());

			// even with 3 points:
			WKSPoint middlePoint;
			middlePoint.X = 2600000.1205;
			middlePoint.Y = 1200000.1205;
			((IPointCollection) reshapePath).InsertWKSPoints(1, 1, middlePoint);

			reshapeInfo = new ReshapeInfo(polyline, reshapePath, null);

			Assert.IsTrue(reshapePath.IsClosed);
			Assert.IsFalse(reshapeInfo.ValidateReshapePath());
		}

		private static IRing GetInnerRing(ReshapeInfo reshapeInfo)
		{
			return
				GeometryUtils.GetParts((IGeometryCollection) reshapeInfo.GeometryToReshape).Cast
					<IRing>().FirstOrDefault(ring => ! ring.IsExterior);
		}

		private static void MakeInnerRing(IGeometry geometryToReshape)
		{
			IGeometry outerRingPoly = GeometryFactory.CreatePolygon(0, 0, 1000, 1000);

			((IGeometryCollection) geometryToReshape).AddGeometryCollection(
				(IGeometryCollection) outerRingPoly);

			GeometryUtils.Simplify(geometryToReshape);
		}

		private static void ExpectResult(ReshapeInfo reshapeInfo,
		                                 RingReshapeSideOfLine reshapeSide, double area,
		                                 int ringCount, int exteriorRingCount)
		{
			Assert.AreEqual(reshapeSide, reshapeInfo.RingReshapeSide);

			Assert.AreEqual(Math.Round(area, 2),
			                Math.Round(((IArea) reshapeInfo.GeometryToReshape).Area, 2),
			                "Wrong result area.");

			Assert.AreEqual(ringCount,
			                ((IGeometryCollection) reshapeInfo.GeometryToReshape).GeometryCount,
			                "Wrong number of rings in result.");
			Assert.AreEqual(exteriorRingCount,
			                ((IPolygon) reshapeInfo.GeometryToReshape).ExteriorRingCount,
			                "Wrong number of exterior rings in result.");
		}

		private static ReshapeInfo Reshape(IGeometry geometryToReshape,
		                                   IGeometry reshapeLine,
		                                   bool tryReshapeNonDefaultSide)
		{
			var reshapePath = (IPath) ((IGeometryCollection) reshapeLine).get_Geometry(0);

			var reshapeInfo = new ReshapeInfo(geometryToReshape, reshapePath, null);

			reshapeInfo.ReshapeResultFilter = new ReshapeResultFilter(tryReshapeNonDefaultSide);

			reshapeInfo.IdentifyUniquePartIndexToReshape(out IList<int> _);

			Assert.IsTrue(ReshapeUtils.ReshapePolygonOrMultipatch(reshapeInfo));

			GeometryUtils.Simplify(geometryToReshape);

			return reshapeInfo;
		}
	}
}
