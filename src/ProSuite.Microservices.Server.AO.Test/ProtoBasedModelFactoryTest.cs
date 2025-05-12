using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Testing;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Server.AO.QA;
using TestUtils = ProSuite.Commons.Test.Testing.TestUtils;

namespace ProSuite.Microservices.Server.AO.Test
{
	[TestFixture]
	public class ProtoBasedModelFactoryTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.ConfigureUnitTestLogging();
			Commons.AO.Test.TestUtils.InitializeLicense();
		}

		// TODO: From Sde, with special datasets (Topology, RasterMosaic)

		[Test]
		public void CanCreateDdxModelFromSchemaMsg()
		{
			string gdbPath = TestData.GetGdb1Path();

			int modelId = 23;
			DdxModel model = CreateModel(gdbPath, modelId);

			Assert.AreEqual(4, model.Datasets.Count);

			Dataset pointDataset = model.Datasets.Single(
				d => d.GeometryType is GeometryTypeShape shapeType &&
				     shapeType.ShapeType == ProSuiteGeometryType.Point);

			Assert.AreEqual("points", pointDataset.Name);

			// All entities have id -1 as they are not persistent
			// Consider CloneId pattern used in QualityCondition for all rehydrated entities?
			Assert.AreEqual(-1, pointDataset.Id);
			Assert.AreEqual(modelId, pointDataset.Model.Id);

			Dataset lineDataset = model.Datasets.Single(
				d => d.GeometryType is GeometryTypeShape shapeType &&
				     shapeType.ShapeType == ProSuiteGeometryType.Polyline);

			Assert.AreEqual("lines", lineDataset.Name);
			Assert.AreEqual(modelId, lineDataset.Model.Id);

			Dataset polyDataset = model.Datasets.Single(
				d => d.GeometryType is GeometryTypeShape shapeType &&
				     shapeType.ShapeType == ProSuiteGeometryType.Polygon);

			Assert.AreEqual("polygons", polyDataset.Name);
			Assert.AreEqual(modelId, polyDataset.Model.Id);
		}

		[Test]
		public void CanCreateWorkspaceWithRelClassesFromSchemaMsg()
		{
			string gdbPath = TestDataPreparer
			                 .ExtractZip("TableJoinUtilsTest.gdb.zip",
			                             @"..\ProSuite.Commons.AO.Test\TestData")
			                 .GetPath();

			int modelId = 23;

			var workspace = (IFeatureWorkspace) WorkspaceUtils.OpenFileGdbWorkspace(gdbPath);

			// Create SchemaMsg (client side):
			SchemaMsg schemaMsg = ProtoTestUtils.CreateGdbSchemaMsg(workspace, modelId);

			GdbWorkspace gdbWorkspace = CreateGdbSchema(schemaMsg).First();

			Assert.AreEqual(4, gdbWorkspace.GetDatasets().Count());

			IFeatureClass pointFeatureClass = gdbWorkspace.OpenFeatureClass("Points");

			Assert.AreEqual("Points", DatasetUtils.GetName(pointFeatureClass));

			// ObjectClasses get a process-wide unique ID assigned.
			Assert.Greater(pointFeatureClass.FeatureClassID, 0);

			IFeatureClass streetFeatureClass = gdbWorkspace.OpenFeatureClass("Streets");

			Assert.AreEqual("Streets", DatasetUtils.GetName(streetFeatureClass));
			Assert.Greater(streetFeatureClass.FeatureClassID, 0);

			// Now open the m:n relationship class as table:
			ITable relationshipTable = gdbWorkspace.OpenTable("Rel_Streets_Routes");
			Assert.Greater(relationshipTable.Fields.FindField("STREET_OID"), 0);
			Assert.Greater(relationshipTable.Fields.FindField("ROUTE_OID"), 0);
		}

		private static DdxModel CreateModel(string gdbPath,
		                                 int modelId)
		{
			SchemaMsg schemaMsg = ProtoTestUtils.CreateSchemaMsg(gdbPath, modelId);

			DdxModel rehydratedModel = CreateModel(schemaMsg);

			return rehydratedModel;
		}

		private static DdxModel CreateModel(SchemaMsg schemaMsg)
		{
			IVerifiedModelFactory modelFactory =
				new ProtoBasedModelFactory(schemaMsg, new MasterDatabaseWorkspaceContextFactory());

			IList<GdbWorkspace> gdbWorkspaces = CreateGdbSchema(schemaMsg);

			Assert.AreEqual(1, gdbWorkspaces.Count);
			var workspace = gdbWorkspaces[0];

			string modelName = "gdb1";

			Assert.NotNull(workspace.WorkspaceHandle);

			int modelId = Convert.ToInt32(workspace.WorkspaceHandle.Value);
			var model = modelFactory.CreateModel(workspace, modelName, modelId,
			                                     null, null);

			return model;
		}

		private static IList<GdbWorkspace> CreateGdbSchema(SchemaMsg schemaMsg)
		{
			return ProtobufConversionUtils.CreateSchema(
				schemaMsg.ClassDefinitions,
				schemaMsg.RelclassDefinitions);
		}
	}
}
