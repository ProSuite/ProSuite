using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.ChangeAlong;
using ProSuite.Commons.AO.Geometry.Cut;
using ProSuite.Commons.AO.Geometry.ZAssignment;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.Commons.AO.Test.Geometry.Cut
{
	[TestFixture]
	public class FeatureCutterTest
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
		public void CanCutPolygonWithCorrectZ_Top4666()
		{
			// Tests the work-around for TOP-4666
			// TODO: Report ArcObjects bug to Esri Inc.
			IFeature polygonFeature = TestUtils.CreateMockFeature("PolygonTop4666.xml");

			var cutLine =
				(IPolyline)
				TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("CutLineTop4666.xml"));

			GeometryUtils.EnsureSpatialReference(
				cutLine, polygonFeature.Shape.SpatialReference);

			var cutter = new FeatureCutter(new[] {polygonFeature});

			Stopwatch watch = Stopwatch.StartNew();

			cutter.Cut(cutLine);

			watch.Stop();

			Console.WriteLine("Cut large feature: {0}ms", watch.ElapsedMilliseconds);

			Assert.AreEqual(5, cutter.ResultGeometriesByFeature[polygonFeature].Count);

			foreach (IGeometry geometry in cutter.ResultGeometriesByFeature[
				polygonFeature])
			{
				var polygon = (IPolygon) geometry;

				Assert.False(GeometryUtils.HasUndefinedZValues(polygon));

				GeometryUtils.Simplify(polygon);

				Assert.False(GeometryUtils.HasUndefinedZValues(polygon));

				foreach (IPoint point in GeometryUtils.GetPoints(
					(IPointCollection) polygon))
				{
					Assert.AreNotEqual(0, point.Z);
				}

				IPolyline resultAsPolyline = GeometryFactory.CreatePolyline(polygon);

				IPolyline zDifference =
					ReshapeUtils.GetZOnlyDifference(resultAsPolyline, cutLine);

				// ArcObjects uses the cutLine's Z also at the intersection points.
				// Do the same in CustomIntersect?
				Assert.IsNull(zDifference);
			}
		}

		[Test]
		public void CanCutMultipartPolyline()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPath path1 = GeometryFactory.CreatePath(
				GeometryFactory.CreatePoint(2600000, 1200000, lv95),
				GeometryFactory.CreatePoint(2600010, 1200000, lv95));

			IPath path2 = GeometryFactory.CreatePath(
				GeometryFactory.CreatePoint(2600000, 1200002, lv95),
				GeometryFactory.CreatePoint(2600010, 1200002, lv95));

			ICollection<IPath> pathCollection = new List<IPath> {path1, path2};

			IPolyline multipartPolyline = GeometryFactory.CreatePolyline(
				pathCollection, lv95);

			// cuts the lower path and assigns the western part to the original feature
			IPolyline cutOnePartLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600006, 1200000),
				GeometryFactory.CreatePoint(2600006, 1200001));
			cutOnePartLine.SpatialReference = lv95;

			IFeature mockFeature = TestUtils.CreateMockFeature(multipartPolyline);

			var cutter = new FeatureCutter(new[] {mockFeature});

			cutter.Cut(cutOnePartLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double sizeSum = 0;
			foreach (IGeometry result in results)
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				sizeSum +=
					GeometryUtils.GetParts((IGeometryCollection) result)
					             .Sum(part => ((ICurve) part).Length);
			}

			Assert.IsTrue(
				MathUtils.AreEqual(((ICurve) multipartPolyline).Length, sizeSum));

			// cuts both remaining parts into 4 pieces
			// Consider re-joining the parts at the left of the line and those at the right
			// But this logic would also have to be applied for polygons and should probably be optional
			IPolyline cutBothPartsLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600004, 1200000),
				GeometryFactory.CreatePoint(2600004, 1200002));
			cutBothPartsLine.SpatialReference = lv95;

			// the largest ist the first
			mockFeature.Shape = results[0];

			cutter = new FeatureCutter(new[] {mockFeature});

			cutter.Cut(cutBothPartsLine);

			results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			sizeSum = 0;
			foreach (IGeometry result in results)
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				sizeSum +=
					GeometryUtils.GetParts((IGeometryCollection) result)
					             .Sum(part => ((ICurve) part).Length);
			}

			Assert.IsTrue(
				MathUtils.AreEqual(((ICurve) mockFeature.Shape).Length, sizeSum));
		}

		[Test]
		public void CanCutMultipatch()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 50, 20, lv95));

			IMultiPatch multiPatch = GeometryFactory.CreateMultiPatch(originalPoly);

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600000 - 100, 1200000),
				GeometryFactory.CreatePoint(2600000 + 100, 1200000));
			cutLine.SpatialReference = lv95;

			IFeature mockFeature = TestUtils.CreateMockFeature(multiPatch);

			var cutter = new FeatureCutter(new[] {mockFeature});
			cutter.ZSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					string.Empty, ChangeAlongZSource.SourcePlane);

			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double areaSum = 0;
			foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				areaSum +=
					GeometryUtils.GetParts((IGeometryCollection) result)
					             .Sum(part => ((IArea) part).Area);
			}

			Assert.IsTrue(MathUtils.AreEqual(((IArea) originalPoly).Area, areaSum));
		}

		[Test]
		public void CanCutMultipatchWithInnerRing()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 50, 50, lv95));

			IPolygon innerRingPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 10, 10, lv95));
			var innerRing = (IRing) ((IGeometryCollection) innerRingPoly).Geometry[0];
			innerRing.ReverseOrientation();

			IMultiPatch multiPatch = GeometryFactory.CreateMultiPatch(originalPoly);

			GeometryFactory.AddRingToMultiPatch(innerRing, multiPatch,
			                                    esriMultiPatchRingType
				                                    .esriMultiPatchInnerRing);

			// cut line cuts north of the inner ring -> the inner ring should be assigned to the southern result
			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600000 - 100, 1200020),
				GeometryFactory.CreatePoint(2600000 + 100, 1200020));
			cutLine.SpatialReference = lv95;

			IFeature mockFeature = TestUtils.CreateMockFeature(multiPatch);

			var cutter = new FeatureCutter(new[] {mockFeature});
			cutter.ZSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					string.Empty, ChangeAlongZSource.SourcePlane);

			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double areaSum = 0;
			var partCount = 0;
			foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				areaSum +=
					GeometryUtils.GetParts((IGeometryCollection) result)
					             .Sum(part => ((IArea) part).Area);

				partCount += GeometryUtils.GetParts((IGeometryCollection) result).Count();
			}

			Assert.AreEqual(3, partCount);
			Assert.IsTrue(
				MathUtils.AreEqual(
					((IArea) originalPoly).Area + ((IArea) innerRingPoly).Area,
					areaSum));
		}

		[Test]
		public void CanCutMultipatchWithInnerRingThroughInnerRing()
		{
			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			IPolygon originalPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 50, 50, lv95));

			IPolygon innerRingPoly = GeometryFactory.CreatePolygon(
				GeometryFactory.CreateEnvelope(2600000, 1200000, 500, 10, 10, lv95));
			var innerRing = (IRing) ((IGeometryCollection) innerRingPoly).Geometry[0];
			innerRing.ReverseOrientation();

			IMultiPatch multiPatch = GeometryFactory.CreateMultiPatch(originalPoly);

			GeometryFactory.AddRingToMultiPatch(innerRing, multiPatch,
			                                    esriMultiPatchRingType
				                                    .esriMultiPatchInnerRing);

			// cut line cuts through the inner ring 
			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600000 - 100, 1200000),
				GeometryFactory.CreatePoint(2600000 + 100, 1200000));
			cutLine.SpatialReference = lv95;

			IFeature mockFeature = TestUtils.CreateMockFeature(multiPatch);

			var cutter = new FeatureCutter(new[] {mockFeature});
			cutter.ZSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					string.Empty, ChangeAlongZSource.SourcePlane);

			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double areaSum = 0;
			var partCount = 0;
			foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				areaSum +=
					GeometryUtils.GetParts((IGeometryCollection) result)
					             .Sum(part => ((IArea) part).Area);

				partCount += GeometryUtils.GetParts((IGeometryCollection) result).Count();
			}

			Assert.AreEqual(2, partCount);

			Assert.AreEqual(((IArea) originalPoly).Area + ((IArea) innerRing).Area,
			                areaSum, 0.0001);
		}

		[Test]
		public void CanCutMultipatchWithVerticalWalls()
		{
			// {FE286920-3D4C-4CB3-AC22-51056B97A23F} from TLM:
			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithVerticalWalls.xml");

			ISpatialReference lv95 = mockFeature.Shape.SpatialReference;

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2574909.000, 1196870.000, lv95),
				GeometryFactory.CreatePoint(2574924.000, 1196878.000, lv95));

			double originalArea = ((IArea) mockFeature.Shape).Area;
			double originalPartAreaSum =
				GeometryUtils.GetParts((IGeometryCollection) mockFeature.Shape)
				             .Sum(part => ((IArea) part).Area);

			var cutter = new FeatureCutter(new[] {mockFeature});
			cutter.ZSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					string.Empty, ChangeAlongZSource.SourcePlane);

			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double areaSum = 0;
			double partAreaSum = 0;
			foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				Assert.AreEqual(3, ((IGeometryCollection) result).GeometryCount);

				areaSum += ((IArea) result).Area;

				partAreaSum +=
					GeometryUtils.GetParts((IGeometryCollection) result)
					             .Sum(part => ((IArea) part).Area);
			}

			// The sum of all parts is not the same because some rings have negative orientation
			Assert.AreEqual(originalArea, areaSum, 0.01);
			Assert.AreEqual(originalPartAreaSum, partAreaSum, 0.01);
		}

		[Test]
		public void CanCutMultipatchWithVerticalWallsThroughVerticalWalls()
		{
			// {FE286920-3D4C-4CB3-AC22-51056B97A23F} from TLM:
			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithVerticalWalls.xml");

			ISpatialReference lv95 = mockFeature.Shape.SpatialReference;

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2574923.000, 1196869.000, lv95),
				GeometryFactory.CreatePoint(2574912.000, 1196885.000, lv95));

			double originalArea = ((IArea3D) mockFeature.Shape).Area3D;

			var cutter = new FeatureCutter(new[] {mockFeature});
			cutter.ZSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					string.Empty, ChangeAlongZSource.SourcePlane);
			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double areaSum = 0;
			var totalParts = 0;
			foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				totalParts += ((IGeometryCollection) result).GeometryCount;

				areaSum += ((IArea3D) result).Area3D;
			}

			Assert.AreEqual(7, totalParts);

			Assert.AreEqual(originalArea, areaSum, 0.01);
		}

		[Test]
		public void CanCutMultipatchThroughVerticalWall_Top5022a()
		{
			// {4286CCE0-4539-4D91-B14B-1C3D79640021} from TLM:
			IFeature mockFeature =
				TestUtils.CreateMockFeature("TOP5022a_MultipatchToCut.xml");

			ISpatialReference lv95 = mockFeature.Shape.SpatialReference;

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2694016.709, 1263646, lv95),
				GeometryFactory.CreatePoint(2694025, 1263657, lv95));

			double originalArea = ((IArea3D) mockFeature.Shape).Area3D;

			var cutter = new FeatureCutter(new[] {mockFeature});
			cutter.ZSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					string.Empty, ChangeAlongZSource.SourcePlane);
			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double areaSum = 0;
			var totalParts = 0;
			foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				totalParts += ((IGeometryCollection) result).GeometryCount;

				areaSum += ((IArea3D) result).Area3D;
			}

			Assert.AreEqual(6, totalParts);

			Assert.IsTrue(MathUtils.AreEqual(originalArea, areaSum, 0.01));
		}

		[Test]
		public void CanCutMultipatchThroughVerticalWall_Top5022b()
		{
			// {5C8388FB-9A00-4AF4-BB0D-D120C8BDA88D} only the northern dormer window:
			IFeature mockFeature =
				TestUtils.CreateMockFeature("TOP5022b_MultipatchToCut.xml");

			ISpatialReference lv95 = mockFeature.Shape.SpatialReference;

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2694073.141, 1263073.24, 408.781000000003),
				GeometryFactory.CreatePoint(2694073.91, 1263036.271, 408.781000000003));

			cutLine.SpatialReference = lv95;

			double originalArea = ((IArea3D) mockFeature.Shape).Area3D;

			var cutter = new FeatureCutter(new[] {mockFeature});
			cutter.ZSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					string.Empty, ChangeAlongZSource.SourcePlane);

			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double areaSum = 0;
			var totalParts = 0;
			foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				totalParts += ((IGeometryCollection) result).GeometryCount;

				areaSum += ((IArea3D) result).Area3D;
			}

			Assert.AreEqual(7, totalParts);

			Assert.IsTrue(MathUtils.AreEqual(originalArea, areaSum, 0.01));
		}

		[Test]
		public void CanCutMultipatchWithVerticalWallsThroughVerticalWallsSnapped()
		{
			// {FE286920-3D4C-4CB3-AC22-51056B97A23F} from TLM:
			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithVerticalWalls.xml");

			ISpatialReference lv95 = mockFeature.Shape.SpatialReference;

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2574923.000, 1196869.000, lv95),
				GeometryFactory.CreatePoint(2574912.000, 1196885.000, lv95));

			IPolygon origFootprint = GeometryFactory.CreatePolygon(mockFeature.Shape);

			cutLine =
				IntersectionUtils.GetIntersectionLines(
					cutLine, origFootprint, true, true);

			double originalArea = ((IArea3D) mockFeature.Shape).Area3D;

			var cutter = new FeatureCutter(new[] {mockFeature});
			cutter.ZSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					string.Empty, ChangeAlongZSource.SourcePlane);

			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double areaSum = 0;
			var totalParts = 0;
			foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				totalParts += ((IGeometryCollection) result).GeometryCount;

				areaSum += ((IArea3D) result).Area3D;
			}

			Assert.AreEqual(7, totalParts);

			Assert.IsTrue(MathUtils.AreEqual(originalArea, areaSum, 0.01));
		}

		[Test]
		public void CanCutMultipatchWithVerticalWallsThroughVerticalWallsTwice()
		{
			// {FE286920-3D4C-4CB3-AC22-51056B97A23F} from TLM:
			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithVerticalWalls.xml");

			ISpatialReference lv95 = mockFeature.Shape.SpatialReference;

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2574923.000, 1196869.000, lv95),
				GeometryFactory.CreatePoint(2574916.000, 1196878.000, lv95),
				GeometryFactory.CreatePoint(2574915, 1196877, lv95),
				GeometryFactory.CreatePoint(2574920, 1196868, lv95));

			double originalArea = ((IArea3D) mockFeature.Shape).Area3D;

			var cutter = new FeatureCutter(new[] {mockFeature});
			cutter.ZSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					string.Empty, ChangeAlongZSource.SourcePlane);

			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double areaSum = 0;
			var totalParts = 0;
			foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				totalParts += ((IGeometryCollection) result).GeometryCount;

				areaSum += ((IArea3D) result).Area3D;
			}

			Assert.AreEqual(8, totalParts);

			// why is this so inaccurate???
			const double epsilon = 2.2;
			Assert.IsTrue(MathUtils.AreEqual(originalArea, areaSum, epsilon));
		}

		[Test]
		public void CannotCutMultipatchWithTriangleStrip()
		{
			// To support triangles, triangle fans, etc.:
			// Consider implementing a completely different approach:
			// Go through points, determine for each point if it is on the left or right side of the cut line (above or below for vertical surfaces)
			// If the side changes, use intersection point to finish the strip on the one side and to start the strip on the other.

			ISpatialReference lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95);

			object missing = Type.Missing;

			IMultiPatch multiPatch = new MultiPatchClass();

			IPointCollection newStrip = new TriangleStripClass();
			newStrip.AddPoint(GeometryFactory.CreatePoint(2600000, 1200000, 500),
			                  ref missing, ref missing);

			newStrip.AddPoint(GeometryFactory.CreatePoint(2600002, 1200000, 550),
			                  ref missing, ref missing);

			newStrip.AddPoint(GeometryFactory.CreatePoint(2600002, 1200002, 550),
			                  ref missing, ref missing);

			((IGeometryCollection) multiPatch).AddGeometry(
				(IGeometry) newStrip, ref missing,
				ref missing);

			multiPatch.SpatialReference = lv95;
			GeometryUtils.MakeZAware(multiPatch);

			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2600000, 1200001),
				GeometryFactory.CreatePoint(2600002, 1200001));
			cutLine.SpatialReference = lv95;

			IFeature mockFeature = TestUtils.CreateMockFeature(multiPatch);

			var cutter = new FeatureCutter(new[] {mockFeature});

			Exception assertionException = null;
			try
			{
				cutter.Cut(cutLine);
			}
			catch (Exception e)
			{
				assertionException = e;
			}

			Assert.NotNull(assertionException);

			// Once it is implemented:
			//IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			//Assert.AreEqual(2, results.Count);

			//double areaSum = 0;
			//var partCount = 0;
			//foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			//{
			//	Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

			//	areaSum +=
			//		GeometryUtils.GetParts((IGeometryCollection)result)
			//					 .Sum(part => ((IArea)part).Area);

			//	partCount += GeometryUtils.GetParts((IGeometryCollection)result).Count();
			//}

			//Assert.AreEqual(2, partCount);
		}

		[Test]
		public void CanCutMultipatchWithSelfIntersectingSketch()
		{
			// {FE286920-3D4C-4CB3-AC22-51056B97A23F} from TLM:
			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithVerticalWalls.xml");

			ISpatialReference lv95 = mockFeature.Shape.SpatialReference;

			// Use case: snap inside the multipatch in order to create a sketch segment along an existing multipatch segment to cut along,
			// then use F7 (segment deflection) with deflection 0 to prolong the sketch line to make sure it cuts across the entire multipatch.
			// NOTE: The non-simple intersect output can only be reproduced with these real-world coordinates / snapped to storage SR it would work ok
			//       Fix: simplify 
			IPolyline cutLine = GeometryFactory.CreateLine(
				GeometryFactory.CreatePoint(2574913.26117225, 1196875.8438835, lv95),
				GeometryFactory.CreatePoint(2574917.7921709, 1196878.12591931, lv95),
				GeometryFactory.CreatePoint(2574908.14863447, 1196873.26895571, lv95),
				GeometryFactory.CreatePoint(2574922.05492981, 1196880.27285628, lv95));

			double originalArea = ((IArea3D) mockFeature.Shape).Area3D;

			var cutter = new FeatureCutter(new[] {mockFeature});
			cutter.ZSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					string.Empty, ChangeAlongZSource.SourcePlane);

			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double areaSum = 0;
			var totalParts = 0;
			foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				totalParts += ((IGeometryCollection) result).GeometryCount;

				areaSum += ((IArea3D) result).Area3D;
			}

			Assert.AreEqual(6, totalParts);

			const double epsilon = 0.0001;
			Assert.IsTrue(MathUtils.AreEqual(originalArea, areaSum, epsilon));
		}

		[Test]
		public void CanCutMultipatchWithCutBackAtBottomRight()
		{
			// TOP-5226
			// This case has a cut-back (duplicate segment) at the bottom right due to TOP-5227
			// (Cracker creates cut-back for pointy angles and a vertex within the crack tolerance)
			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithCutBack.xml");

			IPolyline cutLine = (IPolyline) TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath("MultipatchWithCutBackCutLine.xml"));

			double originalArea = ((IArea3D) mockFeature.Shape).Area3D;

			var cutter = new FeatureCutter(new[] {mockFeature});
			cutter.ZSourceProvider =
				new DatasetSpecificSettingProvider<ChangeAlongZSource>(
					string.Empty, ChangeAlongZSource.SourcePlane);

			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			double areaSum = 0;
			var totalParts = 0;
			foreach (IGeometry result in cutter.ResultGeometriesByFeature[mockFeature])
			{
				Assert.IsFalse(GeometryUtils.HasUndefinedZValues(result));

				totalParts += ((IGeometryCollection) result).GeometryCount;

				areaSum += ((IArea3D) result).Area3D;
			}

			Assert.AreEqual(2, totalParts);

			// why is this so inaccurate???
			const double epsilon = 2.2;
			Assert.IsTrue(MathUtils.AreEqual(originalArea, areaSum, epsilon));
		}

		[Test]
		public void CanCutMultipatchResultingInDegenerateMultipatchFootprint_Top5258()
		{
			// Degenerate result multipatch:
			IFeature mockFeature =
				TestUtils.CreateMockFeature(
					"MultipatchCutResultWithDegenerateFootprint_Source.xml");

			IPolyline cutLine = (IPolyline) TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath(
					"MultipatchCutResultWithDegenerateFootprint_Target.xml"));

			var cutter = new FeatureCutter(new[] {mockFeature})
			             {
				             DegenerateMultipatchFootprintAction =
					             DegenerateMultipatchFootprintAction.Discard
			             };

			cutter.Cut(cutLine);

			IList<IGeometry> results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(1, results.Count);

			cutter = new FeatureCutter(new[] {mockFeature})
			         {
				         DegenerateMultipatchFootprintAction =
					         DegenerateMultipatchFootprintAction.Keep
			         };

			cutter.Cut(cutLine);

			results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			cutter = new FeatureCutter(new[] {mockFeature})
			         {
				         DegenerateMultipatchFootprintAction =
					         DegenerateMultipatchFootprintAction.Throw
			         };

			Assert.Throws<DegenerateResultGeometryException>(() => cutter.Cut(cutLine));

			//
			// Clear case of TOP-5258:
			mockFeature =
				TestUtils.CreateMockFeature(
					"MultipatchCutResultWithOkFootprint_Source.xml");

			cutLine = (IPolyline) TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath(
					"MultipatchCutResultWithOkFootprint_Target.xml"));

			cutter = new FeatureCutter(new[] {mockFeature})
			         {
				         DegenerateMultipatchFootprintAction =
					         DegenerateMultipatchFootprintAction.Throw
			         };

			cutter.Cut(cutLine);

			results = cutter.ResultGeometriesByFeature[mockFeature];
			Assert.AreEqual(2, results.Count);

			//
			// Unclear case of TOP-5258: (depends on tolerance, fuzzy!)
			// Probably the sqare footprint check should be done after assigning the storage SR

			mockFeature =
				TestUtils.CreateMockFeature(
					"MultipatchCutResultWithBorderlineFootprint_Source.xml");

			cutLine = (IPolyline) TestUtils.ReadGeometryFromXml(
				TestUtils.GetGeometryTestDataPath(
					"MultipatchCutResultWithBorderlineFootprint_Target.xml"));

			// Currently it throws, but in ArcMap the footprint is not square!?!
			cutter = new FeatureCutter(new[] {mockFeature})
			         {
				         DegenerateMultipatchFootprintAction =
					         DegenerateMultipatchFootprintAction.Throw
			         };

			Assert.Throws<DegenerateResultGeometryException>(() => cutter.Cut(cutLine));
		}
	}
}
