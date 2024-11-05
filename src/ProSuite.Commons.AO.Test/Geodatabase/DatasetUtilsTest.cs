#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using OSGeo.GDAL;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class DatasetUtilsTest
	{
		private string _simpleGdbPath;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging();

			_msg.IsVerboseDebugEnabled = true;

			TestUtils.InitializeLicense();
			_simpleGdbPath = TestData.GetGdb1Path();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateQueryLayerFeatureClassForUnegisteredStGeometryTable()
		{
			IWorkspace workspace = TestUtils.OpenDDxWorkspaceOracle();

			const string periFclass = "UNITTEST_MANAGER.TOPDD_WFL_REVISIONPERI_POLY";
			const string rpView = "UNITTEST_MANAGER.V_TOPDD_WFL_REVISIONPERIMETER";
			string periFK = string.Format("{0}.PERIMETER_OID", rpView);
			string periPK = "OBJECTID";

			string tables = string.Format("{0}, {1}", periFclass, rpView);

			string joinExpression = string.Format("{0} = {1}", periFK, periPK);

			string sql = $"SELECT * FROM {tables} WHERE {joinExpression}";

			string queryClassName =
				DatasetUtils.GetQueryLayerClassName((IFeatureWorkspace) workspace,
				                                    "TEST_CLASS");

			// Unqualified Name must start with %, and it is owned by the current user:
			Assert.AreEqual("UNITTEST.%TEST_CLASS", queryClassName);

			double xyTolerance = 0.01;

			IFeatureClass fclass = (IFeatureClass)
				DatasetUtils.CreateQueryLayerClass((ISqlWorkspace) workspace, sql, queryClassName,
				                                   periPK, xyTolerance);

			var filter = new SpatialFilterClass
			             {
				             Geometry =
					             GeometryFactory.CreateEnvelope(2600000, 1200000, 2700000, 1300000),
				             SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects
			             };

			IFeatureCursor cursor = fclass.Search(filter, true);

			Assert.NotNull(cursor.NextFeature());
			Marshal.ReleaseComObject(cursor);

			long countInBox = GdbQueryUtils.Count(fclass, filter.Geometry);
			long countAll = GdbQueryUtils.Count(fclass);

			Assert.AreEqual(78, countInBox);
			Assert.AreEqual(260, countAll);

			Assert.AreEqual(countInBox, fclass.FeatureCount(filter));
			Assert.AreEqual(countAll, fclass.FeatureCount(null));

			// solution found elsewhere: only spatial queries work, need to pass spatial filter with geometry
			IFeatureCursor cursorWithNullFilter = fclass.Search(null, true);

			IFeature feature = cursorWithNullFilter.NextFeature();

			Assert.NotNull(feature);
			Assert.AreEqual(xyTolerance, GeometryUtils.GetXyTolerance(feature.Shape));

			Marshal.ReleaseComObject(cursorWithNullFilter);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(Commons.Test.TestCategory.NoContainer)]
		public void CanOpenMosaicDataset()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			IMosaicDataset dataset = MosaicUtils.OpenMosaicDataset(workspace,
				"TOPGIS_TLM.TLM_DTM_MOSAIC");

			Assert.NotNull(dataset);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(Commons.Test.TestCategory.NoContainer)]
		public void CanGetRasterFromMosaicDataset()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			IMosaicDataset dataset = MosaicUtils.OpenMosaicDataset(workspace,
				"TOPGIS_TLM.TLM_DTM_MOSAIC");

			IRaster raster = ((IMosaicDataset3) dataset).GetRaster(string.Empty);

			Assert.NotNull(dataset);

			IRasterCursor rasterCursor = raster.CreateCursor();

			IPixelBlock rasterCursorPixelBlock = rasterCursor.PixelBlock;

			Assert.NotNull(rasterCursorPixelBlock);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(Commons.Test.TestCategory.NoContainer)]
		public void CanGetRasterFileFromMosaicDatasetUsingSpatialQuery()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			IMosaicDataset mosaicDataset = MosaicUtils.OpenMosaicDataset(workspace,
				"TOPGIS_TLM.TLM_DTM_MOSAIC");

			IFeatureClass rasterCatalog = mosaicDataset.Catalog;

			IEnvelope winterthur = GeometryFactory.CreateEnvelope(
				2690000, 1254000, 2707500, 1266000,
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95));

			winterthur.Expand(-0.1, -0.1, false);

			IQueryFilter spatialFilter =
				GdbQueryUtils.CreateSpatialFilter(rasterCatalog, winterthur);

			Stopwatch watch = Stopwatch.StartNew();

			int count = 0;
			foreach (IFeature catalogFeature in GdbQueryUtils.GetFeatures(
				         rasterCatalog, spatialFilter, false))
			{
				// Method 1 (slow):
				var rasterCatalogItem = (IRasterCatalogItem) catalogFeature;

				IRasterDataset rasterDataset = rasterCatalogItem.RasterDataset;
				var itemPaths = (IItemPaths) rasterDataset;
				IStringArray stringArray = itemPaths.GetPaths();
				Marshal.ReleaseComObject(rasterDataset);

				Assert.AreEqual(1, stringArray.Count);

				string resultPathViaRasterDataset = stringArray.Element[0];

				// Method 2 (fast):
				var itemPathsQuery = (IItemPathsQuery) mosaicDataset;

				if (itemPathsQuery.QueryPathsParameters == null)
				{
					itemPathsQuery.QueryPathsParameters = new QueryPathsParametersClass();
				}

				stringArray = itemPathsQuery.GetItemPaths(catalogFeature);
				Assert.AreEqual(1, stringArray.Count);

				string resultPathViaItemPathsQuery = stringArray.Element[0];
				Assert.AreEqual(resultPathViaRasterDataset, resultPathViaItemPathsQuery);
				count++;
			}

			Console.WriteLine("Successfully extracted {0} raster paths in {1}s", count,
			                  watch.Elapsed.TotalSeconds);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Ignore("Needs GDAL")]
		public void CanOpenRasterFileFromMosaicDatasetUsingSpatialQueryGdal_Learning()
		{
			// TODO: Correct GDAL native reference. Work-around: copy the x64 directory to the output dir
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			IMosaicDataset mosaicDataset = MosaicUtils.OpenMosaicDataset(workspace,
				"TOPGIS_TLM.TLM_DTM_MOSAIC");

			IFeatureClass rasterCatalog = mosaicDataset.Catalog;

			IPoint winterthurLL = GeometryFactory.CreatePoint(
				2690021, 1254011,
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95));

			IQueryFilter spatialFilter =
				GdbQueryUtils.CreateSpatialFilter(rasterCatalog, winterthurLL);

			// In case of DllNotFoundException, copy the appropriate dlls from the gdal subdirectory to the bin
			Gdal.AllRegister();

			List<IFeature> features = GdbQueryUtils.GetFeatures(
				rasterCatalog, spatialFilter, false).ToList();

			Assert.True(features.Count > 0);

			string rasterPath = GetPathViaCatalogItemDataset(features[0]);

			Console.WriteLine("Opening raster dataset {0}...", rasterPath);

			Dataset ds = Gdal.Open(rasterPath, Access.GA_ReadOnly);

			if (ds == null)
			{
				Console.WriteLine("Can't open " + rasterPath);
				return;
			}

			Console.WriteLine("Raster dataset parameters:");
			Console.WriteLine("  Projection: " + ds.GetProjectionRef());
			Console.WriteLine("  RasterCount: " + ds.RasterCount);
			Console.WriteLine("  RasterSize (" + ds.RasterXSize + "," + ds.RasterYSize + ")");

			double[] adfGeoTransform = new double[6];
			ds.GetGeoTransform(adfGeoTransform);

			double originX = adfGeoTransform[0];
			double originY = adfGeoTransform[3];

			Console.WriteLine($"Origin: {originX} | {originY}");

			double pixelSizeX = adfGeoTransform[1];
			double pixelSizeY = adfGeoTransform[5];

			Console.WriteLine($"Pixel Size: {pixelSizeX} | {pixelSizeY}");

			/* -------------------------------------------------------------------- */
			/*      Get driver                                                      */
			/* -------------------------------------------------------------------- */
			Driver drv = ds.GetDriver();

			if (drv == null)
			{
				Console.WriteLine("Can't get driver.");
				Environment.Exit(-1);
			}

			Console.WriteLine("Using driver " + drv.LongName);

			/* -------------------------------------------------------------------- */
			/*      Get raster band                                                 */
			/* -------------------------------------------------------------------- */
			for (int iBand = 1; iBand <= ds.RasterCount; iBand++)
			{
				Band band = ds.GetRasterBand(iBand);
				Console.WriteLine("Band " + iBand + " :");
				Console.WriteLine("   DataType: " + band.DataType);
				Console.WriteLine("   Size (" + band.XSize + "," + band.YSize + ")");
				Console.WriteLine("   PaletteInterp: " +
				                  band.GetRasterColorInterpretation().ToString());

				double noDataValue;
				int hasNoDataValue;
				band.GetNoDataValue(out noDataValue, out hasNoDataValue);

				Console.WriteLine("   Has NoData value: " + hasNoDataValue);
				Console.WriteLine("   NoData value: " + noDataValue);

				band.GetBlockSize(out int blockSizeX, out int blockSizeY);

				Console.WriteLine("   Block Size (" + blockSizeX + "," + blockSizeY + ")");

				for (int iOver = 0; iOver < band.GetOverviewCount(); iOver++)
				{
					Band over = band.GetOverview(iOver);
					Console.WriteLine("      OverView " + iOver + " :");
					Console.WriteLine("         DataType: " + over.DataType);
					Console.WriteLine("         Size (" + over.XSize + "," + over.YSize + ")");
					Console.WriteLine("         PaletteInterp: " +
					                  over.GetRasterColorInterpretation());
				}

				// Get the value at winterthurLL:
				int pxlOffsetX = (int) Math.Floor((winterthurLL.X - originX) / pixelSizeX);
				int pxlOffsetY = (int) Math.Floor((winterthurLL.Y - originY) / pixelSizeY);

				// NOTE: ReadBlock is not available (and generally discouraged anyway)
				float[] buffer = new float[1];
				band.ReadRaster(pxlOffsetX, pxlOffsetY, 1, 1, buffer, 1, 1, 0, 0);

				double z = buffer[0];

				// TODO: Theoretically there is an underlying block cache which would make subsequent
				//       reads in a similar area very fast (TEST!) - probably the same as IRaster behaviour.
				// TODO: Bilinear interpolation if it's not the middle of a pixel!
				Console.WriteLine("Z value at {0}, {1}: {2}", winterthurLL.X, winterthurLL.Y, z);

				// Test for pixel acess - the boundary behaviour is different (bounds are checked!)
				// But the performance is 3 times as fast! Memory is almost stable after disposal.
				Random random = new Random();

				double minimumX = originX + 1;
				double width = 17500 / 4d - 2;
				double minimumY = originY + 1;
				double height = 12000 / 4d - 2;

				Stopwatch pixelWatch = Stopwatch.StartNew();

				int count = 10000;

				Console.WriteLine("Memory before: {0}", GetMemoryConsumptionText(out _));

				float[] pixelBuffer4 = new float[4];

				for (int i = 0; i < count; i++)
				{
					double x = minimumX + random.NextDouble() * width;
					double y = minimumY + random.NextDouble() * height;

					pxlOffsetX = (int) Math.Floor((x - originX) / pixelSizeX);
					pxlOffsetY = (int) Math.Floor((originY - y) / pixelSizeY);

					band.ReadRaster(pxlOffsetX, pxlOffsetY, 2, 2, pixelBuffer4, 2, 2, 0, 0);

					float v00 = pixelBuffer4[0];
					float v10 = pixelBuffer4[1];
					float v01 = pixelBuffer4[2];
					float v11 = pixelBuffer4[3];
				}

				Console.WriteLine($"Time: {pixelWatch.ElapsedMilliseconds} ms");

				Console.WriteLine("Memory after: {0}", GetMemoryConsumptionText(out _));

				// END Pixel access

				band.Dispose();
			}

			ds.Dispose();

			Console.WriteLine("Memory after disposal: {0}", GetMemoryConsumptionText(out _));
		}

		[Test]
		[Category(TestCategory.Fast)]
		public void TestSetValueNull()
		{
			const string featureClassName = "lines";
			const string fieldName = "OBJEKTART";

			IFeatureWorkspace workspace =
				WorkspaceUtils.OpenFileGdbFeatureWorkspace(_simpleGdbPath);
			IFeatureClass featureClass = DatasetUtils.OpenFeatureClass(workspace,
				featureClassName);

			IList<IFeature> rows = GdbQueryUtils.FindList(featureClass, "OBJECTID = 1");
			Assert.IsTrue(rows.Count == 1,
			              "Expected object not in test data");
			IRow row = rows[0];

			Console.WriteLine(@"Testing with field: {0}", fieldName);
			int fieldIndex = row.Fields.FindField(fieldName);
			Assert.AreNotSame(-1, fieldIndex,
			                  "No such field; check test data");

			row.set_Value(fieldIndex, null);
			object readback1 = row.get_Value(fieldIndex);
			Console.WriteLine(@"get_Value(set_Value(null)) is {0}",
			                  readback1?.GetType().ToString() ?? "null");

			row.set_Value(fieldIndex, DBNull.Value);

			object readback2 = row.get_Value(fieldIndex);
			Console.WriteLine(@"get_Value(set_Value(DBNull.Value)) is {0}",
			                  readback2?.GetType().ToString() ?? "null");
			bool isNull = readback2 == null;
			bool isDBNull = readback2 == DBNull.Value;

			Assert.IsTrue(isNull || isDBNull);
		}

		[Test]
		public void CanQualifyFieldNameFileGdb()
		{
			const string featureClassName = "lines";
			const string fieldName = "OBJEKTART";

			IFeatureWorkspace workspace =
				WorkspaceUtils.OpenFileGdbFeatureWorkspace(_simpleGdbPath);

			IFeatureClass featureClass = DatasetUtils.OpenFeatureClass(workspace,
				featureClassName);

			string qualified = DatasetUtils.QualifyFieldName(featureClass, fieldName);

			Assert.AreEqual($"{featureClassName}.{fieldName}", qualified);
		}

		[Test]
		public void CanQualifyFieldNameMobileGdb()
		{
			const string featureClassName = "main.lines";
			const string fieldName = "OBJEKTART";

			IFeatureWorkspace workspace =
				WorkspaceUtils.OpenMobileGdbFeatureWorkspace(TestData.GetMobileGdbPath());

			IFeatureClass featureClass = DatasetUtils.OpenFeatureClass(workspace,
				featureClassName);
			
			string qualified = DatasetUtils.QualifyFieldName(featureClass, fieldName);

			Assert.AreEqual($"{featureClassName}.{fieldName}", qualified);
		}

		[Test]
		public void CanQualifyFieldNameOracle()
		{
			const string featureClassName = "TOPGIS_TLM.TLM_STRASSE";
			const string tableName = "TOPGIS_TLM.TLM_VELOROUTE";
			const string fieldName = "OBJECTID";

			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			IFeatureClass featureClass = DatasetUtils.OpenFeatureClass(workspace, featureClassName);

			string qualified = DatasetUtils.QualifyFieldName(featureClass, fieldName);

			Assert.AreEqual($"{featureClassName}.{fieldName}", qualified);

			// Now as read-only feature class
			qualified =
				DatasetUtils.QualifyFieldName(ReadOnlyTableFactory.Create(featureClass), fieldName);
			Assert.AreEqual($"{featureClassName}.{fieldName}", qualified);

			ITable table = DatasetUtils.OpenTable(workspace, tableName);
			qualified = DatasetUtils.QualifyFieldName(table, fieldName);
			Assert.AreEqual($"{tableName}.{fieldName}", qualified);

			// Now as read-only table
			ReadOnlyTable veloRouteRO = ReadOnlyTableFactory.Create(table);
			qualified = DatasetUtils.QualifyFieldName(veloRouteRO, fieldName);
			Assert.AreEqual($"{tableName}.{fieldName}", qualified);

			IVersionInfo anyVersionInfo =
				WorkspaceUtils.GetVersionInfos(workspace, null).FirstOrDefault();
			Assert.IsNotNull(anyVersionInfo);

			IVersion version = WorkspaceUtils.OpenVersion(workspace, anyVersionInfo.VersionName);

			table = DatasetUtils.OpenTable((IFeatureWorkspace) version, tableName);
			qualified = DatasetUtils.QualifyFieldName(table, fieldName);
			Assert.AreEqual($"{tableName}.{fieldName}", qualified);

			veloRouteRO = ReadOnlyTableFactory.Create(table);
			qualified = DatasetUtils.QualifyFieldName(veloRouteRO, fieldName);
			Assert.AreEqual($"{tableName}.{fieldName}", qualified);
		}

		private static string GetMemoryConsumptionText(out long privateBytesMB)
		{
			long virtualBytes;
			long privateBytes;
			long workingSet;
			ProcessUtils.GetMemorySize(out virtualBytes, out privateBytes, out workingSet);

			const int mb = 1024 * 1024;

			privateBytesMB = privateBytes / mb;

			return string.Format(
				"VB:{0:N0} PB:{1:N0} WS:{2:N0}",
				virtualBytes / mb,
				privateBytesMB,
				workingSet / mb);
		}

		private static string GetPathViaCatalogItemDataset(IFeature catalogFeature)
		{
			var rasterCatalogItem = (IRasterCatalogItem) catalogFeature;

			IRasterDataset rasterDataset = rasterCatalogItem.RasterDataset;

			var itemPaths = (IItemPaths) rasterDataset;

			IStringArray stringArray = itemPaths.GetPaths();

			return stringArray.Element[0];
		}
	}
}
