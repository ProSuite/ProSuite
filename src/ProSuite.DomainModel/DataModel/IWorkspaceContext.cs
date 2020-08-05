using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.DomainModel.DataModel
{
	public interface IWorkspaceContext : IDatasetContext
	{
		// todo daro: IWorkspaceContext.GetDefinition(table)
		Geodatabase OpenGeodatabase();

		bool Contains(GdbTableReference proxy);
	}
}
