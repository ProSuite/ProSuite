using System;
using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.Test.TestSupport
{
	[TestFixture]
	public class GeographicCSGeometryTest
	{
		private ISpatialReference _wgs84;
		private ISpatialReference _lv95;

		[SetUp]
		public void SetUp()
		{
			_wgs84 = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRGeoCSType.esriSRGeoCS_WGS1984,
				true);
			_lv95 = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				true);
		}

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.AO.Test.TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			Commons.AO.Test.TestUtils.ReleaseLicense();
		}

		[Test]
		public void ProjectPerformanceTest()
		{
			IPolygon polygon = CreateTestPolygon();

			const int count = 100;
			IEnumerable<IPolygon> clones = GetClones(polygon, count);

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			foreach (IPolygon clone in clones)
			{
				clone.Project(_lv95);
			}

			stopwatch.Stop();

			Console.WriteLine(@"{0:N2} ms per polygon", stopwatch.ElapsedMilliseconds / count);
		}

		[Test]
		public void GetAreaByProjectionPerformanceTest()
		{
			IPolygon polygon = CreateTestPolygon();

			const int count = 100;
			IEnumerable<IPolygon> clones = GetClones(polygon, count);

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			double area = 0;

			foreach (IPolygon clone in clones)
			{
				clone.Project(_lv95);
				area = ((IArea) clone).Area;
			}

			stopwatch.Stop();

			Console.WriteLine(@"Area: {0}", area);
			Console.WriteLine(@"{0:N2} ms per polygon", stopwatch.ElapsedMilliseconds / count);
		}

		[Test]
		public void GetAreaByProjectionAndGeoTransformationPerformanceTest()
		{
			IPolygon polygon = CreateTestPolygon();

			const int count = 100;
			IEnumerable<IPolygon> clones = GetClones(polygon, count);

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			double area = 0;
			esriTransformDirection direction;
			IGeoTransformation transformation = GetGeoTransformationToLv95(out direction);

			foreach (IPolygon clone in clones)
			{
				((IGeometry5) clone).ProjectEx(_lv95, direction, transformation, false, 0, 0);
				area = ((IArea) clone).Area;
			}

			stopwatch.Stop();

			Console.WriteLine(@"Area: {0}", area);
			Console.WriteLine(@"{0:N2} ms per polygon", stopwatch.ElapsedMilliseconds / count);
		}

		[Test]
		[Ignore("does not run on 10.0 (IAreaGeodetic introduced in 10.1)")]
		public void GetAreaGeodeticPerformanceTest()
		{
			IPolygon polygon = CreateTestPolygon();

			const int count = 100;

			IEnumerable<IPolygon> clones0 = GetClones(polygon, count);
			IEnumerable<IPolygon> clones1 = GetClones(polygon, count);
			IEnumerable<IPolygon> clones2 = GetClones(polygon, count);
			IEnumerable<IPolygon> clones3 = GetClones(polygon, count);

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			double areaGeodesic = GetAreas(clones0,
			                               esriGeodeticType.esriGeodeticTypeGeodesic);

			stopwatch.Stop();

			Console.WriteLine();
			Console.WriteLine(@"esriGeodeticTypeGeodesic: Area = {0}", areaGeodesic);
			Console.WriteLine(@"{0:N2} ms per polygon", stopwatch.ElapsedMilliseconds / count);

			stopwatch.Reset();
			stopwatch.Start();

			double area = GetAreas(clones1,
			                       esriGeodeticType.esriGeodeticTypeGreatElliptic);

			stopwatch.Stop();

			Console.WriteLine();
			Console.WriteLine(@"esriGeodeticTypeGreatElliptic: Area = {0}", area);
			Console.WriteLine(@"{0:N2} ms per polygon", stopwatch.ElapsedMilliseconds / count);

			stopwatch.Reset();
			stopwatch.Start();

			double areaLoxodrome = GetAreas(clones2,
			                                esriGeodeticType.esriGeodeticTypeLoxodrome);

			stopwatch.Stop();

			Console.WriteLine();
			Console.WriteLine(@"esriGeodeticTypeLoxodrome: Area = {0}", areaLoxodrome);
			Console.WriteLine(@"{0:N2} ms per polygon", stopwatch.ElapsedMilliseconds / count);

			stopwatch.Reset();
			stopwatch.Start();

			double areaNormalSection = GetAreas(clones3,
			                                    esriGeodeticType.esriGeodeticTypeNormalSection);

			stopwatch.Stop();

			Console.WriteLine();
			Console.WriteLine(@"esriGeodeticTypeNormalSection: Area = {0}", areaNormalSection);
			Console.WriteLine(@"{0:N2} ms per polygon", stopwatch.ElapsedMilliseconds / count);
		}

		private double GetAreas(IEnumerable<IPolygon> polygons,
		                        esriGeodeticType geodeticType)
		{
			double area = 0;

			ILinearUnit linearUnit = ((IProjectedCoordinateSystem) _lv95).CoordinateUnit;

			foreach (IPolygon clone in polygons)
			{
				// NOTE uncomment to run on 10.1+
				//area = ((IAreaGeodetic) clone).AreaGeodetic[geodeticType, linearUnit];
			}

			return area;
		}

		[Test]
		public void ProjectExWithHintPerformanceTest()
		{
			IPolygon polygon = CreateTestPolygon();

			const int count = 100;
			IEnumerable<IPolygon> clones = GetClones(polygon, count);

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var direction = esriTransformDirection.esriTransformForward;
			ITransformation transformation = null;

			foreach (IPolygon clone in clones)
			{
				int hint = 0;
				((ISpatialReference3) _wgs84).ProjectionHint(clone, _lv95, ref direction,
				                                             ref transformation, ref hint);

				((IGeometry5) clone).ProjectEx5(_lv95, direction, null, false, 0, 0, hint);
			}

			stopwatch.Stop();

			// NOTE: projection WITHOUT the hint is FASTER overall.
			Console.WriteLine(@"{0:N2} ms per polygon", stopwatch.ElapsedMilliseconds / count);
		}

		[Test]
		public void ProjectExWithGeoTransformationPerformanceTest()
		{
			IPolygon polygon = CreateTestPolygon();

			const int count = 100;
			IEnumerable<IPolygon> clones = GetClones(polygon, count);

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			esriTransformDirection direction;
			IGeoTransformation transformation = GetGeoTransformationToLv95(out direction);

			foreach (IPolygon clone in clones)
			{
				((IGeometry5) clone).ProjectEx(_lv95, direction, transformation, false, 0, 0);
			}

			stopwatch.Stop();

			Console.WriteLine(@"{0:N2} ms per polygon", stopwatch.ElapsedMilliseconds / count);
		}

		[Test]
		public void ProjectExWithGeoTransformationAndHintPerformanceTest()
		{
			IPolygon polygon = CreateTestPolygon();

			const int count = 100;
			IEnumerable<IPolygon> clones = GetClones(polygon, count);

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			esriTransformDirection direction;
			ITransformation transformation = GetGeoTransformationToLv95(out direction);

			foreach (IPolygon clone in clones)
			{
				int hint = 0;
				((ISpatialReference3) _wgs84).ProjectionHint(clone, _lv95, ref direction,
				                                             ref transformation, ref hint);

				((IGeometry5) clone).ProjectEx5(_lv95, direction, transformation,
				                                false, 0, 0, hint);
			}

			stopwatch.Stop();

			// NOTE: projection WITHOUT the hint is FASTER overall.
			Console.WriteLine(@"{0:N2} ms per polygon", stopwatch.ElapsedMilliseconds / count);
		}

		[Test]
		public void ProjectExWithDensificationPerformanceTest()
		{
			IPolygon polygon = CreateTestPolygon();

			const int count = 100;
			IEnumerable<IPolygon> clones = GetClones(polygon, count);

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			const bool angularDensify = true;
			const double
				maxSegmentLength =
					0.000001; // seems to have to be in source coordinate system units (DD)!!
			const double
				maxDeviation =
					0.0000000000001; // seems to have no effect at all, no matter what values

			int? densifiedCount = null;
			foreach (IPolygon clone in clones)
			{
				((IGeometry5) clone).ProjectEx(_lv95, esriTransformDirection.esriTransformForward,
				                               null, angularDensify, maxSegmentLength,
				                               maxDeviation);
				if (densifiedCount == null)
				{
					densifiedCount = GeometryUtils.GetPointCount(clone);
				}
			}

			stopwatch.Stop();

			Console.WriteLine(@"Densified vertex count: {0:N0}", densifiedCount);

			Console.WriteLine(@"{0:N2} ms per polygon", stopwatch.ElapsedMilliseconds / count);
		}

		[NotNull]
		private IGeoTransformation GetGeoTransformationToLv95(
			out esriTransformDirection direction)
		{
			IList<KeyValuePair<IGeoTransformation, esriTransformDirection>> transformations =
				SpatialReferenceUtils.GetPredefinedGeoTransformations(
					_wgs84, _lv95);

			Assert.AreEqual(1, transformations.Count);

			IGeoTransformation transformation = transformations[0].Key;
			direction = transformations[0].Value;

			Console.WriteLine(@"{0} ({1})", transformation.Name, direction);

			return transformation;
		}

		[NotNull]
		private IPolygon CreateTestPolygon()
		{
			IPolygon result = GeometryFactory.CreatePolygon(7, 46, 7.001, 46.001, _wgs84);
			result.Densify(0.00001, 0);

			GeometryUtils.Simplify(result);

			Console.WriteLine(@"{0:N0} vertices", GeometryUtils.GetPointCount(result));
			// 401 points

			return result;
		}

		[NotNull]
		private static IEnumerable<IPolygon> GetClones([NotNull] IPolygon polygon, int count)
		{
			var result = new List<IPolygon>(count);

			for (int i = 0; i < count; i++)
			{
				result.Add(GeometryFactory.Clone(polygon));
			}

			return result;
		}
	}
}
