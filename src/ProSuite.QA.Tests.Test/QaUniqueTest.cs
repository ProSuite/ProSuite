using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.TestData;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaUniqueTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _pgdbWorkspace;
		private IFeatureWorkspace _fgdbWorkspace;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();

			const string databaseName = "TestUnique";

			_pgdbWorkspace = TestWorkspaceUtils.CreateTestAccessWorkspace(databaseName);
			_fgdbWorkspace = TestWorkspaceUtils.CreateTestFgdbWorkspace(databaseName);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestUniqueIntegers()
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Unique",
			                                       esriFieldType.esriFieldTypeInteger));
			ITable table = TestWorkspaceUtils.CreateSimpleTable(_pgdbWorkspace, "Unique1",
			                                                    fields);

			for (var i = 0; i < 10; i++)
			{
				IRow row = table.CreateRow();
				row.set_Value(1, i);
				row.Store();
			}

			foreach (bool forceInMemoryTableSort in new[] {false, true})
			{
				var test = new QaUnique(table, "Unique")
				           {
					           ForceInMemoryTableSorting = forceInMemoryTableSort
				           };

				var runner = new QaTestRunner(test);
				runner.Execute();
				AssertUtils.NoError(runner);
			}
		}

		[Test]
		public void TestUniqueStrings()
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001, 0.001);

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("Unique", 100),
				FieldUtils.CreateShapeField(
					"Shape", esriGeometryType.esriGeometryPoint,
					sref, 1000));

			IFeatureClass featureClass = DatasetUtils.CreateSimpleFeatureClass(_pgdbWorkspace,
			                                                                   "TestUniqueStrings",
			                                                                   fields);

			for (var i = 0; i < 10; i++)
			{
				IFeature feature = featureClass.CreateFeature();
				feature.set_Value(1, string.Format("A'{0}{1}", i, "\""));
				feature.Shape = GeometryFactory.CreatePoint(100, 100, sref);
				feature.Store();
			}

			IEnvelope testEnvelope = GeometryFactory.CreateEnvelope(0, 0, 200, 200, sref);

			foreach (bool forceInMemoryTableSort in new[] {false, true})
			{
				var test = new QaUnique((ITable) featureClass, "Unique")
				           {
					           ForceInMemoryTableSorting = forceInMemoryTableSort
				           };
				var runner = new QaTestRunner(test);
				runner.Execute(testEnvelope);
				AssertUtils.NoError(runner);
			}
		}

		[Test]
		public void TestStringsWithNulls()
		{
			Console.WriteLine(@"In Memory Gdb");
			TestStringsWithNulls(
				TestWorkspaceUtils.CreateInMemoryWorkspace("TestStringsWithNulls"));
			Console.WriteLine(@"-----------------");

			Console.WriteLine(@"FileGdb");
			TestStringsWithNulls(_fgdbWorkspace);
			Console.WriteLine(@"-----------------");

			Console.WriteLine(@"Personal Gdb");
			TestStringsWithNulls(_pgdbWorkspace);
			Console.WriteLine(@"-----------------");
		}

		private static void TestStringsWithNulls([NotNull] IFeatureWorkspace workspace)
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001, 0.001);

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("Unique", 100),
				FieldUtils.CreateShapeField(
					"Shape", esriGeometryType.esriGeometryPoint,
					sref, 1000));

			IFeatureClass featureClass = DatasetUtils.CreateSimpleFeatureClass(
				workspace,
				"TestStringsWithNulls",
				fields);

			IFeature f = featureClass.CreateFeature();
			f.Shape = GeometryFactory.CreatePoint(100, 100, sref);
			f.Store();

			for (var i = 0; i < 300; i++)
			{
				IFeature feature = featureClass.CreateFeature();
				feature.set_Value(1, string.Format("A'{0}{1}", i, "\""));
				feature.Shape = GeometryFactory.CreatePoint(100, 100, sref);
				feature.Store();
			}

			f = featureClass.CreateFeature();
			f.set_Value(1, string.Format("A'{0}{1}", 4, "\""));
			f.Shape = GeometryFactory.CreatePoint(100, 100, sref);
			f.Store();

			f = featureClass.CreateFeature();
			f.Shape = GeometryFactory.CreatePoint(300, 100, sref);
			f.Store();

			IEnvelope testEnvelope = GeometryFactory.CreateEnvelope(0, 0, 200, 200, sref);

			foreach (bool forceInMemoryTableSort in new[] {true, false})
			{
				var test = new QaUnique((ITable) featureClass, "Unique", maxRows: 200)
				           {
					           ForceInMemoryTableSorting = forceInMemoryTableSort
				           };

				using (var runner = new QaTestRunner(test))
				{
					runner.Execute();
					Assert.AreEqual(4, runner.Errors.Count);
				}

				using (var runner = new QaTestRunner(test))
				{
					runner.Execute(testEnvelope);
					Assert.AreEqual(4, runner.Errors.Count);
				}

				using (var runner = new QaTestRunner(test))
				{
					runner.Execute();
					Assert.AreEqual(4, runner.Errors.Count);
				}

				using (var runner = new QaTestRunner(test))
				{
					runner.Execute(testEnvelope);
					Assert.AreEqual(4, runner.Errors.Count);
				}
			}
		}

		[Test]
		public void TestUniqueStringsMulti()
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001, 0.001);

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateTextField("Unique", 100),
				FieldUtils.CreateShapeField(
					"Shape", esriGeometryType.esriGeometryPoint,
					sref, 1000));

			IFeatureClass featureClass1 = DatasetUtils.CreateSimpleFeatureClass(_pgdbWorkspace,
			                                                                    "TestUniqueStringsMulti1",
			                                                                    fields);
			IFeatureClass featureClass2 = DatasetUtils.CreateSimpleFeatureClass(_pgdbWorkspace,
			                                                                    "TestUniqueStringsMulti2",
			                                                                    fields);

			for (var i = 0; i < 10; i++)
			{
				IFeature feature = featureClass1.CreateFeature();
				feature.set_Value(1, string.Format("A'{0}{1}", i, "\""));
				feature.Shape = GeometryFactory.CreatePoint(100, 100, sref);
				feature.Store();
			}

			for (var i = 0; i < 2; i++)
			{
				IFeature emptyFeature = featureClass1.CreateFeature();
				emptyFeature.set_Value(1, null);
				emptyFeature.Shape = GeometryFactory.CreatePoint(100, 100, sref);
				emptyFeature.Store();
			}

			for (var i = 0; i < 10; i++)
			{
				IFeature feature = featureClass2.CreateFeature();
				feature.set_Value(1, string.Format("B'{0}{1}", i, "\""));
				feature.Shape = GeometryFactory.CreatePoint(100, 100, sref);
				feature.Store();
			}

			for (var i = 0; i < 2; i++)
			{
				IFeature emptyFeature = featureClass2.CreateFeature();
				emptyFeature.set_Value(1, null);
				emptyFeature.Shape = GeometryFactory.CreatePoint(100, 100, sref);
				emptyFeature.Store();
			}

			foreach (bool forceInMemoryTableSort in new[] {true, false})
			{
				var test = new QaUnique(new[] {(ITable) featureClass1, (ITable) featureClass2},
				                        new[] {"Unique", "Unique"})
				           {
					           ForceInMemoryTableSorting = forceInMemoryTableSort
				           };

				var runner = new QaTestRunner(test);
				runner.Execute();

				Assert.AreEqual(4, runner.Errors.Count);
			}
		}

		[Test]
		public void TestUniqueGuid()
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001, 0.001);

			IFields fields = FieldUtils.CreateFields(
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateField("UniqueValue", esriFieldType.esriFieldTypeGUID),
				FieldUtils.CreateShapeField(
					"Shape", esriGeometryType.esriGeometryPoint,
					sref, 1000));

			IFeatureClass featureClass1 = DatasetUtils.CreateSimpleFeatureClass(_fgdbWorkspace,
			                                                                    "TestUniqueGuid",
			                                                                    fields);

			for (var i = 0; i < 10; i++)
			{
				IFeature feature = featureClass1.CreateFeature();
				feature.set_Value(1, Guid.NewGuid().ToString("B"));
				feature.Shape = GeometryFactory.CreatePoint(100, 100, sref);
				feature.Store();
			}

			for (var i = 0; i < 2; i++)
			{
				IFeature emptyFeature = featureClass1.CreateFeature();
				emptyFeature.set_Value(1, null);
				emptyFeature.Shape = GeometryFactory.CreatePoint(100, 100, sref);
				emptyFeature.Store();
			}

			foreach (bool forceInMemoryTableSort in new[] {true, false})
			{
				var test = new QaUnique((ITable) featureClass1, "UniqueValue")
				           {
					           ForceInMemoryTableSorting = forceInMemoryTableSort
				           };

				var runner = new QaTestRunner(test);
				runner.Execute();
				Assert.AreEqual(2, runner.Errors.Count);
			}
		}

		[Test]
		public void TestNonUniqueIntegers()
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("Unique",
			                                       esriFieldType.esriFieldTypeInteger));
			ITable table = TestWorkspaceUtils.CreateSimpleTable(_pgdbWorkspace, "NonUnique",
			                                                    fields);

			for (var i = 0; i < 10; i++)
			{
				IRow row = table.CreateRow();
				row.set_Value(1, i);
				row.Store();
			}

			// create error values
			{
				IRow row = table.CreateRow();
				row.set_Value(1, 5);
				row.Store();
			}
			IRow errorRow = table.CreateRow();
			errorRow.set_Value(1, 5);
			errorRow.Store();
			{
				IRow row = table.CreateRow();
				row.set_Value(1, 7);
				row.Store();
			}
			IRow validRow = table.CreateRow();
			validRow.set_Value(1, 12);
			validRow.Store();

			foreach (bool forceInMemoryTableSort in new[] {true, false})
			{
				// init Test
				var test = new QaUnique(table, "Unique")
				           {
					           ForceInMemoryTableSorting = forceInMemoryTableSort
				           };

				// Run global test
				var runner = new QaTestRunner(test);
				runner.Execute();
				Assert.AreEqual(5, runner.Errors.Count); // each non unique row is reported

				// Run valid row
				runner = new QaTestRunner(test);
				runner.Execute(new[] {validRow});
				AssertUtils.NoError(runner);

				// Run error row
				runner = new QaTestRunner(test);
				runner.Execute(new[] {errorRow});
				Assert.AreEqual(3, runner.Errors.Count); // '5' exists in 3 rows

				// Run combined
				runner = new QaTestRunner(test);
				runner.Execute(new[] {validRow, errorRow});
				Assert.AreEqual(3, runner.Errors.Count);
			}
		}

		[Test]
		public void CanDetectNtoMUnique_FileGdb()
		{
			CanDetectNtoMUnique(_fgdbWorkspace);
		}

		[Test]
		public void CanDetectNtoMUnique_PersonalGdb()
		{
			CanDetectNtoMUnique(_pgdbWorkspace);
		}

		[Test]
		public void CanDetectNtoMNonUnique_PersonalGdb()
		{
			CanDetectNtoMNonUnique(_pgdbWorkspace);
		}

		[Test]
		public void CanDetectNtoMNonUnique_FileGdb()
		{
			CanDetectNtoMNonUnique(_fgdbWorkspace);
		}

		[Test]
		public void CanDetect1toNUnique_PersonalGdb()
		{
			CanDetect1ToNUnique(_pgdbWorkspace);
		}

		[Test]
		public void CanDetect1toNUnique_FileGdb()
		{
			CanDetect1ToNUnique(_fgdbWorkspace);
		}

		[Test]
		public void CanDetect1toNNonUnique_PersonalGdb()
		{
			CanDetect1toNNonUnique(_pgdbWorkspace);
		}

		[Test]
		public void CanDetect1toNNonUnique_FileGdb()
		{
			ITable relTable = CanDetect1toNNonUnique(_fgdbWorkspace);

			int rowCount = relTable.RowCount(null);
			var firstUniqueFieldName = "Relate1NNonUnique1.Unique";

			// TableSort verification
			int sortCount = 0;
			ITableSort tableSort = TableSortUtils.CreateTableSort(relTable,
			                                                      firstUniqueFieldName);

			tableSort.Compare = new FieldSortCallback();
			tableSort.QueryFilter = null;
			tableSort.Sort(null);

			ICursor rows = tableSort.Rows;
			while (rows.NextRow() != null)
			{
				sortCount++;
			}

			string version = RuntimeUtils.Version;
			double v;
			if (double.TryParse(version, out v) && v < 10.4)
			{
				Assert.AreEqual(rowCount, sortCount);
			}
			else
			{
				Assert.IsTrue(rowCount >
				              sortCount); // bug in TableSort for joined FGDB-Tables, since 10.4
			}

			int orderByCount = 0;
			// Order By verification
			int fieldIndex = relTable.FindField(firstUniqueFieldName);
			IQueryFilter filter = new QueryFilterClass();
			((IQueryFilterDefinition) filter).PostfixClause =
				$"ORDER BY {firstUniqueFieldName}";
			int pre = int.MinValue;
			foreach (IRow row in new EnumCursor(relTable, filter, false))
			{
				int id = (int) row.Value[fieldIndex];
				Assert.IsTrue(pre <= id);
				pre = id;

				orderByCount++;
			}

			Assert.AreEqual(rowCount, orderByCount);
		}

		private class FieldSortCallback : ITableSortCallBack
		{
			public int Compare(object value1, object value2, int fieldIndex, int fieldSortIndex)
			{
				return ((IComparable) value1).CompareTo(value2);
			}
		}

		[Test]
		public void CanCheckGuids()
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("UUID",
			                                       esriFieldType.esriFieldTypeGUID));
			ITable table = TestWorkspaceUtils.CreateSimpleTable(_fgdbWorkspace, "CanCheckGuids",
			                                                    fields);

			Guid latest = Guid.NewGuid();
			IRow row;
			for (var i = 0; i < 10; i++)
			{
				latest = Guid.NewGuid();

				row = table.CreateRow();
				row.set_Value(1, latest.ToString("B"));
				row.Store();
			}

			row = table.CreateRow();
			row.set_Value(1, latest.ToString("B"));
			row.Store();

			//IWorkspace ws = TestDataUtils.OpenTopgisTlm();
			//ITable table = ((IFeatureWorkspace) ws).OpenTable("TOPGIS_TLM.TLM_STRASSE");
			var test = new QaUnique(table, "UUID");

			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanCheckGuidsMultiTable()
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("UUID",
			                                       esriFieldType.esriFieldTypeGUID));
			ITable table = TestWorkspaceUtils.CreateSimpleTable(_fgdbWorkspace,
			                                                    "CanCheckGuids1", fields);

			fields.AddField(FieldUtils.CreateField("UUID2",
			                                       esriFieldType.esriFieldTypeGUID));
			ITable table2 = TestWorkspaceUtils.CreateSimpleTable(_fgdbWorkspace,
			                                                     "CanCheckGuids2", fields);

			Guid latest = Guid.NewGuid();
			IRow row;
			for (var i = 0; i < 10; i++)
			{
				latest = Guid.NewGuid();

				row = table.CreateRow();
				row.set_Value(1, latest.ToString("B"));
				row.Store();
			}

			row = table2.CreateRow();
			row.set_Value(2, latest.ToString("B"));
			row.Store();

			//IWorkspace ws = TestDataUtils.OpenTopgisTlm();
			//ITable table = ((IFeatureWorkspace) ws).OpenTable("TOPGIS_TLM.TLM_STRASSE");
			var test = new QaUnique(new[] {table, table2}, new[] {"UUID", "UUID2"});

			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		[Ignore("Uses local data")]
		public void CanCheckEinzelbaum()
		{
			IWorkspace topgis = TestDataUtils.OpenTopgisTlm();
			ITable table =
				((IFeatureWorkspace) topgis).OpenTable("TOPGIS_TLM.TLM_EINZELBAUM_GEBUESCH");

			IFeatureWorkspace local = WorkspaceUtils.OpenFileGdbFeatureWorkspace(
				@"c:\data\unitTests\felix\testdata.gdb");
			ITable table2 = local.OpenTable("TLM_EINZELBAUM_GEBUESCH");

			//var test = new QaUnique(new[] { table, table2 }, new[] { "OBJECTID,UUID", "OID_COPY,UUID" });
			var test = new QaUnique(new[] {table2, table},
			                        new[] {"Uuid,Oid_COPY", "UUID,OBJECTID"});
			test.SetConstraint(0, "OBJECTID < 1052400");
			test.SetConstraint(1, "OBJECTID < 1052400");

			var runner = new QaTestRunner(test);
			runner.Execute();
		}

		[Test]
		[Ignore("Use TOPGIS connection")]
		public void CanCheckBB()
		{
			// response time with ORDER BY: 3:53
			IWorkspace workspace =
				WorkspaceUtils.OpenSDEWorkspace(DirectConnectDriver.Oracle11g, "TOPGIST",
				                                "SDE.DEFAULT");

			ITable table =
				((IFeatureWorkspace) workspace).OpenTable("TOPGIS_TLM.TLM_EINZELBAUM_GEBUESCH");
			ITable table2 =
				((IFeatureWorkspace) workspace).OpenTable("TOPGIS_TLM.TLM_BODENBEDECKUNG");

			//var test = new QaUnique(new[] { table, table2 }, new[] { "OBJECTID,UUID", "OID_COPY,UUID" });
			var test = new QaUnique(new[] {table, table2}, new[] {"Uuid"});

			var runner = new QaTestRunner(test);
			runner.Execute();
		}

		[Test]
		public void CanCheckIntsMultiTable()
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("UUID",
			                                       esriFieldType.esriFieldTypeInteger));
			ITable table = TestWorkspaceUtils.CreateSimpleTable(_fgdbWorkspace, "CanCheckInts1",
			                                                    fields);

			fields.AddField(FieldUtils.CreateField("UUID2",
			                                       esriFieldType.esriFieldTypeInteger));
			ITable table2 = TestWorkspaceUtils.CreateSimpleTable(_fgdbWorkspace,
			                                                     "CanCheckInts2", fields);

			IRow row;
			for (var i = 0; i < 10; i++)
			{
				row = table.CreateRow();
				row.set_Value(1, i);
				row.Store();
			}

			row = table2.CreateRow();
			row.set_Value(2, 7);
			row.Store();

			//IWorkspace ws = TestDataUtils.OpenTopgisTlm();
			//ITable table = ((IFeatureWorkspace) ws).OpenTable("TOPGIS_TLM.TLM_STRASSE");
			var test = new QaUnique(new[] {table, table2}, new[] {"UUID", "UUID2"});

			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(2, runner.Errors.Count);
		}

		[Test]
		public void CanCheckIntsMultiPgdbTable()
		{
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateField("UUID",
			                                       esriFieldType.esriFieldTypeInteger));
			ITable table = TestWorkspaceUtils.CreateSimpleTable(_pgdbWorkspace,
			                                                    "CanCheckGuids1", fields);

			fields.AddField(FieldUtils.CreateField("UUID2",
			                                       esriFieldType.esriFieldTypeInteger));
			ITable table2 = TestWorkspaceUtils.CreateSimpleTable(_pgdbWorkspace,
			                                                     "CanCheckGuids2", fields);

			IRow row;
			for (var i = 0; i < 10; i++)
			{
				row = table.CreateRow();
				row.set_Value(1, i);
				row.Store();
			}

			row = table2.CreateRow();
			row.set_Value(2, 7);
			row.Store();

			//IWorkspace ws = TestDataUtils.OpenTopgisTlm();
			//ITable table = ((IFeatureWorkspace) ws).OpenTable("TOPGIS_TLM.TLM_STRASSE");
			var test = new QaUnique(new[] {table, table2}, new[] {"UUID", "UUID2"});

			var runner = new QaTestRunner(test);
			runner.Execute();
			Assert.AreEqual(2, runner.Errors.Count);
		}

		private static void CanDetectNtoMUnique([NotNull] IFeatureWorkspace workspace)
		{
			const string uniqueFieldName = "FIELD_UNIQUE";
			const string foreignKeyFieldName = "FOREIGN_KEY_FIELD";

			ITable tableOrig;
			{
				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateField(uniqueFieldName,
				                                       esriFieldType.esriFieldTypeInteger));
				ITable table = TestWorkspaceUtils.CreateSimpleTable(workspace, "RelateUnique1",
				                                                    fields,
				                                                    null);
				tableOrig = table;
			}
			ITable tableRel;
			{
				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateField(foreignKeyFieldName,
				                                       esriFieldType.esriFieldTypeInteger));
				ITable table = TestWorkspaceUtils.CreateSimpleTable(workspace, "RelateUnique2",
				                                                    fields,
				                                                    null);
				tableRel = table;
			}
			IRelationshipClass rel = TestWorkspaceUtils.CreateSimpleMNRelationship(
				workspace, "NToMRelTable", tableOrig, tableRel, uniqueFieldName,
				foreignKeyFieldName);

			{
				((IWorkspaceEdit) workspace).StartEditing(false);
				for (var i = 0; i < 10; i++)
				{
					IRow row = tableOrig.CreateRow();
					row.set_Value(1, i);
					row.Store();
				}

				for (var i = 0; i < 10; i++)
				{
					IRow row = tableRel.CreateRow();
					row.set_Value(1, i);
					row.Store();
					rel.CreateRelationship((IObject) tableOrig.GetRow(i + 1),
					                       (IObject) row);
				}

				((IWorkspaceEdit) workspace).StopEditing(true);
			}

			ITable relTab = TableJoinUtils.CreateQueryTable(rel, JoinType.InnerJoin);

			foreach (bool forceInMemoryTableSort in new[] {true, false})
			{
				var test = new QaUnique(relTab, "RelateUnique1." + uniqueFieldName)
				           {
					           ForceInMemoryTableSorting = forceInMemoryTableSort
				           };

				var runner = new QaTestRunner(test);
				runner.Execute();
				AssertUtils.NoError(runner);
			}
		}

		private static void CanDetect1ToNUnique([NotNull] IFeatureWorkspace workspace)
		{
			const string uniqueFieldName = "Unique";

			ITable tableOrig;
			{
				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateField(
					                uniqueFieldName, esriFieldType.esriFieldTypeInteger));
				ITable table = TestWorkspaceUtils.CreateSimpleTable(
					workspace, "Relate1NUnique1", fields);
				for (var i = 0; i < 10; i++)
				{
					IRow row = table.CreateRow();
					row.set_Value(1, i);
					row.Store();
				}

				tableOrig = table;
			}
			ITable tableRel;
			{
				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateField(
					                "Ref", esriFieldType.esriFieldTypeInteger));
				ITable table = TestWorkspaceUtils.CreateSimpleTable(
					workspace, "Relate1NUnique2", fields);
				for (var i = 0; i < 10; i++)
				{
					IRow row = table.CreateRow();
					row.set_Value(1, i);
					row.Store();
				}

				tableRel = table;
			}
			IRelationshipClass rel =
				TestWorkspaceUtils.CreateSimple1NRelationship(workspace,
				                                              "rel1NUnique", tableOrig,
				                                              tableRel, uniqueFieldName, "Ref");

			ITable relTab = TableJoinUtils.CreateQueryTable(rel, JoinType.InnerJoin);

			foreach (bool forceInMemoryTableSort in new[] {true, false})
			{
				var test = new QaUnique(relTab, "Relate1NUnique1." + uniqueFieldName)
				           {
					           ForceInMemoryTableSorting = forceInMemoryTableSort
				           };

				var runner = new QaTestRunner(test);
				runner.Execute();
				AssertUtils.NoError(runner);
			}
		}

		private static ITable CanDetect1toNNonUnique([NotNull] IFeatureWorkspace workspace)
		{
			ITable tableOrig;
			{
				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateField(
					                "Unique", esriFieldType.esriFieldTypeInteger));
				ITable table = TestWorkspaceUtils.CreateSimpleTable(
					workspace, "Relate1NNonUnique1", fields);

				for (var i = 0; i < 10; i++)
				{
					IRow row = table.CreateRow();
					row.set_Value(1, i);
					row.Store();
				}

				tableOrig = table;
			}
			ITable tableRel;
			{
				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateField(
					                "Ref", esriFieldType.esriFieldTypeInteger));
				ITable table = TestWorkspaceUtils.CreateSimpleTable(
					workspace, "Relate1NNonUnique2", fields);
				for (var i = 0; i < 10; i++)
				{
					IRow row = table.CreateRow();
					row.set_Value(1, i);
					row.Store();
				}

				{
					IRow row = table.CreateRow();
					row.set_Value(1, 5);
					row.Store();
				}
				{
					IRow row = table.CreateRow();
					row.set_Value(1, 5);
					row.Store();
				}
				{
					IRow row = table.CreateRow();
					row.set_Value(1, 7);
					row.Store();
				}
				tableRel = table;
			}
			IRelationshipClass rel = TestWorkspaceUtils.CreateSimple1NRelationship(
				workspace, "rel1NNonUnique", tableOrig, tableRel, "Unique", "Ref");

			ITable relTab = TableJoinUtils.CreateQueryTable(rel, JoinType.InnerJoin);

			foreach (bool forceInMemoryTableSort in new[] {true, false})
			{
				var test = new QaUnique(relTab, "Relate1NNonUnique1.Unique")
				           {
					           ForceInMemoryTableSorting = forceInMemoryTableSort
				           };

				test.SetRelatedTables(new[] {tableOrig, tableRel});

				var runner = new QaTestRunner(test);
				runner.Execute();
				Assert.AreEqual(5, runner.Errors.Count);
			}

			return relTab;
		}

		private static void CanDetectNtoMNonUnique([NotNull] IFeatureWorkspace ws)
		{
			const string uniqueFieldName = "FIELD_UNIQUE";
			const string foreignKeyFieldName = "FOREIGN_KEY_FIELD";
			const string origTableName = "OrigTable2";
			const string destTableName = "DestTable2";
			ITable originTable;
			{
				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateField(uniqueFieldName,
				                                       esriFieldType.esriFieldTypeInteger));
				ITable table = TestWorkspaceUtils.CreateSimpleTable(
					ws, origTableName, fields);
				originTable = table;
			}
			ITable destinationTable;
			{
				IFieldsEdit fields = new FieldsClass();
				fields.AddField(FieldUtils.CreateOIDField());
				fields.AddField(FieldUtils.CreateField(
					                foreignKeyFieldName, esriFieldType.esriFieldTypeInteger));
				ITable table = TestWorkspaceUtils.CreateSimpleTable(
					ws, destTableName, fields);

				destinationTable = table;
			}

			IRelationshipClass relClass = TestWorkspaceUtils.CreateSimpleMNRelationship(
				ws, "relNonUnique", originTable, destinationTable,
				"OrigFKey", "DestFKey");

			{
				// insert Data
				((IWorkspaceEdit) ws).StartEditing(false);
				for (var i = 0; i < 10; i++)
				{
					IRow row = originTable.CreateRow();
					row.set_Value(1, i);
					row.Store();
				}

				for (var i = 0; i < 10; i++)
				{
					IRow row = destinationTable.CreateRow();
					row.set_Value(1, i);
					row.Store();

					relClass.CreateRelationship((IObject) originTable.GetRow(i + 1),
					                            (IObject) row);
				}

				{
					IRow row = destinationTable.CreateRow();
					row.set_Value(1, 5);
					row.Store();
					relClass.CreateRelationship((IObject) originTable.GetRow(5 + 1),
					                            (IObject) row);
				}
				{
					IRow row = destinationTable.CreateRow();
					row.set_Value(1, 5);
					row.Store();
					relClass.CreateRelationship((IObject) originTable.GetRow(5 + 1),
					                            (IObject) row);
				}
				{
					IRow row = destinationTable.CreateRow();
					row.set_Value(1, 7);
					row.Store();
					relClass.CreateRelationship((IObject) originTable.GetRow(7 + 1),
					                            (IObject) row);
				}
				((IWorkspaceEdit) ws).StopEditing(true);
			}

			ITable relTab = TableJoinUtils.CreateQueryTable(relClass, JoinType.InnerJoin);

			foreach (bool forceInMemoryTableSort in new[] {true, false})
			{
				var test = new QaUnique(relTab, origTableName + "." + uniqueFieldName)
				           {
					           ForceInMemoryTableSorting = forceInMemoryTableSort
				           };

				test.SetRelatedTables(new[] {originTable, destinationTable});

				var runner = new QaTestRunner(test);
				runner.Execute();
				Assert.AreEqual(5, runner.Errors.Count);
			}
		}
	}
}