using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Test;
using ProSuite.QA.TestFactories;
using ProSuite.QA.Tests.Test.TestRunners;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.DataModel.Harvesting;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaRelConstraintTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			_lic.Release();
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
			int pk = t.OID;
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
			QualityConditionParameterUtils.AddParameterValue(condition, "relationTables", vectorDataset);
			QualityConditionParameterUtils.AddParameterValue(condition, "relationTables", tableDataset);
			QualityConditionParameterUtils.AddParameterValue(condition, "relation", "rc");
			QualityConditionParameterUtils.AddParameterValue(condition, "join", JoinType.InnerJoin);
			QualityConditionParameterUtils.AddParameterValue(condition, "constraint",
			                                        "(fc.OBJECTID = 1 AND table.OBJECTID = 1) AND (table.TEXT = 'table')");

			var factory = new QaRelConstraint {Condition = condition};
			ITest test = factory.CreateTests(new SimpleDatasetOpener(childWorkspaceContext))[0];

			var runner = new QaContainerTestRunner(1000, test);
			runner.Execute(GeometryFactory.CreateEnvelope(0, 0, 1000, 1000));

			AssertUtils.NoError(runner);
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
