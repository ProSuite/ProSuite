using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class MasterDatabaseDatasetContext : IDatasetContext
	{
		public bool CanOpen(IDdxDataset dataset)
		{
			return ModelElementUtils.CanOpenFromMasterDatabase(dataset);
		}

		public IFeatureClass OpenFeatureClass(IVectorDataset dataset)
		{
			return ModelElementUtils.TryOpenFromMasterDatabase(dataset);
		}

		public ITable OpenTable(IObjectDataset dataset)
		{
			return (ITable) ModelElementUtils.TryOpenFromMasterDatabase(dataset);
		}

		public IObjectClass OpenObjectClass(IObjectDataset dataset)
		{
			return ModelElementUtils.TryOpenFromMasterDatabase(dataset);
		}

		public IRasterDataset OpenRasterDataset(IDdxRasterDataset dataset)
		{
			return ModelElementUtils.TryOpenFromMasterDatabase(dataset);
		}

		private Dictionary<ISimpleTerrainDataset, TerrainReference> _terrainDict;
		public TerrainReference OpenTerrainReference(ISimpleTerrainDataset dataset)
		{
			_terrainDict =
				_terrainDict ?? new Dictionary<ISimpleTerrainDataset, TerrainReference>();
			if (!_terrainDict.TryGetValue(dataset, out TerrainReference terrainRef))
			{
				terrainRef = ModelElementUtils.TryOpenFromMasterDatabase(dataset);
				_terrainDict.Add(dataset, terrainRef);
			}

			return terrainRef;
		}

		public SimpleRasterMosaic OpenSimpleRasterMosaic(ISimpleRasterMosaicDataset dataset)
		{
			return ModelElementUtils.TryOpenFromMasterDatabase(dataset);
		}

		public IRelationshipClass OpenRelationshipClass(Association association)
		{
			return ModelElementUtils.TryOpenFromMasterDatabase(association);
		}
	}
}
