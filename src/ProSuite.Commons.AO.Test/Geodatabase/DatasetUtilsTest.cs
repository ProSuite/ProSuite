#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Test.Geodatabase
{
	[TestFixture]
	public class DatasetUtilsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private string _simpleGdbPath;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.ConfigureUnittestLogging();

			_msg.IsVerboseDebugEnabled = true;

			_lic.Checkout();
			_simpleGdbPath = TestData.GetGdb1Path();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
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

			int countInBox = GdbQueryUtils.Count(fclass, filter.Geometry);
			int countAll = GdbQueryUtils.Count(fclass);

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
		public void CanOpenMosaicDataset()
		{
			IWorkspace workspace = TestUtils.OpenUserWorkspaceOracle();

			IMosaicDataset dataset = DatasetUtils.OpenMosaicDataset(workspace,
			                                                        "TOPGIS_TLM.TLM_DTM_MOSAIC");

			Assert.NotNull(dataset);
		}

		[Test]
		[Category(TestCategory.Fast)]
		public void TestSetValueNull()
		{
			const string featureClassName = "lines";
			const string fieldName = "OBJEKTART";

			IFeatureWorkspace workspace =
				WorkspaceUtils.OpenPgdbFeatureWorkspace(_simpleGdbPath);
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
			                  (readback1 == null)
				                  ? "null"
				                  : readback1.GetType().ToString());

			row.set_Value(fieldIndex, DBNull.Value);

			object readback2 = row.get_Value(fieldIndex);
			Console.WriteLine(@"get_Value(set_Value(DBNull.Value)) is {0}",
			                  (readback2 == null)
				                  ? "null"
				                  : readback2.GetType().ToString());
			bool isNull = readback2 == null;
			bool isDBNull = readback2 == DBNull.Value;

			Assert.IsTrue(isNull || isDBNull);
		}
	}
}
