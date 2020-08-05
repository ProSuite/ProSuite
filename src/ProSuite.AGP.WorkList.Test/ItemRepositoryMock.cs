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

		public IEnumerable<ISourceClass> RegisterDatasets(ICollection<GdbTableIdentity> datasets)
		{
			throw new NotImplementedException();
		}
	}
}
