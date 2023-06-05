using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;

namespace ProSuite.Commons.AO.Test.Geodatabase.GdbSchema
{
	[TestFixture]
	public class GdbTableTest
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
		[Category(TestCategory.Sde)]
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

		[Test]
		[Category(TestCategory.Sde)]
		public void CanCreateGdbFeatureClassWrappingRealFeatureClass()
		{
			IWorkspace ws = TestUtils.OpenUserWorkspaceOracle();

			const string tlmStrasse = "TOPGIS_TLM.TLM_STRASSE";

			IFeatureClass realFeatureClass = DatasetUtils.OpenFeatureClass(ws, tlmStrasse);

			GdbTable gdbTable = new GdbFeatureClass(realFeatureClass);

			IFeatureClass gdbFeatureClass = (IFeatureClass) gdbTable;

			Assert.AreEqual(realFeatureClass.ObjectClassID, gdbFeatureClass.ObjectClassID);
			Assert.AreEqual(DatasetUtils.GetName(realFeatureClass),
			                DatasetUtils.GetName(gdbFeatureClass));
			Assert.AreEqual(DatasetUtils.GetAliasName(realFeatureClass),
			                DatasetUtils.GetAliasName(gdbFeatureClass));
			Assert.AreEqual(realFeatureClass.ShapeType,
			                gdbFeatureClass.ShapeType);

			Assert.IsTrue(gdbFeatureClass.HasOID);
			Assert.NotNull(gdbFeatureClass.OIDFieldName);

			Assert.AreEqual("OBJECTID", gdbFeatureClass.OIDFieldName);
			Assert.AreEqual("SHAPE", gdbFeatureClass.ShapeFieldName);

			// We're only using the schema, not the actual data!
			Assert.AreEqual(0,
			                GdbQueryUtils.Count(gdbFeatureClass));

			IQueryFilter queryFilter = GdbQueryUtils.CreateQueryFilter("OBJECTID");
			queryFilter.WhereClause = "OBJECTID < 0";

			Assert.AreEqual(0,
			                GdbQueryUtils.Count(gdbFeatureClass, queryFilter));

			// Now with querying the template class

			gdbFeatureClass = new GdbFeatureClass(realFeatureClass, true);

			Assert.AreEqual(realFeatureClass.FeatureCount(null),
			                GdbQueryUtils.Count(gdbFeatureClass));

			queryFilter.WhereClause = "OBJECTID > 12345";

			Assert.AreEqual(GdbQueryUtils.Count(realFeatureClass, queryFilter),
			                GdbQueryUtils.Count(gdbFeatureClass, queryFilter));

			IEnvelope gdbClassEnvelope = ((IGeoDataset) gdbFeatureClass).Extent;
			IEnvelope realClassEnvelope = ((IGeoDataset) realFeatureClass).Extent;
			Assert.IsTrue(GeometryUtils.AreEqual(realClassEnvelope, gdbClassEnvelope));
		}
	}
}
