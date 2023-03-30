using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Server.AO.QA;

namespace ProSuite.Microservices.Server.AO.Test
{
	[TestFixture]
	public class ProtoBasedModelFactoryTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.ConfigureUnitTestLogging();
			TestUtils.InitializeLicense();
		}

		// TODO: From Sde, with special datasets (Topology, RasterMosaic)

		[Test]
		public void CanCreateDdxModelFromSchemaMsg()
		{
			string gdbPath = TestData.GetGdb1Path();

			int modelId = 23;
			DdxModel model = CreateModel(gdbPath, modelId);

			Assert.AreEqual(3, model.Datasets.Count);

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

		private static Model CreateModel(string gdbPath,
		                                 int modelId)
		{
			SchemaMsg schemaMsg = ProtoTestUtils.CreateSchemaMsg(gdbPath, modelId);

			Model rehydratedModel = CreateModel(schemaMsg);

			return rehydratedModel;
		}

		private static Model CreateModel(SchemaMsg schemaMsg)
		{
			IVerifiedModelFactory modelFactory =
				new ProtoBasedModelFactory(schemaMsg, new MasterDatabaseWorkspaceContextFactory());

			IList<GdbWorkspace> gdbWorkspaces =
				ProtobufConversionUtils.CreateSchema(schemaMsg.ClassDefinitions);

			Assert.AreEqual(1, gdbWorkspaces.Count);
			var workspace = gdbWorkspaces[0];

			string modelName = "gdb1";

			Assert.NotNull(workspace.WorkspaceHandle);

			int modelId = Convert.ToInt32(workspace.WorkspaceHandle.Value);
			var model = modelFactory.CreateModel(workspace, modelName, modelId,
			                                     null, null);

			return model;
		}
	}
}
