using System;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class SegmentReplacementUtilsTest
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
		public void LearningTest_ReplaceSegmentsWithNonIdenticalConnectPointsZs()
		{
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);

			IPolyline originalPolyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200000, 400, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600010, 1200000, 405, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600020, 1200000, 410, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600030, 1200000, 420, double.NaN, lv95)));

			IPath replacement = GeometryFactory.CreatePath(
				GeometryFactory.CreatePoint(2600010, 1200000, 0, double.NaN, lv95),
				GeometryFactory.CreatePoint(2600015, 1200010, 500, double.NaN, lv95),
				GeometryFactory.CreatePoint(2600020, 1200000, 900, double.NaN, lv95));

			// NOTE: The Z value of the first point of the replacement is used, however, the envelope of the resulting pathToReshape is not updated!
			//       Hence it is crucial to insert the exact replacement end points into the geometry to reshape!

			IPolyline polyline = GeometryFactory.Clone(originalPolyline);

			var pathToReshape =
				(ISegmentCollection) ((IGeometryCollection) polyline).Geometry[0];

			pathToReshape.ReplaceSegmentCollection(1, 2, (ISegmentCollection) replacement);

			// This makes no difference:
			//pathToReshape.SegmentsChanged();

			Assert.AreEqual(0, ((IPointCollection) pathToReshape).Point[1].Z);
			Assert.AreEqual(400, polyline.Envelope.ZMin,
			                "Envelope is not inconsistent any more after ArcObjects segment replacment!");

			// Make sure this does not happen in SegmentReplacementUtils
			polyline = GeometryFactory.Clone(originalPolyline);
			pathToReshape = (ISegmentCollection) ((IGeometryCollection) polyline).Geometry[0];
			SegmentReplacementUtils.ReplaceSegments((IPath) pathToReshape, replacement,
			                                        replacement.FromPoint, replacement.ToPoint);

			Assert.AreEqual(0, ((IPointCollection) pathToReshape).Point[1].Z);
			Assert.AreEqual(0, polyline.Envelope.ZMin,
			                "Envelope is inconsistent after SegmentReplacementUtils.ReplaceSegments!");
		}

		[Test]
		public void CanReplaceSegmentsWithNonZAwareReplacement()
		{
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95,
				                                             WellKnownVerticalCS.LHN95);

			IPolyline originalPolyline = GeometryFactory.CreatePolyline(
				GeometryFactory.CreatePath(
					GeometryFactory.CreatePoint(2600000, 1200000, 400, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600010, 1200000, 410, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600020, 1200000, 410, double.NaN, lv95),
					GeometryFactory.CreatePoint(2600030, 1200000, 420, double.NaN, lv95)));

			IPath replacement = GeometryFactory.CreatePath(
				GeometryFactory.CreatePoint(2600010, 1200000, 0, double.NaN, lv95),
				GeometryFactory.CreatePoint(2600015, 1200010, 500, double.NaN, lv95),
				GeometryFactory.CreatePoint(2600020, 1200000, 900, double.NaN, lv95));

			// NOTE: The Z value of the first point of the replacement is used, however, the envelope of the resulting pathToReshape is not updated!
			//       Hence it is crucial to insert the exact replacement end points into the geometry to reshape!

			IPolyline polyline = GeometryFactory.Clone(originalPolyline);

			var pathToReshape =
				(ISegmentCollection) ((IGeometryCollection) polyline).Geometry[0];

			GeometryUtils.MakeNonZAware(replacement);

			SegmentReplacementUtils.ReplaceSegments((IPath) pathToReshape, replacement,
			                                        replacement.FromPoint, replacement.ToPoint);

			Assert.AreEqual(410, ((IPointCollection) pathToReshape).Point[1].Z);
			Assert.AreEqual(400, polyline.Envelope.ZMin,
			                "Envelope is not inconsistent any more after segment replacment!");

			// Now connect within a segment:
			IPoint firstPoint = replacement.FromPoint;
			firstPoint.X = 2600007;
			((IPointCollection) replacement).UpdatePoint(0, firstPoint);

			polyline = GeometryFactory.Clone(originalPolyline);
			pathToReshape = (ISegmentCollection) ((IGeometryCollection) polyline).Geometry[0];

			GeometryUtils.MakeNonZAware(replacement);
			SegmentReplacementUtils.ReplaceSegments((IPath) pathToReshape, replacement,
			                                        replacement.FromPoint, replacement.ToPoint);

			Assert.AreEqual(407, ((IPointCollection) pathToReshape).Point[1].Z);
			Assert.AreEqual(400, polyline.Envelope.ZMin,
			                "Envelope is not inconsistent any more after segment replacment!");
		}

		[Test]
		public void CanGetSegmentIndexRing()
		{
			const double xyTolerance = 0.0125;
			IPolygon poly = GeometryFactory.CreatePolygon(1000, 2000, 1500, 2500);

			poly.SpatialReference = CreateSpatialReference(xyTolerance, 0.0125);

			var ring = (IRing) ((IGeometryCollection) poly).get_Geometry(0);

			IPoint searchPoint = new PointClass();

			searchPoint.PutCoords(234, 34675);
			int partIndex;

			int? resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				ring, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.IsNull(resultIndex, "Point is not on geometry but segment index returned.");

			searchPoint.PutCoords(1000, 2000);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				ring, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.IsNotNull(resultIndex, "Point is on geometry but segment index not found.");
			Assert.AreEqual(0, resultIndex);

			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				ring, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(3, resultIndex);

			searchPoint.PutCoords(1000, 2500);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				ring, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(1, resultIndex);

			searchPoint.PutCoords(1500, 2000);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				ring, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(3, resultIndex);
		}

		[Test]
		public void CanGetSegmentIndexMultipartPolygon()
		{
			const double xyTolerance = 0.0125;
			IPolygon poly = GeometryFactory.CreatePolygon(1000, 2000, 1500, 2500);
			IGeometry hole = GeometryFactory.CreatePolygon(1100, 2100, 1400, 2400);

			((IGeometryCollection) poly).AddGeometryCollection((IGeometryCollection) hole);

			poly.SpatialReference = CreateSpatialReference(xyTolerance, 0.0125);

			GeometryUtils.Simplify(poly);

			IPoint searchPoint = new PointClass();

			searchPoint.PutCoords(234, 34675);
			int partIndex;

			int? resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.IsNull(resultIndex, "Point is not on geometry but segment index returned.");

			// 1. point, segment from-point
			searchPoint.PutCoords(1000, 2000);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.IsNotNull(resultIndex, "Point is on geometry but segment index not found.");
			Assert.AreEqual(0, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 1. point, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(3, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 2. point, segment from-point
			searchPoint.PutCoords(1000, 2500);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(1, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 2. point, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(0, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 3. point, segment from-point
			searchPoint.PutCoords(1500, 2500);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(2, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 3. point, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(1, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 4. point, segment from-point
			searchPoint.PutCoords(1500, 2000);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(3, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 4. point, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(2, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 1. point of inner ring, segment from-point
			searchPoint.PutCoords(1100, 2100);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(0, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 1. point of inner ring, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(3, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 2. point of inner ring, segment from-point
			searchPoint.PutCoords(1400, 2100);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(1, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 2. point of inner ring, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(0, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 3. point of inner ring, segment from-point
			searchPoint.PutCoords(1400, 2400);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(2, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 3. point of inner ring, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(1, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 4. point of inner ring, segment from-point
			searchPoint.PutCoords(1100, 2400);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(3, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 4. point of inner ring, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				poly, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(2, resultIndex);
			Assert.AreEqual(1, partIndex);
		}

		[Test]
		public void CanGetSegmentIndexMultipartPolyline()
		{
			const double xyTolerance = 0.0125;

			IPolyline polyline = GeometryFactory.CreatePolyline(1000, 2000, 1500, 2500);

			ISpatialReference lv95 = CreateSpatialReference(0.0125, 0.0125);

			polyline.SpatialReference = lv95;

			object missing = Type.Missing;

			((IPointCollection) polyline).AddPoint(
				GeometryFactory.CreatePoint(2000, 3000), ref missing, ref missing);

			IGeometry secondLine = GeometryFactory.CreatePolyline(5000, 8000, 5500, 8500);

			((IPointCollection) secondLine).AddPoint(
				GeometryFactory.CreatePoint(6000, 9000), ref missing, ref missing);

			((IGeometryCollection) polyline).AddGeometryCollection(
				(IGeometryCollection) secondLine);

			GeometryUtils.Simplify(polyline);

			IPoint searchPoint = new PointClass();

			searchPoint.PutCoords(234, 34675);
			int partIndex;

			int? resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.IsNull(resultIndex,
			              "Point is not on geometry but non-null segment index returned.");

			// 1. point, segment from-point
			searchPoint.PutCoords(1000, 2000);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.IsNotNull(resultIndex, "Point is on geometry but segment index not found.");
			Assert.AreEqual(0, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 1. point, segment to-point (actually incorrect parameter combination)
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(0, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 2. point, segment from-point
			searchPoint.PutCoords(1500, 2500);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(1, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 2. point, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(0, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 3. point, segment from-point (actually incorrect parameter combination)
			searchPoint.PutCoords(2000, 3000);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(1, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 3. point, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(1, resultIndex);
			Assert.AreEqual(0, partIndex);

			// 1. point of second, segment from-point
			searchPoint.PutCoords(5000, 8000);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(0, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 1. point of inner ring, segment to-point (incorrect parameter combination)
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(0, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 2. point of inner ring, segment from-point
			searchPoint.PutCoords(5500, 8500);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(1, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 2. point of inner ring, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(0, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 3. point of inner ring, segment from-point (actually incorrect parameter combination)
			searchPoint.PutCoords(6000, 9000);
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, true, true);

			Assert.AreEqual(1, resultIndex);
			Assert.AreEqual(1, partIndex);

			// 3. point of inner ring, segment to-point
			resultIndex = SegmentReplacementUtils.GetSegmentIndex(
				polyline, searchPoint, xyTolerance, out partIndex, false, true);

			Assert.AreEqual(1, resultIndex);
			Assert.AreEqual(1, partIndex);
		}

		[Test]
		public void CanInsertVertexIntoPolyline()
		{
			IPolyline polyline = GeometryFactory.CreatePolyline(1000, 2000, 1500, 2500);

			ISpatialReference lv95 = CreateSpatialReference(0.0125, 0.0125);

			polyline.SpatialReference = lv95;

			IPoint newPoint = GeometryFactory.CreatePoint(1250, 2250, lv95);

			SegmentReplacementUtils.InsertVertex(newPoint, 0, 0, (ISegmentCollection) polyline);

			Assert.AreEqual(2, ((ISegmentCollection) polyline).SegmentCount);

			newPoint = GeometryFactory.CreatePoint(1400, 2400, lv95);

			SegmentReplacementUtils.InsertVertex(newPoint, 1, 0, (ISegmentCollection) polyline);

			Assert.AreEqual(3, ((ISegmentCollection) polyline).SegmentCount);
		}

		[Test]
		public void CanInsertVertexIntoPolygon()
		{
			ISpatialReference lv95 = CreateSpatialReference(0.0125, 0.0125);

			IPolygon polygon = GeometryFactory.CreatePolygon(1000, 2000, 1500, 2500, lv95);

			GeometryUtils.Simplify(polygon, false);

			Assert.AreEqual(4, ((ISegmentCollection) polygon).SegmentCount);

			IPoint newPoint = GeometryFactory.CreatePoint(1250, 2000, lv95);

			SegmentReplacementUtils.InsertVertex(newPoint, 3, 0, (ISegmentCollection) polygon);

			Assert.AreEqual(5, ((ISegmentCollection) polygon).SegmentCount);

			newPoint = GeometryFactory.CreatePoint(1000, 2250, lv95);

			SegmentReplacementUtils.InsertVertex(newPoint, 0, 0, (ISegmentCollection) polygon);

			Assert.AreEqual(6, ((ISegmentCollection) polygon).SegmentCount);
		}

		[Test]
		public void CanJoinConnectedLines()
		{
			var line1 =
				(IPolyline) TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("line1.xml"));
			var line2 =
				(IPolyline) TestUtils.ReadGeometryFromXml(
					TestUtils.GetGeometryTestDataPath("line2.xml"));

			AssertCanJoin(line1, line2);

			line1.ReverseOrientation();

			AssertCanJoin(line1, line2);

			line2.ReverseOrientation();

			AssertCanJoin(line1, line2);

			line1.ReverseOrientation();

			AssertCanJoin(line1, line2);
		}

		[Test]
		public void CanGetPathSegmentsBetweenPoints()
		{
			const string xmlPath =
				@"<Path xsi:type='typens:Path' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.1'><PointArray xsi:type='typens:ArrayOfPoint'><Point xsi:type='typens:PointN'><X>2744261.7537500001</X><Y>1201884.120000001</Y><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>140996569.55187955</XYScale><ZOrigin>-100000</ZOrigin><ZScale>800</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.0025000000000000001</XYTolerance><ZTolerance>0.012500000000000001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID><LatestWKID>2056</LatestWKID></SpatialReference></Point><Point xsi:type='typens:PointN'><X>2744255.8000000007</X><Y>1201848.4012500048</Y><SpatialReference xsi:type='typens:ProjectedCoordinateSystem'><WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0],AUTHORITY[&quot;EPSG&quot;,2056]]</WKT><XOrigin>-27386400</XOrigin><YOrigin>-32067900</YOrigin><XYScale>140996569.55187955</XYScale><ZOrigin>-100000</ZOrigin><ZScale>800</ZScale><MOrigin>-100000</MOrigin><MScale>10000</MScale><XYTolerance>0.0025000000000000001</XYTolerance><ZTolerance>0.012500000000000001</ZTolerance><MTolerance>0.001</MTolerance><HighPrecision>true</HighPrecision><WKID>2056</WKID><LatestWKID>2056</LatestWKID></SpatialReference></Point></PointArray></Path>";

			var path = (IPath) GeometryUtils.FromXmlString(xmlPath);

			IPoint startPoint = path.FromPoint;
			IPoint endPoint = new PointClass();
			path.QueryPoint(esriSegmentExtension.esriNoExtension, 2.1881204836952222, false,
			                endPoint);

			// Not Z aware:
			Assert.IsFalse(GeometryUtils.IsZAware(path));
			ICurve result = SegmentReplacementUtils.GetCurveBetween(startPoint, endPoint, path);

			Assert.IsFalse(GeometryUtils.IsZAware(result));

			// Z-aware but not simple:
			// The input must be Z-aware and Z-simple
			GeometryUtils.MakeZAware(path);
			Assert.IsTrue(((IZAware) path).ZAware);
			Assert.IsFalse(((IZAware) path).ZSimple);

			result = SegmentReplacementUtils.GetCurveBetween(startPoint, endPoint, path);

			// result is also Z-aware and not Z-simple:
			Assert.IsTrue(GeometryUtils.IsZAware(result));
			Assert.IsFalse(((IZAware) result).ZSimple);

			// Z aware and simple:
			GeometryUtils.SimplifyZ(path);
			Assert.IsTrue(((IZAware) path).ZAware);
			Assert.IsTrue(((IZAware) path).ZSimple);

			result = SegmentReplacementUtils.GetCurveBetween(startPoint, endPoint, path);

			// result is also Z-aware and Z-simple:
			Assert.IsTrue(GeometryUtils.IsZAware(result));
			Assert.IsTrue(((IZAware) result).ZSimple);
		}

		private static void AssertCanJoin(IPolyline line1, IPolyline line2)
		{
			var path1 = (IPath) ((IGeometryCollection) line1).get_Geometry(0);

			double totalLength = path1.Length + line2.Length;

			IPolyline resultPolyline = GeometryFactory.Clone(line2);
			var resultPath = (IPath) ((IGeometryCollection) resultPolyline).get_Geometry(0);

			SegmentReplacementUtils.JoinConnectedPaths(path1, resultPath);
			GeometryUtils.Simplify(resultPolyline, true, true);

			Assert.AreEqual(totalLength, resultPath.Length);
			Assert.AreEqual(1, ((IGeometryCollection) resultPolyline).GeometryCount);
		}

		private static ISpatialReference CreateSpatialReference(double xyTolerance,
		                                                        double zTolerance)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95, WellKnownVerticalCS.LHN95);

			// Need to initialize resolution in XY and Z:
			((ISpatialReferenceResolution) sr).ConstructFromHorizon();
			((ISpatialReferenceResolution) sr).SetDefaultZResolution();

			// Only now can set tolerance values:
			((ISpatialReferenceTolerance) sr).XYTolerance = xyTolerance;
			((ISpatialReferenceTolerance) sr).ZTolerance = zTolerance;

			return sr;
		}
	}
}
