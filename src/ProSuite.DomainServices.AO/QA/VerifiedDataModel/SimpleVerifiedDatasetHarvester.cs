using ESRI.ArcGIS.Geodatabase;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.QA.VerifiedDataModel
{
	public class SimpleVerifiedDatasetHarvester : VerifiedDatasetHarvesterBase
	{
		public override bool IgnoreDataset(IDatasetName datasetName, out string reason)
		{
			reason = string.Empty;

			bool isSupportedType =
				datasetName.Type == esriDatasetType.esriDTFeatureClass ||
				datasetName.Type == esriDatasetType.esriDTTable ||
				datasetName.Type == esriDatasetType.esriDTMosaicDataset;

			if (isSupportedType)
			{
				return false;
			}

			reason = $"Unsupported dataset type ({datasetName.Type})";

			return true;
		}

		protected override Dataset CreateDataset(IDatasetName datasetName)
		{
			if (datasetName is IFeatureClassName)
			{
				return GetVectorDataset(datasetName);
			}

			if (datasetName is ITableName)
			{
				return GetTableDataset(datasetName);
			}

			if (datasetName is ITopologyName)
			{
				return GetTopologyDataset(datasetName);
			}

			if (datasetName is IMosaicDatasetName)
			{
				return GetRasterMosaicDataset(datasetName);
			}

			return null;
		}
	}
}
