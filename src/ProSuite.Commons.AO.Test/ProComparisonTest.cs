using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;

namespace ProSuite.Commons.AO.Test
{
	[TestFixture]
	public class ProComparisonTest
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
		public void GeometryMemoryUsage()
		{
			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV03);
			IEnvelope envelope =
				GeometryFactory.CreateEnvelope(1000, 1000, 2000, 2000, sref);

			GC.Collect();
			GC.WaitForPendingFinalizers();

			var process = Process.GetCurrentProcess();
			var privateBytes = process.PrivateMemorySize64;

			var polygons = new List<IPolygon>();
			const int count = 100;
			for (var i = 0; i < count; i++)
			{
				IPolygon polygon = GeometryFactory.CreatePolygon(envelope);
				polygon.Densify(0.5, 1);

				//var segment = ((ISegmentCollection) polygon).Segment[100];
				var point = ((IPointCollection) polygon).Point[100];
				var parts = GeometryUtils.GetParts((IGeometryCollection) polygon).Count();
				var segments =
					GeometryUtils
						.GetSegments(((ISegmentCollection) polygon).EnumSegments, true)
						.Count();
				polygons.Add(polygon);
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();

			process.Refresh();
			var delta = process.PrivateMemorySize64 - privateBytes;

			Console.WriteLine($@"{delta / (double) count / 1024} KB per geometry");
		}

		[Test]
		public void MeasurePerformance_GetSubCurve()
		{
			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV03);
			IEnvelope envelope =
				GeometryFactory.CreateEnvelope(1000, 1000, 2000, 2000, sref);
			IPolygon polygon = GeometryFactory.CreatePolygon(envelope);
			polygon.Densify(0.5, 1);
			((ISpatialIndex) polygon).AllowIndexing = true; // ENABLE the index

			Console.WriteLine($@"point count: {GeometryUtils.GetPointCount(polygon)}");

			var watch = new Stopwatch();
			watch.Start();

			const int count = 1000;
			for (var i = 0; i < count; i++)
			{
				ICurve subcurve;
				((ICurve) polygon).GetSubcurve(0, 100, false, out subcurve);
				//var subcurve = GeometryEngine.GetSubCurve(polygon, 0, 100, GeometryEngine.AsRatioOrLength.AsLength);
				//Assert.True(((IRelationalOperator) polygon).Contains(point));

				double length = 0;
				foreach (IGeometry part in GeometryUtils.GetParts(
					         (IGeometryCollection) subcurve))
				{
					foreach (var segment in GeometryUtils.GetSegments(
						         ((ISegmentCollection) part).EnumSegments, true))
					{
						length += segment.Length;
					}
				}
			}

			watch.Stop();
			Console.WriteLine(
				$@"{watch.ElapsedMilliseconds / (double) count} ms per operation");

			// just GetSubcurve, per operation: AO: 0.088 ms
			// with segment access:             AO: 0.56  ms
		}

		[Test]
		public void MeasurePerformance_DisjointLargeAOI()
		{
			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(
					WellKnownHorizontalCS.LV03);
			IEnvelope envelope =
				GeometryFactory.CreateEnvelope(1000, 1000, 2000, 2000, sref);

			foreach (var length in new[] {0.2, 0.3, 0.4, 0.5, 0.75, 1, 1.25, 2, 4, 8, 16})
			{
				IPolygon polygon = GeometryFactory.CreatePolygon(envelope);
				polygon.Densify(length, 1);
				((ISpatialIndex) polygon).AllowIndexing = true; // ENABLE the index

				int pointCount = GeometryUtils.GetPointCount(polygon);

				IPoint point = GeometryFactory.CreatePoint(1500, 1500, sref);

				// try to make sure there's no GC run within the measurement
				GC.Collect();
				GC.WaitForPendingFinalizers();

				var watch = new Stopwatch();
				watch.Start();

				const int count = 1000;
				for (var i = 0; i < count; i++)
				{
					((IRelationalOperator) polygon).Disjoint(point);
				}

				watch.Stop();
				Console.WriteLine(
					$@"Vertex count {pointCount}: {watch.ElapsedMilliseconds / (double) count} ms per operation");
			}

			// 10.4.1:
			// -------
			// Vertex count 20005: 0.019 ms per operation
			// Vertex count 13337: 0.012 ms per operation
			// Vertex count 10005: 0.01 ms per operation
			// Vertex count 8005: 0.006 ms per operation
			// Vertex count 5337: 0.005 ms per operation
			// Vertex count 4005: 0.004 ms per operation
			// Vertex count 3205: 0.003 ms per operation
			// Vertex count 2005: 0.003 ms per operation
			// Vertex count 1005: 0.002 ms per operation
			// Vertex count 505: 0.002 ms per operation
			// Vertex count 253: 0.002 ms per operation
		}

		[Test]
		public void MeasurePerformance_DisjointLargeAOI_NoSpatialIndex()
		{
			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(
					WellKnownHorizontalCS.LV03);
			IEnvelope envelope =
				GeometryFactory.CreateEnvelope(1000, 1000, 2000, 2000, sref);

			foreach (var length in new[]
			                       {
				                       0.2, 0.3, 0.4, 0.5, 0.75, 1, 1.25, 2, 4, 8, 16
			                       })
			{
				IPolygon polygon = GeometryFactory.CreatePolygon(envelope);
				polygon.Densify(length, 1);
				((ISpatialIndex) polygon).AllowIndexing = false; // DISABLE the index

				int pointCount = GeometryUtils.GetPointCount(polygon);

				IPoint point = GeometryFactory.CreatePoint(1500, 1500, sref);

				// try to make sure there's no GC run within the measurement
				GC.Collect();
				GC.WaitForPendingFinalizers();

				var watch = new Stopwatch();
				watch.Start();

				const int count = 1000;
				for (var i = 0; i < count; i++)
				{
					((IRelationalOperator) polygon).Disjoint(point);
				}

				watch.Stop();
				Console.WriteLine($@"Vertex count {pointCount}: {
					watch.ElapsedMilliseconds / (double) count
				} ms per operation");
			}
		}

		[Test]
		public void MeasurePerformance_DisjointLargeAOI_InvalidateSpatialIndex()
		{
			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(
					WellKnownHorizontalCS.LV03);
			IEnvelope envelope =
				GeometryFactory.CreateEnvelope(1000, 1000, 2000, 2000, sref);

			foreach (var length in new[]
			                       {
				                       0.2, 0.3, 0.4, 0.5, 0.75, 1, 1.25, 2, 4, 8, 16
			                       })
			{
				IPolygon polygon = GeometryFactory.CreatePolygon(envelope);
				polygon.Densify(length, 1);
				((ISpatialIndex) polygon).AllowIndexing = true; // ENABLE the index

				int pointCount = GeometryUtils.GetPointCount(polygon);

				IPoint point = GeometryFactory.CreatePoint(1500, 1500, sref);

				// try to make sure there's no GC run within the measurement
				GC.Collect();
				GC.WaitForPendingFinalizers();

				var watch = new Stopwatch();
				watch.Start();

				const int count = 1000;
				for (var i = 0; i < count; i++)
				{
					// invalidate the index each time, to measure the cost of index creation
					((ISpatialIndex) polygon).Invalidate();

					((IRelationalOperator) polygon).Disjoint(point);
				}

				watch.Stop();
				Console.WriteLine($@"Vertex count {pointCount}: {
					watch.ElapsedMilliseconds / (double) count
				} ms per operation");
			}
		}
	}
}
