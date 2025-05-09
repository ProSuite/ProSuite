using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.Transformers;
using System.Collections.Generic;
using System;
using System.Linq;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrMakeTableTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(Commons.Test.TestCategory.FixMe)]
		[Ignore("requires version non-existing TG_SERVICE.RC_TLM_2022-6-30")]
		public void CanOpenAssociationTable()
		{
			IFeatureWorkspace workspace = OpenTestWorkspaceSde();

			const string baseTableName = "TOPGIS_TLM.TLM_STRASSE";
			const string associationTableName = "TOPGIS_TLM.TLM_STRASSENROUTE_STRASSE";

			IFeatureClass realFeatureClass =
				DatasetUtils.OpenFeatureClass(workspace, baseTableName);
			var baseTable = ReadOnlyTableFactory.Create(realFeatureClass);

			var tr = new TrMakeTable(baseTable, associationTableName);

			const string transformerName = "route_association";
			((ITableTransformer) tr).TransformerName = transformerName;
			IReadOnlyTable associationTable = tr.GetTransformed();

			Assert.AreEqual(associationTable.Name, transformerName);

			long rowCount = associationTable.RowCount(null);

			long checkCount =
				DatasetUtils.OpenTable(workspace, associationTableName).RowCount(null);

			Assert.AreEqual(checkCount, rowCount);
		}

		[Test]
		[Category(TestCategory.Sde)]
		[Category(Commons.Test.TestCategory.FixMe)]
		[Ignore("requires version non-existing TG_SERVICE.RC_TLM_2022-6-30")]
		public void CanOpenQueryClass()
		{
			IFeatureWorkspace workspace = OpenTestWorkspaceSde();

			const string baseTableName = "TOPGIS_TLM.TLM_FLIESSGEWAESSER";

			IFeatureClass realFeatureClass =
				DatasetUtils.OpenFeatureClass(workspace, baseTableName);
			var baseTable = ReadOnlyTableFactory.Create(realFeatureClass);

			var tr = new TrMakeTable(baseTable,
			                         "SELECT * FROM TOPGIS_TLM.GEWISS_REGION WHERE REGION LIKE 'V%'",
			                         null);

			const string transformerName = "kantone_mit_V";

			((ITableTransformer) tr).TransformerName = transformerName;
			IReadOnlyTable transformedTable = tr.GetTransformed();

			Assert.AreEqual(transformedTable.Name, transformerName);

			long rowCount = transformedTable.RowCount(null);

			// VD, VS
			Assert.AreEqual(2, rowCount);
		}

		[NotNull]
		[Category(TestCategory.Sde)]
		private static IFeatureWorkspace OpenTestWorkspaceSde()
		{
			string versionName = "TG_SERVICE.RC_TLM_2022-6-30";

			IFeatureWorkspace defaultVersion =
				(IFeatureWorkspace) TestUtils.OpenUserWorkspaceOracle();

			return WorkspaceUtils.OpenFeatureWorkspaceVersion(defaultVersion, versionName);
		}

		[Test]
		public void CanHandleOutOfTileRequests()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrMakeTable");

			IFeatureClass featureClass1 =
				CreateFeatureClass(
					ws, "polyFc1", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr") });

			IFeatureClass featureClass2 =
				CreateFeatureClass(
					ws, "polyFc2", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr") });

			ReadOnlyFeatureClass roPolyFc1 = ReadOnlyTableFactory.Create(featureClass1);
			ReadOnlyFeatureClass roPolyFc2 = ReadOnlyTableFactory.Create(featureClass2);

			double tileSize = 100;
			double x = 2600000;
			double y = 1200000;

			// Left of first tile, NOT within search distance
			IFeature leftOfFirst = CreateFeature(featureClass1, x - 20, y + 30, x - 15, y + 40);
			IFeature leftOfFirstIntersect =
				CreateFeature(featureClass2, x - 20, y + 30, x - 15, y + 40);

			// Inside first tile:
			IFeature insideFirst = CreateFeature(featureClass1, x, y, x + 10, y + 10);
			IFeature insideFirstIntersect = CreateFeature(featureClass2, x, y, x + 10, y + 10);

			// Right of first tile, NOT within search distance
			IFeature rightOfFirst =
				CreateFeature(featureClass1, x + tileSize + 15, y + 30, x + tileSize + 20, y + 40);
			IFeature rightOfFirstIntersect =
				CreateFeature(featureClass2, x + tileSize + 15, y + 30, x + tileSize + 20, y + 40);

			// Left of second tile, NOT within the search distance:
			IFeature leftOfSecond =
				CreateFeature(featureClass1, x + tileSize - 20, y, x + tileSize - 15, y + 10);
			IFeature leftOfSecondIntersect =
				CreateFeature(featureClass2, x + tileSize - 20, y, x + tileSize - 15, y + 10);

			TrMakeTable tr = new TrMakeTable(roPolyFc1, "polyFc2")
			                 {
				                 // NOTE: The search logic should work correctly even if search option is Tile! (e.g. due to downstream transformers)
				                 //NeighborSearchOption = TrSpatialJoin.SearchOption.All
			                 };

			var transformedClass = tr.GetTransformed();
			WriteFieldNames(transformedClass);

			var test =
				new ContainerOutOfTileDataAccessTest(transformedClass)
				{
					SearchDistanceIntoNeighbourTiles = 50
				};

			test.TileProcessed = (tile, outsideTileFeatures) =>
			{
				if (tile.CurrentEnvelope.XMin == x && tile.CurrentEnvelope.YMin == y)
				{
					// first tile: the leftOfFirst and rightOfFirst
					Assert.AreEqual(2, outsideTileFeatures.Count);

					Assert.True(outsideTileFeatures.All(
						            r => r.OID == leftOfFirst.OID ||
						                 r.OID == leftOfFirstIntersect.OID ||
						                 r.OID == rightOfFirst.OID ||
						                 r.OID == rightOfFirstIntersect.OID));
				}

				if (tile.CurrentEnvelope.XMin == x + tileSize && tile.CurrentEnvelope.YMin == y)
				{
					// second tile: leftOfSecond
					Assert.AreEqual(1, outsideTileFeatures.Count);

					Assert.True(outsideTileFeatures.All(
						            r => r.OID == leftOfSecond.OID ||
						                 r.OID == leftOfSecondIntersect.OID));
				}

				return 0;
			};

			test.SetSearchDistance(10);

			var container = new TestContainer { TileSize = tileSize };

			container.AddTest(test);

			ISpatialReference sr = DatasetUtils.GetSpatialReference(featureClass1);

			IEnvelope aoi = GeometryFactory.CreateEnvelope(
				2600000, 1200000.00, 2600000 + 2 * tileSize, 1200000.00 + tileSize, sr);

			// First, using FullGeometrySearch:
			test.UseFullGeometrySearch = true;
			container.Execute(aoi);

			// Now simulate full tile loading:
			test.UseFullGeometrySearch = false;
			test.UseTileEnvelope = true;
			container.Execute(aoi);
		}

		private static IFeature CreateFeature(IFeatureClass featureClass,
		                                      double xMin, double yMin,
		                                      double xMax, double yMax)
		{
			ISpatialReference sr = DatasetUtils.GetSpatialReference(featureClass);

			IFeature row = featureClass.CreateFeature();
			row.Shape = GeometryFactory.CreatePolygon(xMin, yMin, xMax, yMax, sr);
			row.Store();
			return row;
		}

		private static void WriteFieldNames(IReadOnlyTable targetTable)
		{
			for (int i = 0; i < targetTable.Fields.FieldCount; i++)
			{
				IField field = targetTable.Fields.Field[i];

				Console.WriteLine(field.Name);
			}
		}

		private IFeatureClass CreateFeatureClass(IFeatureWorkspace ws, string name,
		                                         esriGeometryType geometryType,
		                                         IList<IField> customFields = null)
		{
			List<IField> fields = new List<IField>();
			fields.Add(FieldUtils.CreateOIDField());
			if (customFields != null)
			{
				fields.AddRange(customFields);
			}

			fields.Add(FieldUtils.CreateShapeField(
				           "Shape", geometryType,
				           SpatialReferenceUtils.CreateSpatialReference
				           ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				            true), 1000));

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(ws, name,
				FieldUtils.CreateFields(fields));
			return fc;
		}
	}
}
