using System;
using System.Data;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaGroupConstraintsTest
	{
		private IFeatureWorkspace _testWs;
		private IFeatureWorkspace _fgdbWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			_testWs = TestWorkspaceUtils.CreateTestAccessWorkspace("QaGroupConstraintsTest");
			_fgdbWs =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("QaGroupConstraintsTest");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void TernaryLogicTest()
		{
			var tbl = new DataTable();
			tbl.Columns.Add("Int", typeof(int));
			tbl.Columns.Add("Expr", typeof(bool), "Int > 5");
			DataRow row = tbl.NewRow();
			tbl.Rows.Add(row);
			row.AcceptChanges();
			Assert.AreEqual(DBNull.Value, row["Expr"]);
			row["Int"] = 3;
			Assert.AreEqual(false, row["Expr"]);
			row["Int"] = 6;
			Assert.AreEqual(true, row["Expr"]);
		}

		[Test]
		public void TestGroupContraints()
		{
			TestGroupContraints(_testWs);
			TestGroupContraints(_fgdbWs);
		}

		private static void TestGroupContraints(IFeatureWorkspace ws)
		{
			ITable tbl =
				DatasetUtils.CreateTable(ws, "TestGroupContraints",
				                         FieldUtils.CreateOIDField(),
				                         FieldUtils.CreateTextField("Kostenstelle", 20));

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IRow row1 = tbl.CreateRow();
			row1.set_Value(1, "123456-10");
			row1.Store();

			IRow row2 = tbl.CreateRow();
			row2.set_Value(1, "123456-11");
			row2.Store();

			IRow row3 = tbl.CreateRow();
			row3.set_Value(1, "123456-11");
			row3.Store();

			IRow row4 = tbl.CreateRow();
			row4.set_Value(1, "023456-10");
			row4.Store();

			const bool limitToTestedRows = false;
			var test = new QaGroupConstraints(ReadOnlyTableFactory.Create(tbl),
			                                  "IIF(LEN(Kostenstelle) >=6, SUBSTRING(Kostenstelle, 1, 6), '')",
			                                  "SUBSTRING(Kostenstelle, 8, 9)",
			                                  1,
			                                  limitToTestedRows);

			using (var runner = new QaTestRunner(test))
			{
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}

			var containerRunner = new QaContainerTestRunner(100, test);
			containerRunner.Execute();
			Assert.AreEqual(1, containerRunner.Errors.Count);
		}

		[Test]
		public void TestGeomGroupConstraints()
		{
			TestGeomGroupConstraints(_testWs);
			TestGeomGroupConstraints(_fgdbWs);
		}

		private static void TestGeomGroupConstraints(IFeatureWorkspace ws)
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateTextField("Kostenstelle", 20));
			fields.AddField(FieldUtils.CreateShapeField(
				                "Shape", esriGeometryType.esriGeometryPoint,
				                SpatialReferenceUtils.CreateSpatialReference
				                ((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				                 true), 1000, false, false));

			IFeatureClass fc =
				DatasetUtils.CreateSimpleFeatureClass(ws, "TestGeomGroupContraints",
				                                      fields,
				                                      null);
			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IFeature row1 = fc.CreateFeature();
			row1.set_Value(1, "123456-10");
			row1.Shape = GeometryFactory.CreatePoint(200, 100);
			row1.Store();

			IFeature row2 = fc.CreateFeature();
			row2.set_Value(1, "123456-11");
			row2.Shape = GeometryFactory.CreatePoint(200, 200);
			row2.Store();

			IFeature row3 = fc.CreateFeature();
			row3.set_Value(1, "123456-11");
			row3.Shape = GeometryFactory.CreatePoint(200, 300);
			row3.Store();

			IFeature row4 = fc.CreateFeature();
			row4.set_Value(1, "023456-10");
			row4.Shape = GeometryFactory.CreatePoint(200, 150);
			row4.Store();

			const bool limitToTestedRows = false;
			var test = new QaGroupConstraints(ReadOnlyTableFactory.Create(fc),
			                                  "IIF(LEN(Kostenstelle) >=6, SUBSTRING(Kostenstelle, 1, 6), '')",
			                                  "SUBSTRING(Kostenstelle, 8, 9)", 1,
			                                  limitToTestedRows);

			{
				IEnvelope box = GeometryFactory.CreateEnvelope(150, 120, 250, 170);
				using (var runner = new QaTestRunner(test))
				{
					runner.Execute(box);
					Assert.AreEqual(0, runner.Errors.Count);
				}

				var containerRunner = new QaContainerTestRunner(100, test);
				containerRunner.Execute(box);
				Assert.AreEqual(0, containerRunner.Errors.Count);
			}
			{
				IEnvelope box = GeometryFactory.CreateEnvelope(150, 80, 250, 170);
				using (var runner = new QaTestRunner(test))
				{
					runner.Execute(box);
					Assert.AreEqual(1, runner.Errors.Count);
				}

				var containerRunner = new QaContainerTestRunner(100, test);
				containerRunner.Execute(box);
				Assert.AreEqual(1, containerRunner.Errors.Count);
			}
			{
				IEnvelope box = GeometryFactory.CreateEnvelope(150, 80, 250, 220);
				using (var runner = new QaTestRunner(test))
				{
					runner.Execute(box);
					Assert.AreEqual(1, runner.Errors.Count);
				}

				var containerRunner = new QaContainerTestRunner(100, test);
				containerRunner.Execute(box);
				Assert.AreEqual(1, containerRunner.Errors.Count);
			}

			{
				IRow row = fc.GetFeature(1);
				using (var runner = new QaTestRunner(test))
				{
					runner.Execute(row);
					Assert.AreEqual(1, runner.Errors.Count);
				}

				//var containerRunner = new QaContainerTestRunner(100, test);
				//containerRunner.Execute(row);
				//Assert.AreEqual(1, containerRunner.Errors.Count);
			}
			{
				IRow row = fc.GetFeature(4);
				using (var runner = new QaTestRunner(test))
				{
					runner.Execute(row);
					Assert.AreEqual(0, runner.Errors.Count);
				}

				//var containerRunner = new QaContainerTestRunner(100, test);
				//containerRunner.Execute(row);
				//Assert.AreEqual(1, containerRunner.Errors.Count);
			}

			{
				IQueryFilter filter = new QueryFilterClass { WhereClause = "ObjectId < 3" };
				ISelectionSet set = fc.Select(
					filter, esriSelectionType.esriSelectionTypeIDSet,
					esriSelectionOption.esriSelectionOptionNormal, null);
				using (var runner = new QaTestRunner(test))
				{
					runner.Execute(new EnumCursor(set, null, false));
					Assert.AreEqual(1, runner.Errors.Count);
				}

				var containerRunner = new QaContainerTestRunner(100, test);
				containerRunner.Execute(new[] { set });
				Assert.AreEqual(1, containerRunner.Errors.Count);
			}

			{
				IQueryFilter filter = new QueryFilterClass { WhereClause = "ObjectId > 3" };
				ISelectionSet set = fc.Select(
					filter, esriSelectionType.esriSelectionTypeIDSet,
					esriSelectionOption.esriSelectionOptionNormal, null);
				using (var runner = new QaTestRunner(test))
				{
					runner.Execute(new EnumCursor(set, null, false));
					Assert.AreEqual(0, runner.Errors.Count);
				}

				var containerRunner = new QaContainerTestRunner(100, test);
				containerRunner.Execute(new[] { set });
				Assert.AreEqual(0, containerRunner.Errors.Count);
			}
		}

		[Test]
		public void TestWorkspaceKeywords()
		{
			TestWorkspaceKeywords(_testWs);
			TestWorkspaceKeywords(_fgdbWs);
		}

		private static void TestWorkspaceKeywords(IFeatureWorkspace ws)
		{
			var wsSyntax = (ISQLSyntax) ws;
			IEnumBSTR keys = wsSyntax.GetKeywords();
			keys.Reset();

			for (string key = keys.Next(); key != null; key = keys.Next())
			{
				Console.Write(key);
				Console.Write(@";");
			}

			Console.WriteLine();
		}

		[Test]
		public void TestRelGroupContraints()
		{
			TestRelGroupContraints(_testWs);
			TestRelGroupContraints(_fgdbWs);
		}

		private static void TestRelGroupContraints(IFeatureWorkspace ws)
		{
			ITable tableData =
				DatasetUtils.CreateTable(ws, "TblData1",
				                         FieldUtils.CreateOIDField(),
				                         FieldUtils.CreateField("GroupField",
				                                                esriFieldType
					                                                .esriFieldTypeInteger));

			ITable tableRel = DatasetUtils.CreateTable(ws, "TblRel1",
			                                           FieldUtils.CreateOIDField(),
			                                           FieldUtils.CreateTextField(
				                                           "Kostenstelle", 20));

			IRelationshipClass rel = TestWorkspaceUtils.CreateSimple1NRelationship(
				ws, "rel", tableRel, tableData, "ObjectId", "GroupField");

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			for (int i = 0; i < 20; i++)
			{
				IRow row = tableData.CreateRow();
				row.set_Value(1, 1);
				row.Store();
			}

			for (int i = 0; i < 40; i++)
			{
				IRow row = tableData.CreateRow();
				row.set_Value(1, 2);
				row.Store();
			}

			for (int i = 0; i < 30; i++)
			{
				IRow row = tableData.CreateRow();
				row.set_Value(1, 3);
				row.Store();
			}

			IRow row1 = tableRel.CreateRow();
			row1.set_Value(1, "123456-10");
			row1.Store();

			IRow row2 = tableRel.CreateRow();
			row2.set_Value(1, "123456-11");
			row2.Store();

			IRow row3 = tableRel.CreateRow();
			row3.set_Value(1, "123456-12");
			row3.Store();

			const bool limitToTestedRows = false;
			ITable relTab = TableJoinUtils.CreateQueryTable(rel, JoinType.InnerJoin);

			var test = new QaGroupConstraints(ReadOnlyTableFactory.Create(relTab),
			                                  "IIF(LEN(TblRel1.Kostenstelle) >=6, SUBSTRING(TblRel1.Kostenstelle, 1, 6), '')",
			                                  "SUBSTRING(TblRel1.Kostenstelle, 8, 9)", 1,
			                                  limitToTestedRows);

			test.SetRelatedTables(new[]
			                      {
				                      ReadOnlyTableFactory.Create(tableData),
				                      ReadOnlyTableFactory.Create(tableRel)
			                      });
			using (var runner = new QaTestRunner(test))
			{
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}

			var containerRunner = new QaContainerTestRunner(100, test);
			containerRunner.Execute();
			Assert.AreEqual(1, containerRunner.Errors.Count);
		}

		[Test]
		public void TestDataColumnExpression()
		{
			var tbl = new DataTable();
			tbl.Columns.Add("Kostenstelle", typeof(string));
			tbl.Columns.Add("ColInt", typeof(int));

			tbl.Columns.Add("GroupBy", typeof(object),
			                "IIF(LEN(Kostenstelle) >=6, SUBSTRING(Kostenstelle, 1, 6), '')");

			var view = new DataView(tbl);
			view.Sort = "GroupBy";

			tbl.Rows.Add("123456-10", 1);
			tbl.Rows.Add("123456-11", 1);
			tbl.Rows.Add("123456-12", 1);
			tbl.Rows.Add("123456-13", 1);
			tbl.Rows.Add("023456-10", 1);
		}

		[Test]
		public void TestValueInUniqueTable()
		{
			TestValueInUniqueTable(_testWs);
		}

		private static void TestValueInUniqueTable(IFeatureWorkspace ws)
		{
			ITable table1 =
				DatasetUtils.CreateTable(ws, "TestValueInUniqueTable1",
				                         FieldUtils.CreateOIDField(),
				                         FieldUtils.CreateField("RouteId",
				                                                esriFieldType
					                                                .esriFieldTypeInteger));

			ITable table2 =
				DatasetUtils.CreateTable(ws, "TestValueInUniqueTable2",
				                         FieldUtils.CreateOIDField(),
				                         FieldUtils.CreateField("OtherId",
				                                                esriFieldType
					                                                .esriFieldTypeInteger));

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			IRow row1 = table1.CreateRow();
			row1.set_Value(1, 3);
			row1.Store();

			for (int i = 0; i < 5; i++)
			{
				IRow r = table1.CreateRow();
				r.set_Value(1, 8);
				r.Store();
			}

			IRow row2 = table2.CreateRow();
			row2.set_Value(1, 3);
			row2.Store();

			const bool limitToTestedRows = false;
			var test = new QaGroupConstraints(
				new[] { ReadOnlyTableFactory.Create(table1), ReadOnlyTableFactory.Create(table2) },
				new[] { "RouteID", "OtherId" },
				new[] { "'Haltung'", "'B'" },
				1, limitToTestedRows);

			using (var runner = new QaTestRunner(test))
			{
				runner.Execute();
				Assert.AreEqual(1, runner.Errors.Count);
			}

			var containerRunner = new QaContainerTestRunner(100, test);
			containerRunner.Execute();
			Assert.AreEqual(1, containerRunner.Errors.Count);
		}
	}
}
