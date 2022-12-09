using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Cracking;
using ProSuite.Commons.AO.Geometry.Generalize;
using ProSuite.Commons.Geom;
using IPnt = ProSuite.Commons.Geom.IPnt;

namespace ProSuite.Commons.AO.Test.Geometry.Generalize
{
	[TestFixture]
	public class GeneralizeUtilsTest
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
		public void CanCalculateWeedPointsBetweenFeatures()
		{
			// Both features are selected and should be weeded
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IRing ring = GeometryFactory.CreateRing(
				new[]
				{
					new WKSPointZ {X = 2600000, Y = 1200000, Z = 500},
					new WKSPointZ {X = 2600000.001, Y = 1200020, Z = 500},
					new WKSPointZ {X = 2600000, Y = 1200100, Z = 500},
					new WKSPointZ {X = 2600100, Y = 1200100, Z = 500},
					new WKSPointZ {X = 2600100, Y = 1200060, Z = 522.22},
					new WKSPointZ {X = 2600100, Y = 1200000, Z = 500},
					new WKSPointZ {X = 2600000, Y = 1200000, Z = 500}
				}, lv95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(ring);

			IPolygon neighbourPolygon =
				(IPolygon) IntersectionUtils.Difference(
					GeometryFactory.CreatePolygon(2599950, 1200000, 2600020, 1200200, lv95),
					originalPoly);

			IFeature mockFeature1 = TestUtils.CreateMockFeature(originalPoly, 0.01, 0.001);
			IFeature mockFeature2 = TestUtils.CreateMockFeature(neighbourPolygon, 0.01, 0.001);

			// Scenario 1: mockFeature1 is selected, mockFeature2 protects the weed-able point at 2600000.001, 1200020
			IList<FeatureVertexInfo> featureVertexInfos = CrackUtils.CreateFeatureVertexInfos(
				new[] {mockFeature1}, null, 0.01, 0.1);

			CrackPointCalculator crackPointCalculator =
				GeneralizeUtils.CreateProtectedPointsCalculator();

			// All points: between selected and unselected
			crackPointCalculator.IntersectionPointOption =
				IntersectionPointOptions.IncludeLinearIntersectionAllPoints;

			CrackUtils.AddTargetIntersectionCrackPoints(
				featureVertexInfos, new[] {mockFeature2},
				TargetFeatureSelection.SelectedFeatures, crackPointCalculator, null);

			GeneralizeUtils.CalculateWeedPoints(featureVertexInfos, 0.001, false, false,
			                                    null, null);

			// No weeding!
			Assert.AreEqual(0, featureVertexInfos.Sum(v => v.PointsToDelete?.PointCount));

			// Scenario 2: Both features are selected, the weed-able point at 2600000.001, 1200020 is removed
			featureVertexInfos = CrackUtils.CreateFeatureVertexInfos(
				new[] {mockFeature1, mockFeature2}, null, 0.01, 0.1);

			crackPointCalculator = GeneralizeUtils.CreateProtectedPointsCalculator();

			// Only protect end points of intersection sequences
			crackPointCalculator.IntersectionPointOption =
				IntersectionPointOptions.IncludeLinearIntersectionEndpoints;

			CrackUtils.AddTargetIntersectionCrackPoints(
				featureVertexInfos, new[] {mockFeature2},
				TargetFeatureSelection.SelectedFeatures, crackPointCalculator, null);

			GeneralizeUtils.CalculateWeedPoints(featureVertexInfos, 0.002, false, false,
			                                    null, null);
			// Weeding both features!
			Assert.AreEqual(2, featureVertexInfos.Sum(v => v.PointsToDelete?.PointCount));
		}

		[Test]
		public void CanCalculateWeedPointsBetweenFeaturesNonLinear()
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

			IPolygon neighbourPolygon =
				(IPolygon) IntersectionUtils.Difference(
					GeometryFactory.CreatePolygon(2599950, 1200000, 2600020, 1200200, lv95),
					originalPoly);

			IFeature mockFeature1 = TestUtils.CreateMockFeature(originalPoly, 0.01, 0.001);
			IFeature mockFeature2 = TestUtils.CreateMockFeature(neighbourPolygon, 0.01, 0.001);

			// Scenario 1: mockFeature1 is selected, mockFeature2 protects the weed-able points along the non-linear stretch
			var selectedFeatures = new[] {mockFeature1};
			var visibleFeatures = new[] {mockFeature1, mockFeature2};
			IList<FeatureVertexInfo> featureVertexInfos = CrackUtils.CreateFeatureVertexInfos(
				selectedFeatures, null, 0.01, 0.1);

			GeneralizeUtils.CalculateProtectionPoints(featureVertexInfos, selectedFeatures,
			                                          visibleFeatures, true,
			                                          TargetFeatureSelection.VisibleFeatures,
			                                          null);

			GeneralizeUtils.CalculateWeedPoints(featureVertexInfos, 0.1, false, false,
			                                    null, null);

			// Weeded unprotected point at 2600100, 1200060!
			Assert.AreEqual(1, featureVertexInfos.Sum(v => v.PointsToDelete?.PointCount));

			// Scenario 2: Both features are selected, all weed-able points along the non-linear stretch are removed
			selectedFeatures = visibleFeatures;
			featureVertexInfos = CrackUtils.CreateFeatureVertexInfos(
				selectedFeatures, null, 0.01, 0.1);

			GeneralizeUtils.CalculateProtectionPoints(featureVertexInfos, selectedFeatures,
			                                          visibleFeatures, true,
			                                          TargetFeatureSelection.VisibleFeatures,
			                                          null);

			// NOTE: The standard ramer-douglas-peucker is sensitive to the point order (and the start point)
			// -> This is addressed by
			//    - Cutting the geometries at crack points -> generalize identical segment stretches (the shared parts)
			//    - Make generalize insensitive to point order by standardizing the point order

			GeneralizeUtils.CalculateWeedPoints(featureVertexInfos, 0.1, false, false, null, null);

			Assert.NotNull((IMultipoint) featureVertexInfos[0].PointsToDelete);
			Assert.NotNull((IMultipoint) featureVertexInfos[1].PointsToDelete);

			// Weeding both features!
			const int expectedPointsToDelete = 757;
			Assert.AreEqual(expectedPointsToDelete, featureVertexInfos.Sum(v => v.PointsToDelete?.PointCount));

			// Compare the weed points:

			Multipoint<IPnt> weedPnts0 =
				GeometryConversionUtils.CreateMultipoint(
					(IMultipoint) featureVertexInfos[0].PointsToDelete);
			Multipoint<IPnt> weedPnts1 =
				GeometryConversionUtils.CreateMultipoint(
					(IMultipoint) featureVertexInfos[1].PointsToDelete);

			var differentWeedPoints =
				GeomTopoOpUtils.GetDifferencePoints(weedPnts0, weedPnts1, 0.001, true).ToList();

			// The un-protected point at 2600100, 1200060. The points along the arc are removed in the same way in both geometries!
			Assert.AreEqual(1, differentWeedPoints.Count);

			foreach (FeatureVertexInfo featureVertexInfo in featureVertexInfos)
			{
				featureVertexInfo.LinearizeSegments = true;
			}

			var resultGeometries = new Dictionary<IFeature, IGeometry>();
			CrackUtils.RemovePoints(featureVertexInfos, resultGeometries, null, null);

			foreach (FeatureVertexInfo featureVertexInfo in featureVertexInfos)
			{
				IFeature feature = featureVertexInfo.Feature;

				IGeometry updatedGeometry = resultGeometries[feature];
				updatedGeometry.SnapToSpatialReference();
				feature.Shape = updatedGeometry;
				feature.Store();
			}

			// Check:
			featureVertexInfos = CrackUtils.CreateFeatureVertexInfos(
				new[] {mockFeature1, mockFeature2}, null, 0.01, 0.1);

			GeneralizeUtils.CalculateProtectionPoints(
				featureVertexInfos, selectedFeatures, visibleFeatures, true,
				TargetFeatureSelection.VisibleFeatures, null);

			GeneralizeUtils.CalculateWeedPoints(featureVertexInfos, 0.1, false, false,
			                                    null, null);

			Assert.AreEqual(4, featureVertexInfos.Sum(v => v.CrackPoints?.Count));

			Assert.AreEqual(500, featureVertexInfos.Sum(v => v.PointsToDelete?.PointCount));
		}

