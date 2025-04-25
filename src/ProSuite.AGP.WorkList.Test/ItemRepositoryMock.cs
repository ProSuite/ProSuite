using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Test
{
	public class ItemRepositoryMock : IWorkItemRepository
	{
		private readonly List<IWorkItem> _items;
		private int _currentIndex;
		private int _lastUsedOid;

		public ItemRepositoryMock(IEnumerable<IWorkItem> items, IWorkItemStateRepository stateRepository = null)
		{
			_items = items.ToList();
			WorkItemStateRepository = stateRepository;
		}

		public ItemRepositoryMock(List<Table> items, IWorkItemStateRepository stateRepository = null)
		{
			WorkItemStateRepository = stateRepository;
		}

		public int GetCount(QueryFilter filter = null)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
			QueryFilter filter = null, WorkItemStatus? statusFilter = null, bool recycle = true,
			bool excludeGeometry = false)
		{
			IEnumerable<IWorkItem> query =
				statusFilter == null
					? _items
					: _items.Where(item => item.Status == statusFilter);

			IEnumerable<IWorkItem> result =
				filter == null
					? query
					: query.Where(item => filter.ObjectIDs.Contains(item.GdbRowProxy.ObjectId));

			IEnumerable<KeyValuePair<IWorkItem, Envelope>> dictionary = result.ToDictionary(item => item, item => item.Extent);

			foreach ((IWorkItem item, Geometry geometry) in dictionary)
			{
				WorkItemStateRepository?.Refresh(item);
				yield return KeyValuePair.Create(item, geometry);
			}
		}

		public void Refresh(IWorkItem item)
		{
			WorkItemStateRepository?.Refresh(item);
		}

		public void UpdateState(IWorkItem item)
		{
			WorkItemStateRepository?.UpdateState(item);
		}

		public Task UpdateAsync(IWorkItem item)
		{
			return Task.FromResult(0);
		}

		public void Save(IWorkItem item)
		{
			throw new NotImplementedException();
		}
		
		public void Commit()
		{
			WorkItemStateRepository?.Commit(new List<ISourceClass> { new SourceClassMock() });
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
			WorkItemStateRepository?.UpdateState(item);
		}

		public Task SetStatusAsync(IWorkItem item, WorkItemStatus status)
		{
			item.Status = status;
			WorkItemStateRepository?.UpdateState(item);

			return Task.CompletedTask;
		}

		public void UpdateStateRepository(string path)
		{
			throw new NotImplementedException();
		}

		public IList<ISourceClass> SourceClasses { get; }

		public string WorkListDefinitionFilePath { get; set; }

		[CanBeNull]
		public IWorkItemStateRepository WorkItemStateRepository { get; }

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

		public long GetNextOid()
		{
			return ++_lastUsedOid;
		}

		public IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(Table table, QueryFilter filter = null,
		                                                               WorkItemStatus? statusFilter = null, bool recycle = true,
		                                                               bool excludeGeometry = false)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItemsCore(
			QueryFilter filter, bool excludeGeometry = false)
		{
			throw new NotImplementedException();
		}

		public void Add(IWorkItem item)
		{
			_items.Add(item);
		}

		public bool Remove(IWorkItem item)
		{
			return _items.Remove(item);
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
