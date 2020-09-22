using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Test
{
	public class ItemRepositoryMock : IWorkItemRepository
	{
		private readonly IEnumerable<IWorkItem> _items;

		public ItemRepositoryMock(IEnumerable<IWorkItem> items)
		{
			_items = items;
		}

		public int GetCount(QueryFilter filter = null)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IWorkItem> GetItems(QueryFilter filter = null, bool recycle = true)
		{
			return _items;
		}

		public IEnumerable<IWorkItem> GetItems(GdbTableIdentity tableId, QueryFilter filter,
		                                       bool recycle = true)
		{
			throw new NotImplementedException();
		}

		public void Refresh(IWorkItem item)
		{
			throw new NotImplementedException();
		}

		public void Save(IWorkItem item)
		{
			throw new NotImplementedException();
		}

		public void Update(IWorkItem item)
		{
		}

		public void UpdateVolatileState(IEnumerable<IWorkItem> items)
		{
		}

		public void Commit()
		{
			throw new NotImplementedException();
		}

		public void Discard()
		{
			throw new NotImplementedException();
		}
	}
}
