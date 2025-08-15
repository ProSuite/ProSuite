using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.DataModel.Harvesting;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class ProtoBasedModelFactory : IVerifiedModelFactory
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly SchemaMsg _schemaMsg;
		[NotNull] private readonly IMasterDatabaseWorkspaceContextFactory _workspaceContextFactory;

		public ProtoBasedModelFactory(
			[NotNull] SchemaMsg schemaMsg,
			[NotNull] IMasterDatabaseWorkspaceContextFactory workspaceContextFactory)
		{
			_schemaMsg = schemaMsg;
			_workspaceContextFactory = workspaceContextFactory;

			GeometryTypes = GeometryTypeFactory.CreateGeometryTypes().ToList();
		}

		[PublicAPI]
		public bool HarvestAttributes { get; set; }

		[PublicAPI]
		public bool HarvestObjectTypes { get; set; }

		private List<GeometryType> GeometryTypes { get; set; }

		#region Implementation of IVerifiedModelFactory

		public DdxModel CreateModel(IWorkspace workspace,
		                         string modelName,
		                         int modelId,
		                         string databaseName,
		                         string schemaOwner,
		                         IList<string> usedDatasetNames = null)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			Assert.ArgumentNotNullOrEmpty(modelName, nameof(modelName));

			VerifiedModel model =
				CreateVerifiedModel(modelName, workspace, modelId, databaseName, schemaOwner);

			using (_msg.IncrementIndentation("Re-creating datasets for '{0}'", modelName))
			{
				foreach (ObjectClassMsg objectClassMsg in _schemaMsg.ClassDefinitions)
				{
					// 0 is not a possible model ID in the DDX (sequence starts at 1) and means
					// it has not been initialized -> Allow datasets without model ID
					if (objectClassMsg.DdxModelId != 0 && objectClassMsg.DdxModelId != modelId)
					{
						continue;
					}

					if (usedDatasetNames?.Contains(objectClassMsg.Name) == false)
					{
						continue;
					}

					Dataset dataset = CreateDataset(objectClassMsg);

					if (dataset is ObjectDataset objectDataset)
					{
						HarvestChildren(objectDataset);
					}

					if (objectClassMsg.SpatialReference != null &&
					    model.SpatialReferenceDescriptor == null)
					{
						// Use any of the objectClassMsg -> they should have been assigned the model SR
						ISpatialReference sr = ProtobufGeometryUtils.FromSpatialReferenceMsg(
							objectClassMsg.SpatialReference);

						model.SpatialReferenceDescriptor =
							SpatialReferenceDescriptorExtensions.CreateFrom(sr);
					}

					model.AddDataset(dataset);
				}
			}

			_msg.InfoFormat("{0} dataset(s) read", model.Datasets.Count);

			return model;
		}

		public void AssignMostFrequentlyUsedSpatialReference(DdxModel model,
		                                                     IEnumerable<Dataset> usedDatasets)
		{
			ISpatialReference spatialReference = VerifiedModelFactory.GetMainSpatialReference(
				model, usedDatasets);

			if (spatialReference != null)
			{
				model.SpatialReferenceDescriptor =
					SpatialReferenceDescriptorExtensions.CreateFrom(spatialReference);
			}
		}

		#endregion

		private void HarvestChildren([NotNull] ObjectDataset dataset)
		{
			// TODO harvest some attribute roles selectively (mainly ObjectID, based on heuristics for what might be a suitable OID in case of non-SDE-geodatabases)

			if (HarvestAttributes)
			{
				AttributeHarvestingUtils.HarvestAttributes(dataset);
			}

			if (HarvestObjectTypes)
			{
				ObjectTypeHarvestingUtils.HarvestObjectTypes(dataset);
			}
		}

		private Dataset CreateDataset(ObjectClassMsg objectClassMsg)
		{
			var geometryType = (ProSuiteGeometryType) objectClassMsg.GeometryType;
			switch (geometryType)
			{
				case ProSuiteGeometryType.Null:
					return CreateTableDataset(objectClassMsg.Name);

				case ProSuiteGeometryType.Point:
				case ProSuiteGeometryType.Multipoint:
				case ProSuiteGeometryType.Polyline:
				case ProSuiteGeometryType.Polygon:
				case ProSuiteGeometryType.MultiPatch:
					return CreateVectorDataset(objectClassMsg.Name, geometryType);

				case ProSuiteGeometryType.Raster:
					return CreateRasterDataset(objectClassMsg.Name);

				case ProSuiteGeometryType.RasterMosaic:
					return CreateRasterMosaicDataset(objectClassMsg.Name);

				case ProSuiteGeometryType.Topology:
					return CreateTopologyDataset(objectClassMsg.Name);

				case ProSuiteGeometryType.Terrain:
				// TODO: Transfer the definition in the name? Add string-list with dependent datasets?
				// TODO: Same for XML
				//return CreateSimpleTerrainDataset(objectClassMsg.Name);

				default:

					// TODO: SimpleTerrain
					throw new ArgumentOutOfRangeException(
						$"Unsupported geometry type: {geometryType}");
			}
		}

		//private Dataset CreateSimpleTerrainDataset(string datasetName)
		//{
		//	return new VerifiedSimpleTerrain(datasetName)
		//	       {
		//		       GeometryType = GetGeometryType<GeometryTypeSimpleTerrain>()
		//	       };
		//}

		private Dataset CreateTableDataset(string datasetName)
		{
			VerifiedTableDataset dataset =
				new VerifiedTableDataset(datasetName)
				{
					GeometryType = GetGeometryType<GeometryTypeNoGeometry>()
				};

			return dataset;
		}

		private Dataset CreateVectorDataset(string datasetName,
		                                    ProSuiteGeometryType prosuiteGeometryType)
		{
			GeometryTypeShape geometryType = GetGeometryType(prosuiteGeometryType);

			var verifiedVectorDataset = new VerifiedVectorDataset(datasetName)
			                            {
				                            GeometryType = geometryType
			                            };

			return verifiedVectorDataset;
		}

		private Dataset CreateRasterMosaicDataset(string datasetName)
		{
			return new VerifiedRasterMosaicDataset(datasetName)
			       {
				       GeometryType = GetGeometryType<GeometryTypeRasterMosaic>()
			       };
		}

		private Dataset CreateRasterDataset(string datasetName)
		{
			return new VerifiedRasterDataset(datasetName)
			       {
				       GeometryType = GetGeometryType<GeometryTypeRasterDataset>()
			       };
		}

		private Dataset CreateTopologyDataset(string datasetName)
		{
			return new VerifiedTopologyDataset(datasetName)
			       {
				       GeometryType = GetGeometryType<GeometryTypeTopology>()
			       };
		}

		[CanBeNull]
		private T GetGeometryType<T>() where T : GeometryType
		{
			return GeometryTypes.OfType<T>()
			                    .FirstOrDefault();
		}

		[CanBeNull]
		private GeometryTypeShape GetGeometryType(ProSuiteGeometryType prosuiteGeometryType)
		{
			return GeometryTypes.OfType<GeometryTypeShape>()
			                    .FirstOrDefault(gt => gt.ShapeType == prosuiteGeometryType);
		}

		private VerifiedModel CreateVerifiedModel(string name, IWorkspace workspace, int modelId,
		                                          string databaseName, string schemaOwner)
		{
			// NOTE: The schema owner is ignored by harvesting (it's probably intentional).
			bool useQualitfiedDatasetNames = WorkspaceUtils.UsesQualifiedDatasetNames(workspace);

			if (! useQualitfiedDatasetNames)
			{
				databaseName = null;
				schemaOwner = null;
			}

			VerifiedModel model =
				new VerifiedModel(name, workspace, _workspaceContextFactory, databaseName,
				                  schemaOwner, SqlCaseSensitivity.SameAsDatabase,
				                  modelId);

			return model;
		}
	}
}
