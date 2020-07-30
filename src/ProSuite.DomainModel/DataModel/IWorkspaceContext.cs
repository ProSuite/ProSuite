using ArcGIS.Core.Data;

namespace ProSuite.DomainModel.DataModel
{
	public interface IWorkspaceContext : IDatasetContext
	{
		Geodatabase Geodatabase { get; }
	}
}
