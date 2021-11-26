#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.QA
{
	public class SimpleDatasetOpener : IOpenDataset
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IDatasetContext _datasetContext;

		public SimpleDatasetOpener(IDatasetContext datasetContext)
		{
			_datasetContext = datasetContext;
		}

		public object OpenDataset(IDdxDataset dataset, Type knownType)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			if (knownType != null)
			{
				return OpenKnownDatasetType(dataset, knownType);
			}

			try
			{
				if (dataset is ObjectDataset objectDataset)
				{
					return _datasetContext.OpenObjectClass(objectDataset);
				}

				if (dataset is ISimpleTerrainDataset simpleTerrainDataset)
				{
					return _datasetContext.OpenTerrainReference(simpleTerrainDataset);
				}

				if (dataset is IRasterMosaicDataset simpleRasterMosaicDataset)
				{
					return _datasetContext.OpenSimpleRasterMosaic(simpleRasterMosaicDataset);
				}

				// TODO: Raster, Mosaic

				return null;
			}
			catch (Exception e)
			{
				_msg.VerboseDebug(() => $"Error opening dataset {dataset.Name}", e);
				return null;
			}
		}

		public IRelationshipClass OpenRelationshipClass(Association association)
		{
			return _datasetContext.OpenRelationshipClass(association);
		}

		private object OpenKnownDatasetType(IDdxDataset dataset, Type knownType)
		{
			Assert.ArgumentNotNull(knownType, nameof(knownType));

			if (typeof(IFeatureClass) == knownType)
				return _datasetContext.OpenFeatureClass((IVectorDataset) dataset);

			if (typeof(ITable) == knownType)
				return _datasetContext.OpenTable((IObjectDataset) dataset);

			if (typeof(IMosaicDataset) == knownType)
				return (IMosaicDataset) _datasetContext.OpenRasterDataset(
					(IDdxRasterDataset) dataset);

			if (typeof(IRasterDataset) == knownType)
				return _datasetContext.OpenRasterDataset((IDdxRasterDataset) dataset);

			if (typeof(IRasterDataset2) == knownType)
				return (IRasterDataset2) _datasetContext.OpenRasterDataset(
					(IDdxRasterDataset) dataset);

			if (typeof(SimpleRasterMosaic) == knownType)
				return _datasetContext.OpenSimpleRasterMosaic((IRasterMosaicDataset) dataset);

			if (typeof(TerrainReference) == knownType)
				return _datasetContext.OpenTerrainReference((ISimpleTerrainDataset) dataset);

			throw new ArgumentException($"Unsupported data type {knownType}");
		}
	}
}
