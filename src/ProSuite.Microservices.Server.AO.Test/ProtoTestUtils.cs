using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Server.AO.Test
{
	public static class ProtoTestUtils
	{
		public static SchemaMsg CreateSchemaMsg(string gdbPath,
		                                        int modelId)
		{
			// Initial harvest:
			IWorkspace workspace = WorkspaceUtils.OpenFileGdbWorkspace(gdbPath);

			VerifiedModelFactory modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester())
				{
					HarvestAttributes = false,
					HarvestObjectTypes = false
				};

			Model model = modelFactory.CreateModel(workspace, "TestModel", -100, null, null);
			modelFactory.AssignMostFrequentlyUsedSpatialReference(model, model.Datasets);

			// Create SchemaMsg (client side):
			SchemaMsg schemaMsg = new SchemaMsg();
			foreach (Dataset dataset in model.GetDatasets())
			{
				// If persistent, use model id

				ObjectClassMsg objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(
					dataset, modelId, model.SpatialReferenceDescriptor.SpatialReference);

				schemaMsg.ClassDefinitions.Add(objectClassMsg);
			}

			return schemaMsg;
		}
	}
}
