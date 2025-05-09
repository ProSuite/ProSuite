using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO;
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
	public class TrProjectTest
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
		public void VerifyProjectedSelection()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			ISpatialReference srWgs84 = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRGeoCSType.esriSRGeoCS_WGS1984, true);

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");

			IFeatureClass fcLv95 = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));

			IFeatureClass fcWgs84 = DatasetUtils.CreateSimpleFeatureClass(ws, "wgs84",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPoint, srWgs84, 1000)));

			double x0 = 2600000;
			double x1 = 2601000;
			double y0 = 1200000;
			double y1 = 1201000;

			{
				IEnvelope l = CurveConstruction.StartLine(x0, y1)
				                               .LineTo(x1, y0).Curve.Envelope;
				l.SpatialReference = srLv95;
				l.Project(srWgs84);
			}

			double dx = 10;
			int over = 10;
			{
				for (double y = y0 - over * dx; y <= y1 + over * dx; y += dx)
				{
					for (double x = x0 - over * dx; x <= x1 + over * dx; x = x + dx)
					{
						IPoint p = new PointClass { X = x, Y = y };
						p.SpatialReference = srLv95;
						p.Project(srWgs84);

						IFeature f = fcWgs84.CreateFeature();
						f.Shape = p;
						f.Store();
					}
				}
			}

			int nAutoPrj = 0;
			{
				IEnvelope l = CurveConstruction.StartLine(2600000, 1201000)
				                               .LineTo(2601000, 1200000).Curve.Envelope;
				l.SpatialReference = srLv95;

				{
					ISpatialFilter sr = new SpatialFilterClass();
					sr.Geometry = l;
					sr.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

					foreach (var row in new EnumCursor((ITable) fcWgs84, sr, recycle: false))
					{
						nAutoPrj++;
					}
				}

				int nPrj = 0;
				{
					IGeometry prj = GeometryFactory.Clone(l);
					prj.Project(srWgs84);

					ISpatialFilter sr = new SpatialFilterClass();
					sr.Geometry = prj;
					sr.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

					foreach (var row in new EnumCursor((ITable) fcWgs84, sr, recycle: false))
					{
						nPrj++;
					}
				}
				Assert.AreEqual(nAutoPrj, nPrj);

				int nPrjEx = 0;
				{
					IGeometry prjEx = GeometryFactory.Clone(l);
					prjEx = SpatialReferenceUtils.ProjectEx(prjEx, srWgs84);

					ISpatialFilter sr = new SpatialFilterClass();
					sr.Geometry = prjEx;
					sr.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

					foreach (var row in new EnumCursor((ITable) fcWgs84, sr, recycle: false))
					{
						nPrjEx++;
					}
				}
				Assert.AreNotEqual(nAutoPrj, nPrjEx);
			}
		}

		[Test]
		public void CanUseTrProject()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;
			ISpatialReference srLv95 = SpatialReferenceUtils.CreateSpatialReference(idLv95, true);

			ISpatialReference srWgs84 = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRGeoCSType.esriSRGeoCS_WGS1984, true);

			//			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("ws");
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateTestFgdbWorkspace("trProjectTest");

			IFeatureClass fcLv95 = DatasetUtils.CreateSimpleFeatureClass(ws, "lv95",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srLv95, 1000)));

			IFeatureClass fcWgs84 = DatasetUtils.CreateSimpleFeatureClass(ws, "wgs84",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(
						"Shape", esriGeometryType.esriGeometryPolyline, srWgs84, 1000)));

			{
				IFeature f = fcLv95.CreateFeature();
				f.Shape = CurveConstruction.StartLine(2600000, 1200000)
				                           .LineTo(2601000, 1201000).Curve;
				f.Store();
			}
			IPolycurve l = CurveConstruction.StartLine(2600000, 1201000)
			                                .LineTo(2601000, 1200000).Curve;
			l.SpatialReference = srLv95;
			{
				IPolycurve l0 = GeometryFactory.Clone(l);
				l0.Project(srWgs84);

				IFeature f = fcWgs84.CreateFeature();
				f.Shape = l0;
				f.Store();
			}

			{
				IPolycurve l0 = SpatialReferenceUtils.ProjectEx(l, srWgs84);

				IFeature f = fcWgs84.CreateFeature();
				f.Shape = l0;
				f.Store();
			}

			IReadOnlyFeatureClass roWgs84 = ReadOnlyTableFactory.Create(fcWgs84);
			IReadOnlyFeatureClass roLv75 = ReadOnlyTableFactory.Create(fcLv95);
			TrProject tr = new TrProject(roWgs84, idLv95);
			QaInteriorIntersectsOther test =
				new QaInteriorIntersectsOther(tr.GetTransformed(), roLv75);

			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}

			// same spatial references in QaContainer --> valid
			{
				var runner = new QaContainerTestRunner(
					1000,
					new QaMinLength(tr.GetTransformed(), 0.0001),
					new QaMinLength(roLv75, 0.0001));
				runner.Execute();
				Assert.AreEqual(0, runner.Errors.Count);
			}

			// differing spatial references in QaContainer --> exception
			{
				var runner = new QaContainerTestRunner(
					1000,
					new QaMinLength(roWgs84, 0.0001),
					new QaMinLength(roLv75, 0.0001));
				bool success = false;
				try
				{
					runner.Execute();
					success = true;
				}
				catch (ArgumentException) { }

				Assert.IsFalse(success);
			}
		}

		[Test]
		public void CanHandleOutOfTileRequests()
		{
			int idLv95 = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95;

			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrProject");

			IFeatureClass featureClass =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
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

			TrProject tr = new TrProject(roPolyFc, idLv95)
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
