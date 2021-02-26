using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class MasterDatabaseDatasetContext : IDatasetContext
	{
		public bool CanOpen(IDdxDataset dataset)
		{
			return ModelElementUtils.CanOpenFromMasterDatabase(dataset);
		}

		[CLSCompliant(false)]
		public IFeatureClass OpenFeatureClass(IVectorDataset dataset)
		{
			return ModelElementUtils.TryOpenFromMasterDatabase(dataset);
		}

		[CLSCompliant(false)]
		public ITable OpenTable(IObjectDataset dataset)
		{
			return (ITable) ModelElementUtils.TryOpenFromMasterDatabase(dataset);
		}

		[CLSCompliant(false)]
		public IObjectClass OpenObjectClass(IObjectDataset dataset)
		{
			return ModelElementUtils.TryOpenFromMasterDatabase(dataset);
		}

		[CLSCompliant(false)]
		public IRasterDataset OpenRasterDataset(IDdxRasterDataset dataset)
		{
			return ModelElementUtils.TryOpenFromMasterDatabase(dataset);
		}

		[CLSCompliant(false)]
		public TerrainReference OpenTerrainReference(ISimpleTerrainDataset dataset)
		{
			return ModelElementUtils.TryOpenFromMasterDatabase(dataset);
		}

		[CLSCompliant(false)]
		public IRelationshipClass OpenRelationshipClass(Association association)
		{
			return ModelElementUtils.TryOpenFromMasterDatabase(association);
		}
	}
}
