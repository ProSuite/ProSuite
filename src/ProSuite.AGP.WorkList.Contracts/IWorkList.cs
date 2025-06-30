using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

public interface IWorkList : IRowCache
{
	string Name { get; }

	string DisplayName { get; }

	WorkItemVisibility? Visibility { get; set; }

	IWorkItem Current { get; }

	int CurrentIndex { get; }

	// TODO: daro hide?
	IWorkItemRepository Repository { get; }

	long? TotalCount { get; set; }

	public Envelope GetExtent();

	event EventHandler<WorkListChangedEventArgs> WorkListChanged;

	IEnumerable<IWorkItem> Search(QueryFilter filter);

	IEnumerable<IWorkItem> GetItems(SpatialQueryFilter filter);
	
	long CountLoadedItems(out int todo);

	bool CanGoFirst();

	void GoFirst();

	void GoTo(long oid);

	bool CanGoNearest();

	void GoNearest([NotNull] Geometry reference,
	               [CanBeNull] Predicate<IWorkItem> match = null,
	               params Polygon[] contextPerimeters);

	bool CanGoNext();

	void GoNext();

	bool CanGoPrevious();

	void GoPrevious();

	bool CanSetStatus();

	//void SetVisited([NotNull] IWorkItem item);
	void SetVisited(IList<IWorkItem> items, bool visited);

	void Commit();

	Task SetStatusAsync([NotNull] IWorkItem item, WorkItemStatus status);

	bool IsValid(out string message);

	IAttributeReader GetAttributeReader(long forSourceClassId);

	Geometry GetItemGeometry(IWorkItem item);

	void SetItemsGeometryDraftMode(bool enable);

	void Rename(string name);

	void Invalidate(Envelope geometry);

	void UpdateExistingItemGeometries(QueryFilter filter);

	void Count();

	Row GetCurrentItemSourceRow();

	void LoadItems(QueryFilter filter);
}