		[Test]
		public void CanGetShortSegments()
		{
			string filePath = TestData.GetHugeLockergesteinPolygonPath();
			IPolygon bigPoly = (IPolygon) TestUtils.ReadGeometryFromXml(filePath);

			bool useCustomIntersect = IntersectionUtils.UseCustomIntersect;

			try
			{
				Stopwatch watch = Stopwatch.StartNew();
				IntersectionUtils.UseCustomIntersect = true;
				IList<esriSegmentInfo> segmentInfosCustom =
					GeneralizeUtils.GetShortSegments(bigPoly, null, 1.5, false);

				watch.Stop();
				Console.WriteLine(
					$"Custom: Found {segmentInfosCustom.Count} short segments ({watch.ElapsedMilliseconds}ms)");

				watch.Restart();
				IntersectionUtils.UseCustomIntersect = false;
				IList<esriSegmentInfo> segmentInfosAo =
					GeneralizeUtils.GetShortSegments(bigPoly, null, 1.5, false);

				watch.Stop();
				Console.WriteLine(
					$"AO: Found {segmentInfosAo.Count} short segments ({watch.ElapsedMilliseconds}ms)");

				Assert.AreEqual(segmentInfosCustom.Count, segmentInfosAo.Count);

				for (var i = 0; i < segmentInfosAo.Count; i++)
				{
					esriSegmentInfo segmentInfoAo = segmentInfosAo[i];
					esriSegmentInfo segmentInfoCustom = segmentInfosCustom[i];

					Assert.AreEqual(segmentInfoAo.iAbsSegment, segmentInfoCustom.iAbsSegment);
					Assert.AreEqual(segmentInfoAo.iPart, segmentInfoCustom.iPart);
					Assert.AreEqual(segmentInfoAo.iRelSegment, segmentInfoCustom.iRelSegment);

					GeometryUtils.AreEqual(segmentInfoAo.pSegment.FromPoint,
					                       segmentInfoCustom.pSegment.FromPoint);

					GeometryUtils.AreEqual(segmentInfoAo.pSegment.ToPoint,
					                       segmentInfoCustom.pSegment.ToPoint);
				}
			}
			finally
			{
				IntersectionUtils.UseCustomIntersect = useCustomIntersect;
			}
		}
	}
}
