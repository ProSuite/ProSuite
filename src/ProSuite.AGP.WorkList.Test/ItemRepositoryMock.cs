using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Test;

public class ItemRepositoryMock : IWorkItemRepository
{
	private readonly List<IWorkItem> _items;
	private int _currentIndex;
	private int _lastUsedOid;

	public ItemRepositoryMock(IEnumerable<IWorkItem> items,
	                          IWorkItemStateRepository stateRepository = null)
	{
		_items = items.ToList();
		WorkItemStateRepository = stateRepository;
	}

	public SpatialReference SpatialReference { get; set; }
	public Geometry AreaOfInterest { get; set; }
	public Envelope Extent { get; set; }

	public long Count()
	{
		throw new NotImplementedException();
	}

	public IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
		QueryFilter filter, WorkItemStatus? statusFilter,
		bool excludeGeometry = false)
	{
		IEnumerable<IWorkItem> query =
			statusFilter == null
				? _items
				: _items.Where(item => item.Status == statusFilter);

		IEnumerable<IWorkItem> result;
		if (filter.ObjectIDs.Count == 0)
		{
			result = query;
		}
		else
		{
			result = query.Where(item => filter.ObjectIDs.Contains(item.GdbRowProxy.ObjectId));
		}

		IEnumerable<KeyValuePair<IWorkItem, Envelope>> dictionary =
			result.ToDictionary(item => item, item => item.Extent);

		foreach ((IWorkItem item, Geometry geometry) in dictionary)
		{
			WorkItemStateRepository?.Refresh(item);
			yield return KeyValuePair.Create(item, geometry);
		}
	}

	public IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(
		Table table, QueryFilter filter,
		WorkItemStatus? statusFilter,
		bool excludeGeometry = false)
	{
		throw new NotImplementedException();
	}

	public IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(QueryFilter filter)
	{
		return GetItems(filter, null, false);
	}

	public void Refresh(IWorkItem item)
	{
		WorkItemStateRepository?.Refresh(item);
	}

	public void UpdateState(IWorkItem item)
	{
		WorkItemStateRepository?.UpdateState(item);
	}

	public void Commit()
	{
		WorkItemStateRepository?.Commit(new List<ISourceClass> { new SourceClassMock() }, Extent);
	}

	public void SetCurrentIndex(int currentIndex)
	{
		_currentIndex = currentIndex;
	}

	public int GetCurrentIndex()
	{
		return _currentIndex;
	}

	public Task SetStatusAsync(IWorkItem item, WorkItemStatus status)
	{
		UpdateState(item);

		return Task.CompletedTask;
	}

	public IList<ISourceClass> SourceClasses { get; }

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

	public Row GetSourceRow(ISourceClass sourceClass, long oid, bool recycle = true)
	{
		throw new NotImplementedException();
	}

	public long GetNextOid()
	{
		return ++_lastUsedOid;
	}

	public void SetVisited(IWorkItem item)
	{
		item.Visited = true;
		UpdateState(item);
	}

	public void UpdateStateRepository(string path)
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
}
