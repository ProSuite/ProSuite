using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Test
{
	public class ItemRepositoryMock : IWorkItemRepository
	{
		private readonly IEnumerable<IWorkItem> _items;
		private int _currentIndex;

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

		public IEnumerable<IWorkItem> GetItems(Geometry areaOfInterest,
		                                       WorkItemStatus? statusFilter, bool recycle = true)
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

		public Task UpdateAsync(IWorkItem item)
		{
			return Task.FromResult(0);
		}

		public void Save(IWorkItem item)
		{
			throw new NotImplementedException();
		}

		public void UpdateVolatileState(IEnumerable<IWorkItem> items) { }

		public void Commit()
		{
			throw new NotImplementedException();
		}

		public void Discard()
		{
			throw new NotImplementedException();
		}

		public void SetCurrentIndex(int currentIndex)
		{
			_currentIndex = currentIndex;
		}

		public int GetCurrentIndex()
		{
			return _currentIndex;
		}

		public void SetVisited(IWorkItem item)
		{
			item.Visited = true;
		}

		public Task SetStatus(IWorkItem item, WorkItemStatus status)
		{
			throw new NotImplementedException();
		}

		public void UpdateStateRepository(string path)
		{
			throw new NotImplementedException();
		}

		public List<ISourceClass> SourceClasses { get; }

		public string WorkListDefinitionFilePath { get; set; }

		public void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo)
		{
			throw new NotImplementedException();
		}

		public bool CanUseTableSchema(IWorkListItemDatastore workListItemSchema)
		{
			throw new NotImplementedException();
		}

		public Row GetSourceRow(ISourceClass sourceClass, long oid)
		{
			throw new NotImplementedException();
		}

		public void RefreshGeometry(IWorkItem item)
		{
			throw new NotImplementedException();
		}

		public void RefreshGeometry2(IWorkItem item)
		{
			throw new NotImplementedException();
		}

		public void Dispose() { }
	}
}
