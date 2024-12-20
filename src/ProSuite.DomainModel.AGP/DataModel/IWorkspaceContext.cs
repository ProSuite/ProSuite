using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.DomainModel.AGP.DataModel
{
	public interface IWorkspaceContext : IDatasetContext
	{
		// todo daro: IWorkspaceContext.GetDefinition(table)
		Geodatabase OpenGeodatabase();

		bool Contains(GdbTableIdentity proxy);
	}
}
