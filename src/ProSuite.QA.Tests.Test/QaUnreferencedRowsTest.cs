using System;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.Test;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaUnreferencedRowsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);

			_testWs = TestWorkspaceUtils.CreateTestFgdbWorkspace("TestUnreferencedRows");
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void TestReferenced()
		{
			TestReferenced(_testWs);
		}

		private static void TestReferenced(IFeatureWorkspace ws)
		{
			IFieldsEdit fields1 = new FieldsClass();
			fields1.AddField(FieldUtils.CreateOIDField());
			fields1.AddField(FieldUtils.CreateSmallIntegerField("Pk"));

			ITable tbl1 = DatasetUtils.CreateTable(ws, "TestReferenced1", null, fields1);

			IFieldsEdit fields2 = new FieldsClass();
			fields2.AddField(FieldUtils.CreateOIDField());
			fields2.AddField(FieldUtils.CreateSmallIntegerField("Fk"));

			ITable tbl2 = DatasetUtils.CreateTable(ws, "TestReferenced2", null, fields2);

			IFieldsEdit fields3 = new FieldsClass();
			fields3.AddField(FieldUtils.CreateOIDField());
			fields3.AddField(FieldUtils.CreateIntegerField("Fk"));

			ITable tbl3 = DatasetUtils.CreateTable(ws, "TestReferenced3", null, fields3);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			CreateRow(tbl1, 1);
			CreateRow(tbl1, 2);
			CreateRow(tbl1, 3);
			CreateRow(tbl1, 4);
			CreateRow(tbl1, 5);

			CreateRow(tbl2, 1);
			CreateRow(tbl2, 3);
			CreateRow(tbl2, 5);

			CreateRow(tbl3, 2);
			CreateRow(tbl3, 4);

			var test = new QaUnreferencedRows(
				ReadOnlyTableFactory.Create(tbl1),
				new[] { ReadOnlyTableFactory.Create(tbl2), ReadOnlyTableFactory.Create(tbl3) },
				new[] { "pk,fk", "pk,fk" });

			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(0, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		[Test]
		public void TestReferencedGuid()
		{
			TestReferencedGuid(_testWs);
		}

		private static void TestReferencedGuid(IFeatureWorkspace ws)
		{
			IFieldsEdit fields1 = new FieldsClass();
			fields1.AddField(FieldUtils.CreateOIDField());
			fields1.AddField(FieldUtils.CreateField("Pk", esriFieldType.esriFieldTypeGUID));

			ITable tbl1 = DatasetUtils.CreateTable(ws, "TestReferencedGuid1", null, fields1);

			IFieldsEdit fields2 = new FieldsClass();
			fields2.AddField(FieldUtils.CreateOIDField());
			fields2.AddField(FieldUtils.CreateTextField("Fk", 50));

			ITable tbl2 = DatasetUtils.CreateTable(ws, "TestReferencedGuid2", null, fields2);

			IFieldsEdit fields3 = new FieldsClass();
			fields3.AddField(FieldUtils.CreateOIDField());
			fields3.AddField(FieldUtils.CreateField("Fk", esriFieldType.esriFieldTypeGUID));

			ITable tbl3 = DatasetUtils.CreateTable(ws, "TestReferencedGuid3", null, fields3);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			for (int i = 0; i < 5; i++)
			{
				Guid guid = Guid.NewGuid();
				CreateRow(tbl1, guid.ToString("B"));

				if (i % 2 == 0)
				{
					CreateRow(tbl2, guid.ToString());
				}
				else
				{
					CreateRow(tbl3, guid.ToString("B"));
				}
			}

			var test = new QaUnreferencedRows(
				ReadOnlyTableFactory.Create(tbl1),
				new[] { ReadOnlyTableFactory.Create(tbl2), ReadOnlyTableFactory.Create(tbl3) },
				new[] { "pk,fk", "pk,fk" });

			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(0, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(0, container.Errors.Count);
		}

		[Test]
		public void TestNotReferenced()
		{
			TestNotReferenced(_testWs);
		}

		private static void TestNotReferenced(IFeatureWorkspace ws)
		{
			IFieldsEdit fields1 = new FieldsClass();
			fields1.AddField(FieldUtils.CreateOIDField());
			fields1.AddField(FieldUtils.CreateSmallIntegerField("Pk"));

			ITable tbl1 = DatasetUtils.CreateTable(ws, "TestNotReferenced1", null, fields1);

			IFieldsEdit fields2 = new FieldsClass();
			fields2.AddField(FieldUtils.CreateOIDField());
			fields2.AddField(FieldUtils.CreateIntegerField("Fk"));

			ITable tbl2 = DatasetUtils.CreateTable(ws, "TestNotReferenced2", null, fields2);

			IFieldsEdit fields3 = new FieldsClass();
			fields3.AddField(FieldUtils.CreateOIDField());
			fields3.AddField(FieldUtils.CreateIntegerField("Fk"));

			ITable tbl3 = DatasetUtils.CreateTable(ws, "TestNotReferenced3", null, fields3);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) ws).StartEditing(false);
			((IWorkspaceEdit) ws).StopEditing(true);

			CreateRow(tbl1, 1);
			CreateRow(tbl1, 2);
			CreateRow(tbl1, 3);
			CreateRow(tbl1, 4);
			CreateRow(tbl1, 5);

			CreateRow(tbl2, 1);
			//CreateRow(tbl2, 3);
			CreateRow(tbl2, 5);

			CreateRow(tbl3, 2);
			CreateRow(tbl3, 4);

			var test = new QaUnreferencedRows(
				ReadOnlyTableFactory.Create(tbl1),
				new[] { ReadOnlyTableFactory.Create(tbl2), ReadOnlyTableFactory.Create(tbl3) },
				new[] { "pk,fk", "pk,fk" });

			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(1, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(1, container.Errors.Count);
		}

		[Test]
		public void TestNMRelation()
		{
			TestNMRelation(_testWs);
		}

		private static void TestNMRelation(IFeatureWorkspace ws)
		{
			IFieldsEdit fields1 = new FieldsClass();
			fields1.AddField(FieldUtils.CreateOIDField());
			fields1.AddField(FieldUtils.CreateIntegerField("Pk"));

			ITable tbl1 = DatasetUtils.CreateTable(ws, "TestNMRelation1", null, fields1);

			IFieldsEdit fields2 = new FieldsClass();
			fields2.AddField(FieldUtils.CreateOIDField());
			fields2.AddField(FieldUtils.CreateIntegerField("Fk"));

			ITable tbl2 = DatasetUtils.CreateTable(ws, "TestNMRelation2", null, fields2);

			IRelationshipClass rel = CreateSimpleMNRelationship(ws, "TestNMRelationRel", tbl1,
			                                                    tbl2, "Pk", "Fk");

			((IWorkspaceEdit) ws).StartEditing(false);
			IRow r11 = CreateRow(tbl1, 8);
			IRow r12 = CreateRow(tbl1, 12);
			IRow r13 = CreateRow(tbl1, 7);

			IRow r21 = CreateRow(tbl2, 9);
			IRow r22 = CreateRow(tbl2, 5);
			IRow r23 = CreateRow(tbl2, 4);

			Assert.NotNull(r12); // not used otherwise

			rel.CreateRelationship((IObject) r11, (IObject) r21);
			rel.CreateRelationship((IObject) r11, (IObject) r23);
			rel.CreateRelationship((IObject) r13, (IObject) r22);

			r22.set_Value(1, 6);
			r22.Store();

			((IWorkspaceEdit) ws).StopEditing(true);

			var test = new QaUnreferencedRows(
				ReadOnlyTableFactory.Create(tbl1),
				new[] { ReadOnlyTableFactory.Create(tbl2) },
				new[] { "pk,pk,TestNMRelationRel,fk,fk" });

			using (var r = new QaTestRunner(test))
			{
				r.Execute();
				Assert.AreEqual(2, r.Errors.Count);
			}

			var container = new QaContainerTestRunner(10000, test);
			container.Execute();
			Assert.AreEqual(2, container.Errors.Count);
		}

		[NotNull]
		private static IRelationshipClass CreateSimpleMNRelationship(
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] string name,
			[NotNull] ITable tableOrig,
			[NotNull] ITable tableRel,
			[NotNull] string orig,
			[NotNull] string dest)
		{
			IRelationshipClass rel =
				workspace.CreateRelationshipClass(
					name,
					(IObjectClass) tableOrig, (IObjectClass) tableRel,
					"forLabel", "backLabel",
					esriRelCardinality.esriRelCardinalityManyToMany,
					esriRelNotification.esriRelNotificationBoth, false, false, null,
					orig, dest, orig, dest);
			// make sure the table is known by the workspace
			((IWorkspaceEdit) workspace).StartEditing(false);
			((IWorkspaceEdit) workspace).StopEditing(true);
			return rel;
		}

		private static IRow CreateRow(ITable tbl, params object[] values)
		{
			IRow row = tbl.CreateRow();
			for (int i = 0; i < values.Length; i++)
			{
				row.set_Value(i + 1, values[i]);
			}

			row.Store();
			return row;
		}
	}
}
