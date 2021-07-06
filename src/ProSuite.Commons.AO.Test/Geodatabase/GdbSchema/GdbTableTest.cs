using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;

namespace ProSuite.Commons.AO.Test.Geodatabase.GdbSchema
{
	[TestFixture]
	public class GdbTableTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanCreateGdbTable()
		{
			// An in-memory backing dataset is created automatically, if no factory method is provided
			GdbTable gdbTable = new GdbTable(41, "TESTABLE", "Test table");

			IObjectClass objectClass = gdbTable;

			Assert.AreEqual(41, objectClass.ObjectClassID);
			Assert.AreEqual("TESTABLE", DatasetUtils.GetName(objectClass));
			Assert.AreEqual("Test table", DatasetUtils.GetAliasName(objectClass));

			Assert.False(objectClass.HasOID);
			Assert.Null(objectClass.OIDFieldName);

			Assert.AreEqual(0, GdbQueryUtils.Count(objectClass, ""));

			// Add OID field
			gdbTable.AddField(FieldUtils.CreateOIDField());

			Assert.True(objectClass.HasOID);
			Assert.AreEqual("OBJECTID", objectClass.OIDFieldName);

			IQueryFilter queryFilter = GdbQueryUtils.CreateQueryFilter("OBJECTID");
			queryFilter.WhereClause = "OBJECTID < 0";

			Assert.AreEqual(0,
			                GdbQueryUtils.Count(objectClass, queryFilter));

			var backingDataset = gdbTable.BackingDataset as InMemoryDataset;

			Assert.NotNull(backingDataset);

			backingDataset.AllRows.Add(new GdbRow(1, gdbTable));

			Assert.AreEqual(0,
			                GdbQueryUtils.Count(objectClass, queryFilter));

			queryFilter.WhereClause = "OBJECTID > 0";

			Assert.AreEqual(1,
			                GdbQueryUtils.Count(objectClass, queryFilter));
		}

		[Test]
		public void CanCreateGdbFeatureClass()
		{
			// An in-memory backing dataset is created automatically, if no factory method is provided
			GdbFeatureClass gdbFeatureClass =
				new GdbFeatureClass(41, "TESTABLE", esriGeometryType.esriGeometryPoint,
				                    "Test table");

			IFeatureClass featureClass = gdbFeatureClass;

			Assert.AreEqual(41, featureClass.ObjectClassID);
			Assert.AreEqual("TESTABLE", DatasetUtils.GetName(featureClass));
			Assert.AreEqual("Test table",
			                DatasetUtils.GetAliasName(featureClass));
			Assert.AreEqual(esriGeometryType.esriGeometryPoint,
			                featureClass.ShapeType);

			Assert.False(featureClass.HasOID);
			Assert.Null(featureClass.OIDFieldName);

			Assert.AreEqual(0, GdbQueryUtils.Count(featureClass, ""));

			// Add OID field
			gdbFeatureClass.AddField(FieldUtils.CreateOIDField());

			// Add Shape field
			gdbFeatureClass.AddField(
				FieldUtils.CreateShapeField(
					esriGeometryType.esriGeometryPoint,
					SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95)));

			Assert.True(featureClass.HasOID);
			Assert.AreEqual("OBJECTID", featureClass.OIDFieldName);
			Assert.AreEqual("SHAPE", featureClass.ShapeFieldName);

			IQueryFilter queryFilter = GdbQueryUtils.CreateQueryFilter("OBJECTID");
			queryFilter.WhereClause = "OBJECTID < 0";

			Assert.AreEqual(0,
			                GdbQueryUtils.Count(featureClass, queryFilter));

			var backingDataset = gdbFeatureClass.BackingDataset as InMemoryDataset;

			Assert.NotNull(backingDataset);

			backingDataset.AllRows.Add(new GdbRow(1, gdbFeatureClass));

			Assert.AreEqual(0,
			                GdbQueryUtils.Count(featureClass, queryFilter));

			queryFilter.WhereClause = "OBJECTID > 0";

			Assert.AreEqual(1,
			                GdbQueryUtils.Count(featureClass, queryFilter));
		}

		[Test]
		public void CanCreateGdbFeatureClassWithBackingDataset()
		{
			IWorkspace ws = TestUtils.OpenUserWorkspaceOracle();

			const string tlmStrasse = "TOPGIS_TLM.TLM_STRASSE";

			IFeatureClass realFeatureClass = DatasetUtils.OpenFeatureClass(ws, tlmStrasse);

			var rowCache = GdbQueryUtils.GetRows((ITable) realFeatureClass, false)
			                            .Take(100)
			                            .ToList();

			Assert.AreEqual(100, rowCache.Count);

			GdbTable tableWithBackingData =
				new GdbFeatureClass(41, "TESTABLE", esriGeometryType.esriGeometryPoint,
				                    "Test table",
				                    table => new InMemoryDataset(table, rowCache));

			IFeatureClass featureClassWithBackingData = (IFeatureClass) tableWithBackingData;

			Assert.AreEqual(41, featureClassWithBackingData.ObjectClassID);
			Assert.AreEqual("TESTABLE", DatasetUtils.GetName(featureClassWithBackingData));
			Assert.AreEqual("Test table",
			                DatasetUtils.GetAliasName(featureClassWithBackingData));
			Assert.AreEqual(esriGeometryType.esriGeometryPoint,
			                featureClassWithBackingData.ShapeType);

			Assert.False(featureClassWithBackingData.HasOID);
			Assert.Null(featureClassWithBackingData.OIDFieldName);

			for (int i = 0; i < realFeatureClass.Fields.FieldCount; i++)
			{
				featureClassWithBackingData.AddField(realFeatureClass.Fields.Field[i]);
			}

			Assert.AreEqual("OBJECTID", featureClassWithBackingData.OIDFieldName);
			Assert.AreEqual("SHAPE", featureClassWithBackingData.ShapeFieldName);

			Assert.AreEqual(100, GdbQueryUtils.Count(featureClassWithBackingData, ""));

			IQueryFilter queryFilter = GdbQueryUtils.CreateQueryFilter("OBJECTID");
			queryFilter.WhereClause = "OBJECTID < 0";

			Assert.AreEqual(0,
			                GdbQueryUtils.Count(featureClassWithBackingData, queryFilter));

			var backingDataset = tableWithBackingData.BackingDataset as InMemoryDataset;

			Assert.NotNull(backingDataset);

			backingDataset.AllRows.Add(new GdbRow(1, tableWithBackingData));

			Assert.AreEqual(0,
			                GdbQueryUtils.Count(featureClassWithBackingData, queryFilter));

			queryFilter.WhereClause = "OBJECTID >= 0";

			Assert.AreEqual(101,
			                GdbQueryUtils.Count(featureClassWithBackingData, queryFilter));
		}
	}
}
