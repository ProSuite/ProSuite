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
			if (datasetName.Type == esriDatasetType.esriDTFeatureClass)
			{
				return GetVectorDataset(datasetName);
			}

			if (datasetName.Type == esriDatasetType.esriDTTable)
			{
				return GetTableDataset(datasetName);
			}

			if (datasetName.Type == esriDatasetType.esriDTTopology)
			{
				return GetTopologyDataset(datasetName);
			}

			if (datasetName.Type == esriDatasetType.esriDTMosaicDataset)
			{
				return GetRasterMosaicDataset(datasetName);
			}

			return null;
		}
	}
}
