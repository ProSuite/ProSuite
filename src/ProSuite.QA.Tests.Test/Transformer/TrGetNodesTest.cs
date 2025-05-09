using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Container.TestContainer;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrGetNodesTest
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
		public void SimpleGetNodesTest()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fcLv95 = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));

			{
				IFeature f = fcLv95.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(100, 100).Curve;
				f.Store();
			}
			IReadOnlyFeatureClass ro = ReadOnlyTableFactory.Create(fcLv95);
			TrGetNodes tr = new TrGetNodes(ro);
			QaConstraint test =
				new QaConstraint(tr.GetTransformed(), "ObjectId = 2");

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanAggregateAttributes()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fcNet = DatasetUtils.CreateSimpleFeatureClass(ws, "netFc",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));

			{
				IFeature f = fcNet.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(100, 100).Curve;
				f.Store();
			}
			{
				IFeature f = fcNet.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(30, 10).LineTo(100, 100).Curve;
				f.Store();
			}
			{
				IFeature f = fcNet.CreateFeature();
				f.Shape = CurveConstruction.StartLine(90, 10).LineTo(100, 100).Curve;
				f.Store();
			}
			IReadOnlyFeatureClass roNet = ReadOnlyTableFactory.Create(fcNet);
			TrGetNodes tr = new TrGetNodes(roNet);
			tr.Attributes = new List<string>
			                {
				                "COUNT(OBJECTID) as joined_count"
			                };
			QaConstraint test =
				new QaConstraint(tr.GetTransformed(), "joined_count > 1");

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute();
			Assert.AreEqual(1, runner.Errors.Count);
		}

		[Test]
		public void CanHandleOutOfTileRequests()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrGetNodes");

			IFeatureClass featureClass =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr") });

			ReadOnlyFeatureClass roPolyFc = ReadOnlyTableFactory.Create(featureClass);

			double tileSize = 100;
			double x = 2600000;
			double y = 1200000;

			// Left of first tile, NOT within search distance
			IFeature leftOfFirst = CreateFeature(featureClass, x - 20, y + 30, x - 15, y + 40);
			IFeature leftOfFirstIntersect =
				CreateFeature(featureClass, x - 20, y + 30, x - 15, y + 40);

			// Inside first tile:
			IFeature insideFirst = CreateFeature(featureClass, x, y, x + 10, y + 10);
			IFeature insideFirstIntersect = CreateFeature(featureClass, x, y, x + 10, y + 10);

			// Right of first tile, NOT within search distance
			IFeature rightOfFirst =
				CreateFeature(featureClass, x + tileSize + 15, y + 30, x + tileSize + 20, y + 40);
			IFeature rightOfFirstIntersect =
				CreateFeature(featureClass, x + tileSize + 15, y + 30, x + tileSize + 20, y + 40);

			// Left of second tile, NOT within the search distance:
			IFeature leftOfSecond =
				CreateFeature(featureClass, x + tileSize - 20, y, x + tileSize - 15, y + 10);
			IFeature leftOfSecondIntersect =
				CreateFeature(featureClass, x + tileSize - 20, y, x + tileSize - 15, y + 10);

			TrGetNodes tr = new TrGetNodes(roPolyFc)
			                {
				                // NOTE: The search logic should work correctly even if search option is Tile! (e.g. due to downstream transformers)
				                //NeighborSearchOption = TrSpatialJoin.SearchOption.All
			                };

			TransformedFeatureClass transformedClass = tr.GetTransformed();
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
					Assert.AreEqual(4, outsideTileFeatures.Count);

					foreach (IReadOnlyRow outsideTileFeature in outsideTileFeatures)
					{
						Assert.True(InvolvedRowUtils.GetInvolvedRows(outsideTileFeature).All(
							            r => r.OID == leftOfFirst.OID ||
							                 r.OID == leftOfFirstIntersect.OID ||
							                 r.OID == rightOfFirst.OID ||
							                 r.OID == rightOfFirstIntersect.OID));
					}
				}

				if (tile.CurrentEnvelope.XMin == x + tileSize && tile.CurrentEnvelope.YMin == y)
				{
					// second tile: leftOfSecond
					Assert.AreEqual(2, outsideTileFeatures.Count);

					foreach (IReadOnlyRow outsideTileFeature in outsideTileFeatures)
					{
						Assert.True(InvolvedRowUtils.GetInvolvedRows(outsideTileFeature).All(
							            r => r.OID == leftOfSecond.OID ||
							                 r.OID == leftOfSecondIntersect.OID));
					}
				}

				return 0;
			};

			test.SetSearchDistance(10);

			var container = new TestContainer { TileSize = tileSize };

			container.AddTest(test);

			ISpatialReference sr = DatasetUtils.GetSpatialReference(featureClass);

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
			row.Shape = CurveConstruction.StartLine(xMin, yMin).LineTo(xMax, yMax).Curve;
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
