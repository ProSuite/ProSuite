using System;
using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Diagnostics;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class GeometryFactoryTest
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
		public void CanCreateUnionWithPolygonsAndPoints()
		{
			var input = new List<IGeometry>
			            {
				            GeometryFactory.CreatePoint(100, 100),
				            GeometryFactory.CreatePoint(200, 200),
				            GeometryFactory.CreatePolygon(1000, 1000, 1100, 1100),
				            GeometryFactory.CreatePolygon(2000, 2000, 2100, 2100)
			            };

			const double expansionDistance = 10;
			IGeometry union = GeometryFactory.CreateUnion(input, expansionDistance);

			Console.WriteLine(GeometryUtils.ToString(union));

			Assert.AreEqual(4, GeometryUtils.GetPartCount(union));
		}

		[Test]
		public void CanCreateUnionWithPolygonsAndPolylines()
		{
			var input = new List<IGeometry>
			            {
				            GeometryFactory.CreatePolyline(500, 100, 600, 100),
				            GeometryFactory.CreatePolyline(500, 200, 600, 200),
				            GeometryFactory.CreatePolygon(1000, 1000, 1100, 1100),
				            GeometryFactory.CreatePolygon(2000, 2000, 2100, 2100)
			            };

			const double expansionDistance = 10;
			IGeometry union = GeometryFactory.CreateUnion(input, expansionDistance);

			Console.WriteLine(GeometryUtils.ToString(union));

			Assert.AreEqual(esriGeometryType.esriGeometryPolygon, union.GeometryType);
			Assert.AreEqual(4, GeometryUtils.GetPartCount(union));
		}

		[Test]
		public void CanCreateUnionWithPolylinesAndPoints()
		{
			var input = new List<IGeometry>
			            {
				            GeometryFactory.CreatePoint(100, 100),
				            GeometryFactory.CreatePoint(200, 200),
				            GeometryFactory.CreatePolyline(500, 100, 600, 100),
				            GeometryFactory.CreatePolyline(500, 200, 600, 200)
			            };

			const double expansionDistance = 10;
			IGeometry union = GeometryFactory.CreateUnion(input, expansionDistance);

			Console.WriteLine(GeometryUtils.ToString(union));

			Assert.AreEqual(4, GeometryUtils.GetPartCount(union));
			Assert.AreEqual(esriGeometryType.esriGeometryPolyline, union.GeometryType);
		}

		[Test]
		public void CanCreateUnionWithMultipointsAndPoints()
		{
			var input = new List<IGeometry>
			            {
				            GeometryFactory.CreatePoint(100, 100),
				            GeometryFactory.CreatePoint(200, 200),
				            GeometryFactory.CreateMultipoint(
					            GeometryFactory.CreatePoint(1000, 2000),
					            GeometryFactory.CreatePoint(2000, 3000)),
				            GeometryFactory.CreateMultipoint(
					            GeometryFactory.CreatePoint(3000, 4000),
					            GeometryFactory.CreatePoint(5000, 6000))
			            };

			const double expansionDistance = 10;
			IGeometry union = GeometryFactory.CreateUnion(input, expansionDistance);

			Console.WriteLine(GeometryUtils.ToString(union));

			Assert.AreEqual(esriGeometryType.esriGeometryMultipoint, union.GeometryType);
			Assert.AreEqual(6, GeometryUtils.GetPartCount(union));
		}

		[Test]
		public void CanCreateUnionWithPointsMultipointsPolylinesAndPolygons()
		{
			var input = new List<IGeometry>
			            {
				            GeometryFactory.CreatePoint(100, 100),
				            GeometryFactory.CreatePoint(200, 200),
				            GeometryFactory.CreatePolyline(500, 100, 600, 100),
				            GeometryFactory.CreatePolyline(500, 200, 600, 200),
				            GeometryFactory.CreateMultipoint(
					            GeometryFactory.CreatePoint(1000, 2000),
					            GeometryFactory.CreatePoint(2000, 3000)),
				            GeometryFactory.CreateMultipoint(
					            GeometryFactory.CreatePoint(3000, 4000),
					            GeometryFactory.CreatePoint(5000, 6000)),
				            GeometryFactory.CreatePolygon(10000, 10000, 11000, 11000),
				            GeometryFactory.CreatePolygon(20000, 20000, 21000, 21000)
			            };

			const double expansionDistance = 10;
			IGeometry union = GeometryFactory.CreateUnion(input, expansionDistance);

			Console.WriteLine(GeometryUtils.ToString(union));

			Assert.AreEqual(esriGeometryType.esriGeometryPolygon, union.GeometryType);
			Assert.AreEqual(10, GeometryUtils.GetPartCount(union));
		}

		[Test]
		public void CanCreateUnionWithIntersectingPointsMultipointsPolylinesAndPolygons()
		{
			var input = new List<IGeometry>
			            {
				            GeometryFactory.CreatePoint(100, 100),
				            GeometryFactory.CreatePoint(200, 200),
				            GeometryFactory.CreatePolyline(500, 100, 600, 100),
				            GeometryFactory.CreatePolyline(550, 100, 650, 100),
				            GeometryFactory.CreateMultipoint(
					            GeometryFactory.CreatePoint(1000, 2000),
					            GeometryFactory.CreatePoint(2000, 3000)),
				            GeometryFactory.CreateMultipoint(
					            GeometryFactory.CreatePoint(2000, 3000),
					            GeometryFactory.CreatePoint(3000, 4000)),
				            GeometryFactory.CreatePolygon(100, 100, 200, 200)
			            };

			const double expansionDistance = 10;
			IGeometry union = GeometryFactory.CreateUnion(input, expansionDistance);

			Console.WriteLine(GeometryUtils.ToString(union));

			Assert.AreEqual(esriGeometryType.esriGeometryPolygon, union.GeometryType);
			Assert.AreEqual(5, GeometryUtils.GetPartCount(union));
		}

		[Test]
		public void CanCreateUnionWithPointsAndPolylinesFastEnough()
		{
			var overall = new MemoryUsageInfo();

			for (int i = 0; i < 5; i++)
			{
				CanCreateUnionWithPointsAndPolylinesFastEnoughCore();
				GC.Collect();
			}

			overall.Refresh();
			Console.WriteLine(@"overall: {0}", overall);
		}

		[Test]
		public void CanCreateBagWithCopies()
		{
			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			IGeometry pt1 = GeometryFactory.CreatePoint(100, 200, 0);
			IGeometry pt2 = GeometryFactory.CreatePoint(100, 200, 0);
			pt1.SpatialReference = sref;

			IGeometryBag bag = GeometryFactory.CreateBag(pt1, pt2);

			var collection = (IGeometryCollection) bag;
			Assert.AreEqual(2, collection.GeometryCount);
			IGeometry bagPt1 = collection.get_Geometry(0);
			IGeometry bagPt2 = collection.get_Geometry(1);

			// expect copies in the bag
			Assert.AreNotEqual(pt1, bagPt1);
			Assert.AreNotEqual(pt2, bagPt2);

			Assert.IsTrue(
				SpatialReferenceUtils.AreEqual(sref, bag.SpatialReference, true, true));
			Assert.IsTrue(
				SpatialReferenceUtils.AreEqual(sref, bagPt1.SpatialReference, true,
				                               true));
			Assert.IsTrue(
				SpatialReferenceUtils.AreEqual(sref, bagPt2.SpatialReference, true,
				                               true));
		}

		[Test]
		public void CanCreateBuffer()
		{
			IPolygon original = GeometryFactory.CreatePolygon(100, 100, 150, 200);

			IPolygon buffer = GeometryFactory.CreateBuffer(original, 100);

			Assert.AreEqual(250, buffer.Envelope.Width, 0.01);
			Assert.AreEqual(300, buffer.Envelope.Height, 0.01);
		}

		[Test]
		public void CanCreateNonSimpleRing()
		{
			IPath path =
				GeometryFactory.CreatePath(GeometryFactory.CreatePoint(100, 100));

			IRing ring = GeometryFactory.CreateRing(path);

			Assert.AreEqual(1, ((IPointCollection) ring).PointCount);

			ring = GeometryFactory.CreateRing(new PathClass());

			Assert.IsTrue(ring.IsEmpty);
		}

		[Test]
		public void CantCreateBagWithOriginalsIfSpatialReferenceDifferent()
		{
			ISpatialReference sref =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);
			IGeometry pt1 = GeometryFactory.CreatePoint(100, 200, 0);
			IGeometry pt2 = GeometryFactory.CreatePoint(100, 200, 0);
			pt1.SpatialReference = sref;

			var list = new List<IGeometry> {pt1, pt2};

			Assert.Throws<ArgumentException>(
				() => GeometryFactory.CreateBag(list, CloneGeometry.Never));
		}

		[Test]
		public void LearningTestBufferConstructionHandlingEmptyInput()
		{
			var inputBag = new GeometryBagClass();

			IBufferConstruction bufferConstruction = GetBufferConstruction();

			IGeometryBag outputBag = new GeometryBagClass();

			bufferConstruction.ConstructBuffers(inputBag, 10,
			                                    (IGeometryCollection) outputBag);

			Assert.AreEqual(0, ((IGeometryCollection) outputBag).GeometryCount);
		}

		[Test]
		public void LearningTestBufferConstructionHandlingSingleEmptyGeometry()
		{
			var inputBag = new GeometryBagClass();
			inputBag.AddGeometry(new PolygonClass());

			IBufferConstruction bufferConstruction = GetBufferConstruction();

			IGeometryBag outputBag = new GeometryBagClass();

			bufferConstruction.ConstructBuffers(inputBag, 10,
			                                    (IGeometryCollection) outputBag);

			Assert.AreEqual(0, ((IGeometryCollection) outputBag).GeometryCount);
		}

		[Test]
		public void MeasureBufferConstructionConstructBuffersPerformance()
		{
			const int iterations = 100;
			IEnumerable<IGeometry> sources = GetBufferInput(iterations);

			IBufferConstruction bufferConstruction = GetBufferConstruction();

			IGeometryBag outputBag = new GeometryBagClass();

			var watch = new Stopwatch();
			watch.Start();

			bufferConstruction.ConstructBuffers(
				new GeometryEnumerator<IGeometry>(sources),
				10, (IGeometryCollection) outputBag);

			watch.Stop();

			Assert.AreEqual(iterations, ((IGeometryCollection) outputBag).GeometryCount);

			Console.Out.WriteLine(
				"IBufferConstruction.ConstructBuffers: {0:N2} ms per geometry",
				(double) watch.ElapsedMilliseconds / iterations);
		}

		[Test]
		public void MeasureBufferConstructionDefaultPerformance()
		{
			const int iterations = 100;
			IEnumerable<IGeometry> sources = GetBufferInput(iterations);

			IBufferConstruction bufferConstruction = new BufferConstructionClass();

			var watch = new Stopwatch();
			watch.Start();

			foreach (IGeometry source in sources)
			{
				bufferConstruction.Buffer(source, 10);
			}

			watch.Stop();

			Console.Out.WriteLine("IBufferConstruction.Buffer: {0:N2} ms per geoemtry",
			                      (double) watch.ElapsedMilliseconds / iterations);
		}

		[Test]
		public void MeasureBufferFactoryPerformance()
		{
			const int iterations = 100;
			IEnumerable<IGeometry> sources = GetBufferInput(iterations);

			var factory = new BufferFactory();

			var watch = new Stopwatch();
			watch.Start();

			var result = new List<IPolygon>(factory.Buffer(sources, 10));

			watch.Stop();

			Console.Out.WriteLine("BufferFactory.Buffer(): {0:N2} ms per geoemtry",
			                      (double) watch.ElapsedMilliseconds / iterations);
			Assert.AreEqual(iterations, result.Count, "Unexptected buffer output count");
		}

		[Test]
		public void MeasureBufferFactoryMappedPerformance()
		{
			const int iterations = 100;
			IEnumerable<IGeometry> sources = GetBufferInput(iterations);

			var input = new List<KeyValuePair<int, IGeometry>>();
			int i = 0;
			foreach (IGeometry geometry in sources)
			{
				input.Add(new KeyValuePair<int, IGeometry>(i, geometry));
				i++;
			}

			var factory = new BufferFactory();

			var watch = new Stopwatch();
			watch.Start();

			var result = new List<KeyValuePair<int, IPolygon>>(factory.Buffer(input, 10));

			watch.Stop();

			Console.Out.WriteLine(
				"BufferFactory.Buffer(IEnumerable<KeyValuePair>): {0:N2} ms per geoemtry",
				(double) watch.ElapsedMilliseconds / iterations);
			Assert.AreEqual(iterations, result.Count, "Unexptected buffer output count");
		}

		[Test]
		public void MeasureCreateBufferPerformance()
		{
			const int iterations = 100;
			IEnumerable<IGeometry> sources = GetBufferInput(iterations);

			var watch = new Stopwatch();
			watch.Start();

			foreach (IGeometry source in sources)
			{
				GeometryFactory.CreateBuffer(source, 10);
			}

			watch.Stop();

			Console.Out.WriteLine("CreateBuffer: {0:N2} ms per geometry",
			                      (double) watch.ElapsedMilliseconds / iterations);
		}

		[Test]
		public void MeasureTopologicalOperatorBufferPerformance()
		{
			const int iterations = 100;
			IEnumerable<IGeometry> sources = GetBufferInput(iterations);

			var watch = new Stopwatch();
			watch.Start();

			foreach (IGeometry source in sources)
			{
				((ITopologicalOperator) source).Buffer(10);
			}

			watch.Stop();

			Console.Out.WriteLine("ITopologicalOperator.Buffer: {0:N2} ms per geoemtry",
			                      (double) watch.ElapsedMilliseconds / iterations);
		}

		private static void CanCreateUnionWithPointsAndPolylinesFastEnoughCore()
		{
			var input = new List<IGeometry>();

			for (int i = 0; i < 100; i++)
			{
				input.Add(GeometryFactory.CreatePoint(100 * i, 100 * i));
			}

			double xmin = 0;
			for (int i = 0; i < 1000; i++)
			{
				xmin = xmin + 100;
				double xmax = xmin + 100;
				input.Add(GeometryFactory.CreatePolyline(xmin, 100, xmax, 100));
			}

			var watch = new Stopwatch();
			watch.Start();

			var memoryInfo = new MemoryUsageInfo();

			const double expansionDistance = 10;
			IGeometry union = GeometryFactory.CreateUnion(input, expansionDistance);

			GC.Collect();

			watch.Stop();
			memoryInfo.Refresh();
			Console.WriteLine(memoryInfo);
			Console.WriteLine(@"{0:N2} ms", watch.ElapsedMilliseconds);

			Assert.AreEqual(esriGeometryType.esriGeometryPolyline, union.GeometryType);
		}

		[NotNull]
		private static IBufferConstruction GetBufferConstruction()
		{
			IBufferConstruction construction = new BufferConstructionClass();

			var properties = (IBufferConstructionProperties) construction;

			properties.UnionOverlappingBuffers = false;
			properties.ExplodeBuffers = true;
			properties.GenerateCurves = true;
			properties.EndOption = esriBufferConstructionEndEnum.esriBufferRound;

			return construction;
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetBufferInput(int count)
		{
			ISpatialReference spatialReference = CreateDefaultSpatialReference();
			IPoint center = GeometryFactory.CreatePoint(0, 0, 0);
			center.SpatialReference = spatialReference;

			var result = new List<IGeometry>();

			for (int i = 0; i < count; i++)
			{
				IGeometry geometry =
					GeometryFactory.CreateCircularPolygon(center, 100, 1);
				geometry.SpatialReference = spatialReference;
				GeometryUtils.Simplify(geometry);
				result.Add(geometry);
			}

			return result;
		}

		[NotNull]
		private static ISpatialReference CreateDefaultSpatialReference()
		{
			const string xml =
				@"<ProjectedCoordinateSystem xsi:type='typens:ProjectedCoordinateSystem' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/9.3'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>800</XYScale><ZOrigin>-100000</ZOrigin><ZScale>800</ZScale><MOrigin>0</MOrigin><MScale>1</MScale><XYTolerance>0.0125</XYTolerance><ZTolerance>0.0125</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID></ProjectedCoordinateSystem>";
			return SpatialReferenceUtils.FromXmlString(xml);
		}
	}
}
