using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.Test
{
	public static class ProtoTestUtils
	{
		public static SchemaMsg CreateSchemaMsg(string gdbPath,
		                                        int modelId)
		{
			// Initial harvest:
			DdxModel model = HarvestModel(gdbPath);

			// Create SchemaMsg (client side):
			return CreateSchemaMsg(model, modelId);
		}

		public static DdxModel HarvestModel(string gdbPath,
		                                 bool harvestAttributes = false,
		                                 bool harvestObjectTypes = false)
		{
			IWorkspace workspace = WorkspaceUtils.OpenFileGdbWorkspace(gdbPath);

			VerifiedModelFactory modelFactory =
				new VerifiedModelFactory(new MasterDatabaseWorkspaceContextFactory(),
				                         new SimpleVerifiedDatasetHarvester())
				{
					HarvestAttributes = harvestAttributes,
					HarvestObjectTypes = harvestObjectTypes
				};

			DdxModel model = modelFactory.CreateModel(workspace, "TestModel", -100, null, null);
			modelFactory.AssignMostFrequentlyUsedSpatialReference(model, model.Datasets);
			return model;
		}

		public static SchemaMsg CreateSchemaMsg(DdxModel model, int modelId)
		{
			SchemaMsg schemaMsg = new SchemaMsg();
			foreach (Dataset dataset in model.GetDatasets())
			{
				// If persistent, use model id
				ObjectClassMsg objectClassMsg = ProtobufGdbUtils.ToObjectClassMsg(
					dataset, modelId, model.SpatialReferenceDescriptor.GetSpatialReference());

				schemaMsg.ClassDefinitions.Add(objectClassMsg);
			}

			return schemaMsg;
		}

		public static SchemaMsg CreateGdbSchemaMsg(IFeatureWorkspace workspace, int modelId)
		{
			SchemaMsg dataSchema = new SchemaMsg();

			foreach (IObjectClass objectClass in DatasetUtils.GetObjectClasses(
				         (IWorkspace) workspace))
			{
				var objectClassMsg =
					ProtobufGdbUtils.ToObjectClassMsg((ITable) objectClass,
					                                  objectClass.ObjectClassID,
					                                  true,
					                                  DatasetUtils.GetAliasName(objectClass));

				if (modelId >= 0)
					objectClassMsg.DdxModelId = modelId;

				dataSchema.ClassDefinitions.Add(objectClassMsg);
			}

			foreach (IRelationshipClass relationshipClass in
			         DatasetUtils.GetRelationshipClasses(workspace))
			{
				var relTableMsg = ProtobufGdbUtils.ToRelationshipClassMsg(relationshipClass);

				relTableMsg.Name = DatasetUtils.GetName(relationshipClass);

				if (modelId >= 0)
					relTableMsg.DdxModelId = modelId;

				dataSchema.RelclassDefinitions.Add(relTableMsg);
			}

			return dataSchema;
		}
	}
}
