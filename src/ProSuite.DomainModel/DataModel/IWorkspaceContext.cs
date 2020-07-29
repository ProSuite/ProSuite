using ArcGIS.Core.Data;

namespace ProSuite.DomainModel.DataModel
{
	public interface IWorkspaceContext : IDatasetContext
	{
		FeatureClass OpenFeatureClass(IVectorDataset dataset);

		Table OpenTable(IObjectDataset dataset);
	}
}
