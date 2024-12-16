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
using ProSuite.QA.Tests.IssueFilters;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using ProSuite.QA.Tests.Transformers;
using ProSuite.QA.Tests.Transformers.Filters;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TrSpatialJoinTest
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
		public void CanIntersect()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrSpatialJoin");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline);
			IFeatureClass polyFc =
				CreateFeatureClass(ws, "polyFc", esriGeometryType.esriGeometryPolygon);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(10, 10).LineTo(10, 30).LineTo(30, 30)
				                           .LineTo(30, 10).ClosePolygon();
				f.Store();
			}

			TrSpatialJoin tr = new TrSpatialJoin(
				                   ReadOnlyTableFactory.Create(polyFc),
				                   ReadOnlyTableFactory.Create(lineFc))
			                   {
				                   Grouped = true,
				                   T1Attributes = new[] { "COUNT(OBJECTID) AS LineCount" }
			                   };

			TransformedFeatureClass transformedClass = tr.GetTransformed();
			WriteFieldNames(transformedClass);

			{
				QaConstraint test = new QaConstraint(transformedClass, "LineCount = 2");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(transformedClass, "LineCount > 2");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
		}

		[Test]
		public void CanOuterJoin()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrSpatialJoin");

			IFeatureClass lineFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline);
			IFeatureClass polyFc =
				CreateFeatureClass(ws, "polyFc", esriGeometryType.esriGeometryPolygon);

			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(5, 5).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Shape = CurveConstruction.StartPoly(10, 10).LineTo(10, 30).LineTo(30, 30)
				                           .LineTo(30, 10).ClosePolygon();
				f.Store();
			}

			TrSpatialJoin tr = new TrSpatialJoin(
				                   ReadOnlyTableFactory.Create(lineFc),
				                   ReadOnlyTableFactory.Create(polyFc))
			                   {
				                   Grouped = false,
				                   T1Attributes = new[]
				                                  {
					                                  // If T1Attribute is not not null, only specified attributes are added
					                                  "OBJECTID as Poly_OID",
					                                  "COUNT(OBJECTID) AS PolyCount"
				                                  },
				                   OuterJoin = true
			                   };

			TransformedFeatureClass transformedClass = tr.GetTransformed();
			WriteFieldNames(transformedClass);

			{
				QaConstraint test = new QaConstraint(transformedClass, "PolyCount = 2");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();

				// 2 times the first feature  (with PolyCount = 1), one time the second (with no poly-row)
				Assert.AreEqual(3, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(transformedClass,
				                                     "Poly_OID IS NULL AND PolyCount IS NULL");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}

			// Outer joined grouped:
			tr = new TrSpatialJoin(
				     ReadOnlyTableFactory.Create(lineFc),
				     ReadOnlyTableFactory.Create(polyFc))
			     {
				     Grouped = true,
				     T1Attributes = new[] { "COUNT(OBJECTID) AS PolyCount" },
				     OuterJoin = true
			     };

			transformedClass = tr.GetTransformed();
			WriteFieldNames(transformedClass);

			{
				QaConstraint test = new QaConstraint(transformedClass, "PolyCount = 2");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();

				// The first feature has PolyCount = 2 -> no error
				// The second feture has PolyCount null -> 1 error
				// 1 time the first feature  (with PolyCount = 2), one time the second (with no poly-row)
				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(transformedClass,
				                                     "PolyCount IS NULL");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();

				// First feature -> error
				// Second feature: no error
				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void CanOuterJoinPointLine()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrSpatialJoin");

			IFeatureClass pointFc =
				CreateFeatureClass(ws, "pointFc", esriGeometryType.esriGeometryPoint);
			IFeatureClass linFc =
				CreateFeatureClass(ws, "lineFc", esriGeometryType.esriGeometryPolyline);

			{
				IFeature p = pointFc.CreateFeature();
				p.Shape = GeometryFactory.CreatePoint(10, 10);
				p.Store();
			}
			{
				IFeature p = pointFc.CreateFeature();
				p.Shape = GeometryFactory.CreatePoint(20, 20);
				p.Store();
			}
			{
				IFeature p = pointFc.CreateFeature();
				p.Shape = GeometryFactory.CreatePoint(20, 10);
				p.Store();
			}

			{
				IFeature f = linFc.CreateFeature();
				f.Shape = CurveConstruction.StartLine(10, 10).LineTo(20, 20).Curve;
				f.Store();
			}

			TrSpatialJoin tr = new TrSpatialJoin(
				                   ReadOnlyTableFactory.Create(pointFc),
				                   ReadOnlyTableFactory.Create(linFc))
			                   {
				                   Grouped = false,
				                   T1Attributes = new[]
				                                  {
					                                  // If T1Attribute is not not null, only specified attributes are added
					                                  "OBJECTID as Line_OID",
					                                  "COUNT(OBJECTID) AS LineCount"
				                                  },
				                   OuterJoin = true
			                   };

			TransformedFeatureClass transformedClass = tr.GetTransformed();
			WriteFieldNames(transformedClass);

			{
				QaConstraint test = new QaConstraint(transformedClass, "LineCount = 1");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();

				Assert.AreEqual(1, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(transformedClass, "LineCount IS NULL");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();

				Assert.AreEqual(2, runner.Errors.Count);
			}
		}

		[Test]
		public void CanAccessAttributes()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrSpatialJoin");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr") });
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 14;
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 8;
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 6;
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}

			TrSpatialJoin tr = new TrSpatialJoin(
				                   ReadOnlyTableFactory.Create(polyFc),
				                   ReadOnlyTableFactory.Create(lineFc))
			                   { Grouped = false };

			tr.T0Attributes = new List<string>
			                  {
				                  "MIN(OBJECTID) AS t0Oid",
				                  "Nr AS liNR",
			                  };
			tr.T1Attributes = new List<string>
			                  {
				                  "Nr",
				                  "Nr as alias_Nr",
				                  "MIN(OBJECTID) AS minObi",
				                  "Count(Nr) as AnzUnqualifiziert",
				                  "SUM(Nr) as SummeUnqualifiziert"
			                  };

			// NOTE: Cross-Field Calculations are only supported on t1 and only for
			//       pre-existing fields!
			// -> Consider separate transformer for more flexibility: trCalculateFields
			tr.T1CalcAttributes = new List<string>
			                      {
				                      "OBJECTID + NR as crossFieldSum"
			                      };

			TransformedFeatureClass transformedClass = tr.GetTransformed();
			WriteFieldNames(transformedClass);

			{
				QaConstraint test = new QaConstraint(transformedClass, "liNR > 10");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(4, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(transformedClass, "liNR > 10");
				IFilterEditTest ft = test;
				ft.SetIssueFilters(
					"filter",
					new List<IIssueFilter>
					{ new IfInvolvedRows("liNR + Nr = 20") { Name = "filter" } });
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
		}

		[Test]
		public void CanDeDuplicateAutoAddedAttributes()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrSpatialJoin");

			IFeatureClass lineFc =
				CreateFeatureClass(
					ws, "lineFc", esriGeometryType.esriGeometryPolyline,
					new[] { FieldUtils.CreateIntegerField("Nr") });
			IFeatureClass polyFc =
				CreateFeatureClass(
					ws, "polyFc", esriGeometryType.esriGeometryPolygon,
					new[] { FieldUtils.CreateIntegerField("Nr") });
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 12;
				f.Shape = CurveConstruction.StartLine(0, 0).LineTo(69.5, 69.5).Curve;
				f.Store();
			}
			{
				IFeature f = lineFc.CreateFeature();
				f.Value[1] = 14;
				f.Shape = CurveConstruction.StartLine(60, 40).LineTo(60, 80).Curve;
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 8;
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = polyFc.CreateFeature();
				f.Value[1] = 6;
				f.Shape = CurveConstruction.StartPoly(50, 50).LineTo(50, 70).LineTo(70, 70)
				                           .LineTo(70, 50).ClosePolygon();
				f.Store();
			}

			TrSpatialJoin tr = new TrSpatialJoin(
				                   ReadOnlyTableFactory.Create(polyFc),
				                   ReadOnlyTableFactory.Create(lineFc))
			                   { Grouped = false };

			// NOTE: Cross-Field Calculations are only supported on t1 and only for
			//       pre-existing fields!
			// -> Consider separate transformer for more flexibility: trCalculateFields
			tr.T1CalcAttributes = new List<string>
			                      {
				                      "OBJECTID + NR as crossFieldSum"
			                      };

			TransformedFeatureClass transformedClass = tr.GetTransformed();
			WriteFieldNames(transformedClass);
			Assert.True(transformedClass.Fields.FindField("polyFc_Nr") > 0);
			Assert.True(transformedClass.Fields.FindField("lineFc_Nr") > 0);

			{
				QaConstraint test = new QaConstraint(transformedClass, "polyFc_NR > 10");
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(4, runner.Errors.Count);
			}
			{
				QaConstraint test = new QaConstraint(transformedClass, "polyFc_NR > 10");
				IFilterEditTest ft = test;
				ft.SetIssueFilters(
					"filter",
					new List<IIssueFilter>
					{ new IfInvolvedRows("lineFc_NR + polyFc_Nr = 20") { Name = "filter" } });
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
		}

		[Test]
		public void Top5734()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("gebaeude");
			IFeatureClass fcGe = CreateFeatureClass(
				ws, "TLM_GEBAEUDEEINHEIT", esriGeometryType.esriGeometryPolygon,
				new List<IField>
				{
					FieldUtils.CreateField("UUID", esriFieldType.esriFieldTypeGUID)
				});

			IFeatureClass fcGk = CreateFeatureClass(
				ws, "TLM_GEBAEUDEKOERPER", esriGeometryType.esriGeometryPolygon,
				new List<IField>
				{
					FieldUtils.CreateField("TLM_GEBAEUDEEINHEIT_UUID",
					                       esriFieldType.esriFieldTypeGUID),
					FieldUtils.CreateIntegerField("OBJEKTART"),
				});

			string uuid = Guid.NewGuid().ToString("B");
			{
				IFeature f = fcGe.CreateFeature();
				f.Value[1] = uuid;
				f.Shape = CurveConstruction.StartPoly(990, 990).LineTo(1010, 990).LineTo(1010, 1010)
				                           .LineTo(990, 1010).LineTo(990, 990).ClosePolygon();
				f.Store();
			}

			{
				IFeature f = fcGk.CreateFeature();
				f.Value[1] = uuid;
				f.Value[2] = 2;
				f.Shape = CurveConstruction.StartPoly(990, 990).LineTo(1010, 990).LineTo(1010, 1010)
				                           .LineTo(990, 1010).LineTo(990, 990).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = fcGk.CreateFeature();
				f.Value[1] = uuid;
				f.Value[2] = 2;
				f.Shape = CurveConstruction.StartPoly(990, 990).LineTo(1010, 990).LineTo(1010, 1010)
				                           .LineTo(990, 1010).LineTo(990, 990).ClosePolygon();
				f.Store();
			}

			uuid = Guid.NewGuid().ToString("B");
			{
				IFeature f = fcGe.CreateFeature();
				f.Value[1] = uuid;
				f.Shape = CurveConstruction
				          .StartPoly(1190, 990).LineTo(1210, 990).LineTo(1210, 1010)
				          .LineTo(1190, 1010).LineTo(1190, 990).ClosePolygon();
				f.Store();
			}
			{
				IFeature f = fcGk.CreateFeature();
				f.Value[1] = uuid;
				f.Value[2] = 2;
				f.Shape = CurveConstruction
				          .StartPoly(1190, 990).LineTo(1210, 990).LineTo(1210, 1010)
				          .LineTo(1190, 1010).LineTo(1190, 990).ClosePolygon();
				f.Store();
			}

			IReadOnlyFeatureClass ge = ReadOnlyTableFactory.Create(fcGe);
			IReadOnlyFeatureClass gk = ReadOnlyTableFactory.Create(fcGk);

			TrSpatialJoin trSj = new TrSpatialJoin(ge, gk);
			trSj.Constraint = "t0.UUID = t1.TLM_GEBAEUDEEINHEIT_UUID";
			trSj.OuterJoin = false;
			trSj.NeighborSearchOption = TrSpatialJoin.SearchOption.All;
			trSj.Grouped = true;
			trSj.T0Attributes = new[] { "UUID" };
			trSj.T1Attributes = new[] { "SUM(GEBAEUDEKOERPER) AS ANZAHL_GEBAEUDEKOERPER" };
			trSj.T1CalcAttributes = new[] { "IIF(OBJEKTART=2,1,0)  AS GEBAEUDEKOERPER" };

			QaConstraint test =
				new QaConstraint(trSj.GetTransformed(), "ANZAHL_GEBAEUDEKOERPER = 1");

			{
				var runner = new QaContainerTestRunner(1000, test);
				runner.Execute(GeometryFactory.CreateEnvelope(0, 0, 1200, 1200));

				Assert.AreEqual(1, runner.Errors.Count);
			}
		}

		[Test]
		public void Top5728()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("Bielersee");

			{
				IFeatureClass fc =
					CreateFeatureClass(
						ws, "TLM_FLIESSGEWAESSER", esriGeometryType.esriGeometryPolyline);

				{
					IFeature f = fc.CreateFeature();
					f.Shape = CurveConstruction.StartLine(2576272, 1211983).LineTo(2576125, 1212189)
					                           .Curve;
					f.Store();
				}
				{
					IFeature f = fc.CreateFeature();
					f.Shape = CurveConstruction.StartLine(2576125, 1212189).LineTo(2575705, 1212739)
					                           .Curve;
					f.Store();
				}
			}
			string uuid = Guid.NewGuid().ToString("B");
			{
				IFeatureClass fc =
					CreateFeatureClass(
						ws, "TLM_STEHENDES_GEWAESSER", esriGeometryType.esriGeometryPolyline,
						new[]
						{
							FieldUtils.CreateField("TLM_GEWAESSER_LAUF_UUID",
							                       esriFieldType.esriFieldTypeGUID)
						});
				{
					IFeature f = fc.CreateFeature();
					f.Shape = CurveConstruction
					          .StartLine(2576125, 1212189).LineTo(2574259, 1210759)
					          .LineTo(2576119, 1211673)
					          .Curve;
					f.Value[1] = uuid;
					f.Store();
				}
				{
					IFeature f = fc.CreateFeature();
					f.Shape = CurveConstruction
					          .StartLine(2576119, 1211673).LineTo(2577695, 1213983)
					          .LineTo(2576125, 1212189)
					          .Curve;
					f.Value[1] = uuid;
					f.Store();
				}
			}
			{
				ITable tb = DatasetUtils.CreateTable(ws, "TLM_GEWAESSER_LAUF",
				                                     FieldUtils.CreateField(
					                                     "UUID", esriFieldType.esriFieldTypeGUID));
				{
					IRow r = tb.CreateRow();
					r.Value[0] = uuid;
					r.Store();
				}
			}

			var fws = ws;
			IReadOnlyFeatureClass fcFg =
				ReadOnlyTableFactory.Create(fws.OpenFeatureClass("TLM_FLIESSGEWAESSER"));
			IReadOnlyFeatureClass fcSg =
				ReadOnlyTableFactory.Create(fws.OpenFeatureClass("TLM_STEHENDES_GEWAESSER"));
			IReadOnlyTable tbGl = ReadOnlyTableFactory.Create(fws.OpenTable("TLM_GEWAESSER_LAUF"));

			TrGeometryToPoints trEp = new TrGeometryToPoints(fcFg, GeometryComponent.LineEndPoints);
			trEp.TransformerName = "trEp";

			TrTableJoinInMemory trIm =
				new TrTableJoinInMemory(
					fcSg, tbGl, "TLM_GEWAESSER_LAUF_UUID", "UUID", JoinType.InnerJoin);
			trIm.TransformerName = "trIm";

			TrOnlyDisjointFeatures trDj =
				new TrOnlyDisjointFeatures((IReadOnlyFeatureClass) trIm.GetTransformed(),
				                           trEp.GetTransformed());
			trDj.TransformerName = "trDj";
			trDj.FilteringSearchOption = TrSpatiallyFiltered.SearchOption.All;

			QaExportTables test = new QaExportTables(
				new List<IReadOnlyTable>
				{
					fcFg, fcSg,
					trEp.GetTransformed(), trIm.GetTransformed(), trDj.GetTransformed()
				},
				"c:\\temp\\top5728_*.gdb"
			);
			test.ExportTileIds = true;
			test.ExportTiles = true;

			{
				var runner = new QaContainerTestRunner(6000, test);
				runner.Execute(GeometryFactory.CreateEnvelope(2571000, 1209000, 2585000, 1221000));
			}

			IWorkspace exportWorkspace = WorkspaceUtils.OpenFileGdbWorkspace(test.UsedFileGdbPath);
			IFeatureClass fcTrDj = ((IFeatureWorkspace) exportWorkspace).OpenFeatureClass("trDj");
			Assert.AreEqual(0, fcTrDj.FeatureCount(null));
		}

		[Test]
		public void CanHandleOutOfTileRequests()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateInMemoryWorkspace("TrIntersect");

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

			TrSpatialJoin tr = new TrSpatialJoin(roPolyFc, roPolyFc)
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
					// second tile: leftOfSecond, found twice because it intersects 2 tiles!
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
