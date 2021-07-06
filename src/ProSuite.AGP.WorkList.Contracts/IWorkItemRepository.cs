using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItemRepository : IDisposable
	{
		int GetCount(QueryFilter filter = null);

		IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true);

		// todo daro: extract Interface ISource
		IEnumerable<IWorkItem> GetItems(GdbTableIdentity tableId, QueryFilter filter, bool recycle = true);

		void Refresh(IWorkItem item);

		Task UpdateAsync(IWorkItem item);

		void UpdateVolatileState(IEnumerable<IWorkItem> items);

		void Commit();

		void Discard();

		// todo daro: is this the right way?
		void SetCurrentIndex(int currentIndex);
		int GetCurrentIndex();

		void SetVisited(IWorkItem item);

		Task SetStatus(IWorkItem item, WorkItemStatus status);

		void UpdateStateRepository(string path);
	}
}
