using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItemRepository
	{
		int GetCount(QueryFilter filter = null);

		IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true);

		// todo daro: get rid of this! Pass in all needed parameters in constructor.
		IEnumerable<ISourceClass> RegisterDatasets(ICollection<GdbTableIdentity> datasets);

		void Save(IWorkItem item);
	}
}
