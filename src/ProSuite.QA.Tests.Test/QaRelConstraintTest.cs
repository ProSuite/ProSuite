using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.DataModel.Harvesting;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;
using ProSuite.QA.TestFactories;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaRelConstraintTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(activateAdvancedLicense: true);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanTranslateSql()
		{
			IFeatureClass fc;
			ITable table;
			IRelationshipClass rc;
			CreateTestWorkspace(
				"CanTranslateSql_master", "fc", "table", "rc",
				out fc, out table, out rc);

			IFeatureClass fc_child;
			ITable table_child;
			IRelationshipClass rc_child;
			IFeatureWorkspace childWorkspace = CreateTestWorkspace(
				"CanTranslateSql_child", "fc_child", "table_child", "rc_child",
				out fc_child, out table_child, out rc_child);

			IRow t = table_child.CreateRow();
			t.Value[table_child.FindField("TEXT")] = "table"; // same as table name
			long pk = t.OID;
			t.Store();

			IFeature f = fc_child.CreateFeature();
			f.Value[fc_child.FindField("FKEY")] = pk;
			f.Shape = GeometryFactory.CreatePoint(100, 200);
			f.Store();

			var model = new SimpleModel("model", fc);

			ModelVectorDataset vectorDataset = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(fc)));
			ModelTableDataset tableDataset = model.AddDataset(
				new ModelTableDataset(DatasetUtils.GetName(table)));

			AttributeHarvestingUtils.HarvestAttributes(
				vectorDataset, ModelElementUtils.GetMasterDatabaseWorkspaceContext(vectorDataset));
			AttributeHarvestingUtils.HarvestAttributes(
				tableDataset, ModelElementUtils.GetMasterDatabaseWorkspaceContext(tableDataset));

			ObjectAttribute fkAttribute = vectorDataset.GetAttribute("FKEY");
			ObjectAttribute pkAttribute = tableDataset.GetAttribute(table.OIDFieldName);
			Assert.NotNull(fkAttribute);
			Assert.NotNull(pkAttribute);

			Association association =
				model.AddAssociation(new ForeignKeyAssociation(DatasetUtils.GetName(rc),
				                                               AssociationCardinality.OneToMany,
				                                               fkAttribute, pkAttribute));

			var childWorkspaceContext = new SimpleWorkspaceContext(
				model, childWorkspace,
				new[]
				{
					new WorkspaceDataset("fc_child", null, vectorDataset),
					new WorkspaceDataset("table_child", null, tableDataset),
				},
				new[]
				{
					new WorkspaceAssociation("rc_child", null, association)
				});

			var clsDesc = new ClassDescriptor(typeof(QaRelConstraint));
			var tstDesc = new TestDescriptor("RelConstraint", clsDesc);
			QualityCondition condition = new QualityCondition("fc_table_constraints", tstDesc);
			InstanceConfigurationUtils.AddParameterValue(
				condition, "relationTables", vectorDataset);
			InstanceConfigurationUtils.AddParameterValue(
				condition, "relationTables", tableDataset);
			InstanceConfigurationUtils.AddParameterValue(condition, "relation", "rc");
			InstanceConfigurationUtils.AddParameterValue(condition, "join", JoinType.InnerJoin);
			InstanceConfigurationUtils.AddParameterValue(condition, "constraint",
			                                             "(fc.OBJECTID = 1 AND table.OBJECTID = 1) AND (table.TEXT = 'table')");

			var factory = new QaRelConstraint { Condition = condition };
			ITest test = factory.CreateTests(new SimpleDatasetOpener(childWorkspaceContext))[0];

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute(GeometryFactory.CreateEnvelope(0, 0, 1000, 1000));

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanApplyFilterExpression()
		{
			IFeatureClass fc;
			ITable table;
			IRelationshipClass rc;
			CreateTestWorkspace(
				"CanApplyFilterExpression_master", "fc", "table", "rc",
				out fc, out table, out rc);

			IFeatureClass fc_child;
			ITable table_child;
			IRelationshipClass rc_child;
			IFeatureWorkspace childWorkspace = CreateTestWorkspace(
				"CanApplyFilterExpression_child", "fc_child", "table_child", "rc_child",
				out fc_child, out table_child, out rc_child);

			IRow t = table_child.CreateRow();
			t.Value[table_child.FindField("TEXT")] = "table"; // same as table name
			long pk = t.OID;
			t.Store();

			IFeature f = fc_child.CreateFeature();
			f.Value[fc_child.FindField("FKEY")] = pk;
			f.Shape = GeometryFactory.CreatePoint(100, 200);
			f.Store();

			var model = new SimpleModel("model", fc);

			ModelVectorDataset vectorDataset = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(fc)));
			ModelTableDataset tableDataset = model.AddDataset(
				new ModelTableDataset(DatasetUtils.GetName(table)));

			AttributeHarvestingUtils.HarvestAttributes(
				vectorDataset, ModelElementUtils.GetMasterDatabaseWorkspaceContext(vectorDataset));
			AttributeHarvestingUtils.HarvestAttributes(
				tableDataset, ModelElementUtils.GetMasterDatabaseWorkspaceContext(tableDataset));

			ObjectAttribute fkAttribute = vectorDataset.GetAttribute("FKEY");
			ObjectAttribute pkAttribute = tableDataset.GetAttribute(table.OIDFieldName);
			Assert.NotNull(fkAttribute);
			Assert.NotNull(pkAttribute);

			Association association =
				model.AddAssociation(new ForeignKeyAssociation(DatasetUtils.GetName(rc),
				                                               AssociationCardinality.OneToMany,
				                                               fkAttribute, pkAttribute));

			var childWorkspaceContext = new SimpleWorkspaceContext(
				model, childWorkspace,
				new[]
				{
					new WorkspaceDataset("fc_child", null, vectorDataset),
					new WorkspaceDataset("table_child", null, tableDataset),
				},
				new[]
				{
					new WorkspaceAssociation("rc_child", null, association)
				});

			var clsDesc = new ClassDescriptor(typeof(QaRelConstraint));
			var tstDesc = new TestDescriptor("RelConstraint", clsDesc);
			QualityCondition condition = new QualityCondition("fc_table_constraints", tstDesc);
			InstanceConfigurationUtils.AddParameterValue(
				condition, "relationTables", vectorDataset, filterExpression: "TEXT = 'table'");
			InstanceConfigurationUtils.AddParameterValue(
				condition, "relationTables", tableDataset);
			InstanceConfigurationUtils.AddParameterValue(condition, "relation", "rc");
			InstanceConfigurationUtils.AddParameterValue(condition, "join", JoinType.InnerJoin);
			InstanceConfigurationUtils.AddParameterValue(condition, "constraint",
			                                             "(fc.OBJECTID = 1 AND table.OBJECTID = 1) AND (table.TEXT = 'table')");

			var factory = new QaRelConstraint { Condition = condition };
			ITest test = factory.CreateTests(new SimpleDatasetOpener(childWorkspaceContext))[0];

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute(GeometryFactory.CreateEnvelope(0, 0, 1000, 1000));

			AssertUtils.NoError(runner);
		}

		[Test]
		public void CanApplyFilterExpressionFgdbLeftJoin_Gen3538()
		{
			IFeatureClass fc;
			ITable table;
			IRelationshipClass rc;
			CreateTestWorkspace(
				"CanApplyFilterExpressionFgdbLeftJoin", "fc", "table", "rc",
				out fc, out table, out rc);

			IFeatureClass fc_child;
			ITable table_child;
			IRelationshipClass rc_child;
			IFeatureWorkspace childWorkspace = CreateTestWorkspace(
				"CanApplyFilterExpressionFgdbLeftJoin_child", "fc_child", "table_child", "rc_child",
				out fc_child, out table_child, out rc_child);

			IRow t1 = table_child.CreateRow();
			t1.Value[table_child.FindField("TEXT")] = "table"; // same as table name
			long pk = t1.OID;
			t1.Store();

			IFeature f1 = fc_child.CreateFeature();
			f1.Value[fc_child.FindField("FKEY")] = pk;
			f1.Shape = GeometryFactory.CreatePoint(100, 200);
			f1.Store();

			// Second (left) feature with no corresponding table record
			IFeature f2 = fc_child.CreateFeature();
			f2.Shape = GeometryFactory.CreatePoint(101, 201);
			f2.Store();

			var model = new SimpleModel("model", fc);

			ModelVectorDataset vectorDataset = model.AddDataset(
				new ModelVectorDataset(DatasetUtils.GetName(fc)));
			ModelTableDataset tableDataset = model.AddDataset(
				new ModelTableDataset(DatasetUtils.GetName(table)));

			AttributeHarvestingUtils.HarvestAttributes(
				vectorDataset, ModelElementUtils.GetMasterDatabaseWorkspaceContext(vectorDataset));
			AttributeHarvestingUtils.HarvestAttributes(
				tableDataset, ModelElementUtils.GetMasterDatabaseWorkspaceContext(tableDataset));

			ObjectAttribute fkAttribute = vectorDataset.GetAttribute("FKEY");
			ObjectAttribute pkAttribute = tableDataset.GetAttribute(table.OIDFieldName);
			Assert.NotNull(fkAttribute);
			Assert.NotNull(pkAttribute);

			Association association =
				model.AddAssociation(new ForeignKeyAssociation(DatasetUtils.GetName(rc),
				                                               AssociationCardinality.OneToMany,
				                                               fkAttribute, pkAttribute));

			var childWorkspaceContext = new SimpleWorkspaceContext(
				model, childWorkspace,
				new[]
				{
					new WorkspaceDataset("fc_child", null, vectorDataset),
					new WorkspaceDataset("table_child", null, tableDataset),
				},
				new[]
				{
					new WorkspaceAssociation("rc_child", null, association)
				});

			var clsDesc = new ClassDescriptor(typeof(QaRelConstraint));
			var tstDesc = new TestDescriptor("RelConstraint", clsDesc);
			QualityCondition condition = new QualityCondition("fc_table_constraints", tstDesc);

			// NOTE: The order of the table parameters seems to matter w.r.t left/right joinType.
			InstanceConfigurationUtils.AddParameterValue(
				condition, "relationTables", vectorDataset);
			InstanceConfigurationUtils.AddParameterValue(
				condition, "relationTables", tableDataset);

			InstanceConfigurationUtils.AddParameterValue(condition, "relation", "rc");
			InstanceConfigurationUtils.AddParameterValue(condition, "join", JoinType.LeftJoin);
			InstanceConfigurationUtils.AddParameterValue(condition, "constraint",
			                                             "(table_child.TEXT is not NULL)");

			var factory = new QaRelConstraint { Condition = condition };
			ITest test = factory.CreateTests(new SimpleDatasetOpener(childWorkspaceContext))[0];

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute(GeometryFactory.CreateEnvelope(0, 0, 1000, 1000));

			Assert.AreEqual(1, runner.Errors.Count);

			IList<InvolvedRow> involvedRows = runner.Errors[0].InvolvedRows;
			Assert.AreEqual(1, involvedRows.Count);

			InvolvedRow involvedRow = involvedRows[0];
			Assert.AreEqual(f2.OID, involvedRow.OID);
			Assert.AreEqual(DatasetUtils.GetName(f2.Table), involvedRow.TableName);
		}

		[NotNull]
		private static IFeatureWorkspace CreateTestWorkspace(
			[NotNull] string fgdbName,
			[NotNull] string fcName, [NotNull] string tableName, [NotNull] string relClassName,
			[NotNull] out IFeatureClass fc,
			[NotNull] out ITable table,
			[NotNull] out IRelationshipClass rc)
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateTestFgdbWorkspace(fgdbName);

			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			fc = DatasetUtils.CreateSimpleFeatureClass(
				ws, fcName, null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField("FKEY"),
				FieldUtils.CreateShapeField(
					"SHAPE", esriGeometryType.esriGeometryPoint,
					sref, 1000));

			table = DatasetUtils.CreateTable(ws, tableName,
			                                 FieldUtils.CreateOIDField(),
			                                 FieldUtils.CreateTextField("TEXT", 100));

			rc = TestWorkspaceUtils.CreateSimple1NRelationship(
				ws, relClassName,
				table, (ITable) fc,
				table.OIDFieldName, "FKEY");

			return ws;
		}
	}
}
