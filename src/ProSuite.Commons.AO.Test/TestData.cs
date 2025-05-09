using System.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Testing;

namespace ProSuite.Commons.AO.Test
{
	public static class TestData
	{
		private const string _filegdb93Name = "filegdb93.gdb";
		private const string _gdb1Name = "gdb1.gdb.zip";
		private const string _mdb1Name = "gdb1.mdb";
		private const string _accessDbMdbName = "access_db.mdb";
		private const string _mobileGdbName = "mobile.geodatabase";

		private const string _filegdb_tableJoinUtils = "TableJoinUtilsTest.gdb.zip";
		private const string _cartoMapImplTestMapName = "CartoMapImplTest.mxd";
		private const string _bigForestPolygonFileName = "BigForestPolygon.xml";

		private const string _intersectingLineNonZawareFileName =
			"IntersectingLineNonZaware.xml";

		private const string _selftouchingmultipartpolylineXmlFileName =
			"SelfTouchingMultipartPolyline.xml";

		private const string _polygonintersectingselftouchingmultipartpolylineXmlFileName
			=
			"PolygonIntersectingSelfTouchingMultipartPolyline.xml";

		private const string _hugeLockergesteinPolygonFileName =
			"HugeLockergesteinPolygon.xml";

		private const string _densifiedWorkUnitPerimeterFileName =
			"DensifiedWorkUnitPerimeter.xml";

		private const string _duplicatePointPolygonFileName =
			"PolygonWithDuplicateVertex.xml";

		private const string _unclosedPolygonFileName = "PolygonUnclosed.xml";

		private const string _selfIntersectingPolygonFileName =
			"PolygonWithSelfIntersection.xml";

		private const string _invertedRingPolygonFileName =
			"PolygonWithIsland_IncorrectRingsOrientation.xml";

		public static string GetFileGdb93Path()
		{
			var locator = new TestDataLocator();
			return locator.GetPath(_filegdb93Name);
		}

		public static string GetArealeFileGdbPath()
		{
			throw new DataException("Not available");
		}

		public static string GetGdb1Path()
		{
			return TestDataPreparer.ExtractZip(_gdb1Name).GetPath();
		}

		public static string GetMdb1Path()
		{
			return TestDataPreparer.FromDirectory().GetPath(_mdb1Name);
		}

		[NotNull]
		public static string GetNonGdbAccessDatabase()
		{
			var locator = new TestDataLocator();
			return locator.GetPath(_accessDbMdbName);
		}

		public static string GetMobileGdbPath()
		{
			return TestDataPreparer.FromDirectory().GetPath(_mobileGdbName);
		}
		
		public static string GetGdbTableJointUtilsPath()
		{
			return TestDataPreparer.ExtractZip(_filegdb_tableJoinUtils).GetPath();
		}

		public static string GetCartoMapImplTestMapPath()
		{
			var locator = new TestDataLocator();
			return locator.GetPath(_cartoMapImplTestMapName);
		}

		public static string GetBigForestPolygonPath()
		{
			return TestDataPreparer.FromDirectory(@"TestData\Geometry")
			                       .GetPath(_bigForestPolygonFileName);
		}

		public static string GetHugeLockergesteinPolygonPath()
		{
			return TestUtils.GetGeometryTestDataPath(_hugeLockergesteinPolygonFileName);
		}

		public static string GetIntersectingLineNonZawarePath()
		{
			return TestUtils.GetGeometryTestDataPath(_intersectingLineNonZawareFileName);
		}

		public static string GetSelfTouchingPolylinePath()
		{
			return TestUtils.GetGeometryTestDataPath(_selftouchingmultipartpolylineXmlFileName);
		}

		public static string GetPolygonIntersectingSelfTouchingPolylinePath()
		{
			return TestUtils.GetGeometryTestDataPath(
				_polygonintersectingselftouchingmultipartpolylineXmlFileName);
		}

		public static string GetDensifiedWorkUnitPerimeterPath()
		{
			return TestUtils.GetGeometryTestDataPath(_densifiedWorkUnitPerimeterFileName);
		}

		public static string GetSelfIntersectingPolygonPath()
		{
			return TestUtils.GetGeometryTestDataPath(_selfIntersectingPolygonFileName);
		}

		public static string GetUnclosedPolygonPath()
		{
			return TestUtils.GetGeometryTestDataPath(_unclosedPolygonFileName);
		}

		public static string GetDuplicateVertexPolygonPath()
		{
			return TestUtils.GetGeometryTestDataPath(_duplicatePointPolygonFileName);
		}

		public static string GetInvertedRingPolygonPath()
		{
			return TestUtils.GetGeometryTestDataPath(_invertedRingPolygonFileName);
		}

		public static string GetBasodinoBorderGrat()
		{
			return TestUtils.GetGeometryTestDataPath("Basodino.xml");
		}

		public static string GetTiffDtm()
		{
			var locator = new TestDataLocator();
			return locator.GetPath("test_dtm.tif");
		}
	}
}
