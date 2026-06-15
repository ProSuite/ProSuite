using System;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.CreateFootprint;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AO.Test.Geometry.CreateFootprint
{
	[TestFixture]
	public class CreateFootprintUtilsTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanCreateFootprintOnSlightOverlaps()
		{
			// TLM_GEBAEUDE {4575A818-C620-4ECF-BF93-1C8173E244A9}
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithSliverOverlapsTop5759.wkb", sr);

			IMultiPatch multiPatch = (IMultiPatch) mockFeature.Shape;

			IPolygon footprintGeom =
				CreateFootprintUtils.TryGetGeomFootprint(multiPatch, null, out _);

			Assert.IsNotNull(footprintGeom);

			Assert.AreEqual(65.630225, footprintGeom.Length, 0.01);
			Assert.AreEqual(238.567801, ((IArea) footprintGeom).Area, 0.01);

			IPolygon footprintAo =
				CreateFootprintUtils.GetFootprintAO(multiPatch);

			GeometryUtils.Simplify(footprintAo);

			// NOTE: In 10.8.1 the AO footprint is incorrect (entire ring is missing)
			//Assert.IsTrue(GeometryUtils.AreEqualInXY(footprintAo, footprintGeom));
		}

		[Test]
		public void CanCreateFootprintWithVerticalRings()
		{
			// TLM_GEBAEUDE {4529AD24-3E03-4B13-A02F-0A3FC00E2968}
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithVerticalRingsTop5759.wkb", sr);

			IMultiPatch multiPatch = (IMultiPatch) mockFeature.Shape;

			IPolygon footprintGeom =
				CreateFootprintUtils.TryGetGeomFootprint(multiPatch, null, out _);

			Assert.IsNotNull(footprintGeom);

			Assert.AreEqual(23.106911, footprintGeom.Length, 0.01);
			Assert.AreEqual(33.334958, ((IArea) footprintGeom).Area, 0.01);

			IPolygon footprintAo =
				CreateFootprintUtils.GetFootprintAO(multiPatch);

			Assert.IsTrue(GeometryUtils.AreEqualInXY(footprintAo, footprintGeom));
		}

		[Test]
		public void CanCreateFootprintRidgedRoofWithVerticals()
		{
			// TLM_GEBAEUDE {4D3D66EE-6A45-4E01-833C-02FBD7D3FC02}
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			// NOTE: With the standard Tolerance/Resolution (0.001/0.0001) this cannot be reproduced
			((ISpatialReferenceTolerance) sr).XYTolerance = 0.01;
			((ISpatialReferenceResolution) sr).XYResolution[true] = 0.001;

			IFeature mockFeature =
				TestUtils.CreateMockFeature(
					"MultipatchWithIntersectionAtShortishSegmentTop5759.wkb", sr);

			IMultiPatch multiPatch = (IMultiPatch) mockFeature.Shape;

			IPolygon footprintGeom =
				CreateFootprintUtils.TryGetGeomFootprint(multiPatch, null, out _);

			Assert.IsNotNull(footprintGeom);

			Assert.AreEqual(75.441285, footprintGeom.Length, 0.0001);
			Assert.AreEqual(349.015, ((IArea) footprintGeom).Area, 0.003);

			IPolygon footprintAo =
				CreateFootprintUtils.GetFootprintAO(multiPatch);

			GeometryUtils.Simplify(footprintAo);

			//Assert.IsTrue(GeometryUtils.AreEqualInXY(footprintAo, footprintGeom));
		}

		[Test]
		public void CanCreateFootprintAcuteTriangleIntersectsZigZagRing()
		{
			// TLM_GEBAEUDE {A144DA55-084F-4444-8DB6-08F995D6DBBB}

			// Originally this resulted in an outer ring remaining almost completely inside another outer ring
			// due to the clean up of a linear intersection within a linear intersection

			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			// NOTE: With the standard Tolerance/Resolution (0.001/0.0001) this cannot be reproduced
			((ISpatialReferenceTolerance) sr).XYTolerance = 0.01;
			((ISpatialReferenceResolution) sr).XYResolution[true] = 0.001;

			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithTriangleCuttingZigZag.wkb", sr);

			IMultiPatch multiPatch = (IMultiPatch) mockFeature.Shape;

			IPolygon footprintGeom =
				CreateFootprintUtils.TryGetGeomFootprint(multiPatch, null, out _);

			Assert.IsNotNull(footprintGeom);

			Assert.AreEqual(57.572264, footprintGeom.Length, 0.0001);
			Assert.AreEqual(199.323799, ((IArea) footprintGeom).Area, 0.01);

			IPolygon footprintAo =
				CreateFootprintUtils.GetFootprintAO(multiPatch);

			GeometryUtils.Simplify(footprintAo);

			//Assert.IsTrue(GeometryUtils.AreEqualInXY(footprintAo, footprintGeom));
		}

		[Test]
		public void CanCreateFootprintTriangleRingsWithMultiBoundaryLoop()
		{
			// TLM_GEBAEUDE {96912D18-62E7-4790-8625-B7D4B18C6B22}

			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			// NOTE: With the standard Tolerance/Resolution (0.001/0.0001) this cannot be reproduced
			((ISpatialReferenceTolerance) sr).XYTolerance = 0.01;
			((ISpatialReferenceResolution) sr).XYResolution[true] = 0.001;

			IFeature mockFeature =
				TestUtils.CreateMockFeature("MultipatchWithTriangleRings.wkb", sr);

			IMultiPatch multiPatch = (IMultiPatch) mockFeature.Shape;

			IPolygon footprintGeom =
				CreateFootprintUtils.TryGetGeomFootprint(multiPatch, null, out _);

			Assert.IsNotNull(footprintGeom);

			Assert.AreEqual(135.58423, footprintGeom.Length, 0.0001);
			Assert.AreEqual(740.23728, ((IArea) footprintGeom).Area, 0.01);

			IPolygon footprintAo =
				CreateFootprintUtils.GetFootprintAO(multiPatch);

			GeometryUtils.Simplify(footprintAo);

			Assert.IsTrue(GeometryUtils.AreEqualInXY(footprintAo, footprintGeom));
		}

		[Test]
		[Ignore("For analysis of the union operations to get the footprint.")]
		public void AnalyzeMultipatchGeometry()
		{

			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			string xmlOrWkbGeometryFilePath = @"C:\Temp\kirchweg_turgi.xml";

			IFeature mockFeature =
				TestUtils.CreateMockFeature(xmlOrWkbGeometryFilePath, sr);

			IMultiPatch multiPatch = (IMultiPatch) mockFeature.Shape;

			Pnt3D holePoint = new Pnt3D(2568351.55, 1112716.47, 0);

			Polyhedron polyhedron =
				GeometryConversionUtils.CreatePolyhedron(multiPatch, false, true);

			string wkbFileName = System.IO.Path.GetFileNameWithoutExtension(xmlOrWkbGeometryFilePath) + ".wkb";
			string directory = System.IO.Path.GetDirectoryName(xmlOrWkbGeometryFilePath);

			Assert.NotNull(directory);

			GeomUtils.ToWkbFile(polyhedron, System.IO.Path.Combine(directory, wkbFileName));

			double tolerance = 0.00625;
			MultiLinestring result = null;
			int count = 0;
			foreach (RingGroup ringGroup in polyhedron.RingGroups.OrderByDescending(
				         r => r.GetArea2D()))
			{
				if (result == null)
				{
					result = ringGroup.Clone();
				}
				else
				{
					count++;
					double area = result.GetArea2D();
					var watch = Stopwatch.StartNew();

					// Save source (pre-union result) and target ring at problematic steps for repro tests
					//if (count >= 30 && count <= 34)
					//{
					//	GeomUtils.ToWkbFile(result, $@"C:\temp\cp_source_{count}.wkb");
					//	GeomUtils.ToWkbFile(ringGroup, $@"C:\temp\cp_ring_{count}.wkb");
					//}

					result = GeomTopoOpUtils.GetUnionAreasXY(result, ringGroup, tolerance);
					watch.Stop();

					if (GeomRelationUtils.AreaContainsXY(result, holePoint, tolerance) == true)
					{
						Console.WriteLine($"Covered point at {count}");
					}

					if (GeomRelationUtils.AreaContainsXY(ringGroup, holePoint, tolerance) == true)
					{
						Console.WriteLine($"Ring at hole has been added at {count}");
					}

					Console.WriteLine($"{count}. Area {ringGroup.GetArea2D()}. Total: {result.GetArea2D()}");

					if (result.GetArea2D() < area)
					{
						Console.WriteLine($"Area decreased at {count}!!!");
					}

					IPolygon polygon =
						GeometryConversionUtils.CreatePolygon(multiPatch, result.GetLinestrings());

					GeomUtils.ToWkbFile(result, $@"C:\temp\result_{count}.wkb");

					if (! GeometryUtils.IsGeometrySimple(polygon, sr, true, out string reason))
					{
						Console.WriteLine($"Non-simple at {count}: {reason}");
					}

					if (count == 86) { }

					GeomUtils.ToWkbFile(result, $@"C:\temp\result_{count}.wkb");

					const long timeout300s = 300000;

					if (watch.ElapsedMilliseconds > timeout300s)
					{
						// Do not continue, most likely the next result will be even more time-consuming.
						throw new AssertionException("Unexpectedly long processing time");
					}
				}
			}

			IPolygon footprintGeom =
				CreateFootprintUtils.TryGetGeomFootprint(multiPatch, tolerance, out _);

			Assert.IsNotNull(footprintGeom);

			IPolygon footprintAo =
				CreateFootprintUtils.GetFootprintAO(multiPatch);

			GeometryUtils.Simplify(footprintAo);

			Console.WriteLine($"Geom footprint: {GeometryUtils.GetGeometrySize(footprintGeom)}");
			Console.WriteLine($"AO footprint: {GeometryUtils.GetGeometrySize(footprintAo)}");

			Assert.IsTrue(GeometryUtils.AreEqualInXY(footprintAo, footprintGeom));
		}
	}
}
