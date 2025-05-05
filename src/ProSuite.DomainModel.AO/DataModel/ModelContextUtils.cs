using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public static class ModelContextUtils
	{
		[CanBeNull]
		public static IDdxDataset GetDataset(
			[NotNull] IDatasetName datasetName,
			bool isValid,
			[NotNull] IWorkspaceContext workspaceContext,
			[NotNull] Model model)
		{
			Assert.ArgumentNotNull(datasetName, nameof(datasetName));
			Assert.ArgumentNotNull(workspaceContext, nameof(workspaceContext));
			Assert.ArgumentNotNull(model, nameof(model));

			IWorkspace workspace = isValid
				                       ? WorkspaceUtils.OpenWorkspace(datasetName)
				                       : null;

			return GetDataset(datasetName.Name, workspace, workspaceContext, model,
			                  dataset => true);
		}

		public static bool HasMatchingGeometryType([NotNull] IObjectDataset objectDataset,
		                                           [NotNull] IObjectClass objectClass)
		{
			var featureClass = objectClass as IFeatureClass;
			if (featureClass == null)
			{
				return objectDataset.GeometryType is GeometryTypeNoGeometry;
			}

			var geometryTypeShape = objectDataset.GeometryType as GeometryTypeShape;

			return geometryTypeShape != null &&
			       geometryTypeShape.IsEqual(featureClass.ShapeType);
		}

		public static bool HasMatchingShapeType(IDdxDataset dataset,
		                                        esriGeometryType shapeType)
		{
			var geometryTypeShape = dataset.GeometryType as GeometryTypeShape;

			return geometryTypeShape != null &&
			       geometryTypeShape.IsEqual(shapeType);
		}

		public static bool IsModelDefaultDatabase([NotNull] IWorkspace workspace,
		                                          [NotNull] Model model)
		{
			return model.IsMasterDatabaseAccessible() &&
			       WorkspaceUtils.IsSameDatabase(model.GetMasterDatabaseWorkspace(),
			                                     workspace);
		}

		public static bool HasMatchingDatasetType([NotNull] IDdxDataset dataset,
		                                          [NotNull] IDatasetName datasetName)
		{
			var featureClassName = datasetName as IFeatureClassName;
			if (featureClassName != null)
			{
				return dataset is IVectorDataset &&
				       HasMatchingShapeType(dataset, DatasetUtils.GetShapeType(featureClassName));
			}

			var tableName = datasetName as ITableName;
			if (tableName != null)
			{
				return dataset is ITableDataset &&
				       dataset.GeometryType is GeometryTypeNoGeometry;
			}

			if (datasetName.Type == esriDatasetType.esriDTTopology)
			{
				return dataset is TopologyDataset &&
				       dataset.GeometryType is GeometryTypeTopology;
			}

			if (datasetName.Type == esriDatasetType.esriDTGeometricNetwork)
			{
				return dataset.GeometryType is GeometryTypeGeometricNetwork;
			}

			if (datasetName.Type == esriDatasetType.esriDTTerrain)
			{
				return dataset.GeometryType is GeometryTypeTerrain;
			}

			var mosaicDatasetName = datasetName as IMosaicDatasetName;
			if (mosaicDatasetName != null)
			{
				return dataset is RasterMosaicDataset &&
				       dataset.GeometryType is GeometryTypeRasterMosaic;
			}

			var rasterDatasetName = datasetName as IRasterDatasetName;
			if (rasterDatasetName != null)
			{
				return dataset is RasterDataset &&
				       dataset.GeometryType is GeometryTypeRasterDataset;
			}

			return false;
		}

		/// <summary>
		/// Tries to get the dataset for a given gdb dataset name and an optional workspace, from
		/// either the primary workspace context or the model master database.
		/// </summary>
		/// <param name="gdbDatasetName">Name of the GDB dataset.</param>
		/// <param name="workspace">The workspace.</param>
		/// <param name="workspaceContext">The primary workspace context.</param>
		/// <param name="model">The model.</param>
		/// <param name="isValidDataset">The optional predicate to evaluate the validity of a dataset.</param>
		/// <returns></returns>
		[CanBeNull]
		private static IDdxDataset GetDataset(
			[NotNull] string gdbDatasetName,
			[CanBeNull] IWorkspace workspace,
			[NotNull] IWorkspaceContext workspaceContext,
			[NotNull] Model model,
			[CanBeNull] Predicate<IDdxDataset> isValidDataset)
		{
			Assert.ArgumentNotNullOrEmpty(gdbDatasetName, nameof(gdbDatasetName));
			Assert.ArgumentNotNull(workspaceContext, nameof(workspaceContext));
			Assert.ArgumentNotNull(model, nameof(model));

			Dataset dataset = workspaceContext.GetDatasetByGdbName(gdbDatasetName);

			if (dataset != null)
			{
				if (isValidDataset == null || isValidDataset(dataset))
				{
					if (workspace == null)
					{
						// assume match, no further checks
						return dataset;
					}

					if (WorkspaceUtils.IsSameDatabase(workspace,
					                                  workspaceContext.Workspace))
					{
						return dataset;
					}
				}
			}

			string modelDatasetName = model.TranslateToModelElementName(gdbDatasetName);

			dataset = model.GetDatasetByModelName(modelDatasetName);

			if (dataset == null)
			{
				return null;
			}

			if (isValidDataset != null && ! isValidDataset(dataset))
			{
				return null;
			}

			if (workspace == null)
			{
				return dataset;
			}

			IWorkspace masterDbWorkspace = model.GetMasterDatabaseWorkspace();
			if (masterDbWorkspace == null)
			{
				// not accessible, but dataset workspace is not null --> assume different database 
				// (Note this could also be due to different credentials used for the connection, one valid and one not)
				return null;
			}

			return WorkspaceUtils.IsSameDatabase(masterDbWorkspace, workspace)
				       ? dataset
				       : null;
		}
	}
}
