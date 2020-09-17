using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItemRepository
	{
		int GetCount(QueryFilter filter = null);

		IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true);

		// todo daro: extract Interface ISource
		IEnumerable<IWorkItem> GetItems(GdbTableIdentity tableId, QueryFilter filter, bool recycle = true);

		void Refresh(IWorkItem item);

		void Update(IWorkItem item);

		void UpdateVolatileState(IEnumerable<IWorkItem> items);

		void Commit();

		void Discard();
	}
}
