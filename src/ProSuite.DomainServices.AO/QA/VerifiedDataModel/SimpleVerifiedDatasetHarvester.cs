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
				datasetName.Type == esriDatasetType.esriDTTable;

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

			return null;
		}
	}
}
