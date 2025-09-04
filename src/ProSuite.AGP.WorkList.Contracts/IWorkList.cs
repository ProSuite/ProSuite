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

	/// <summary>
	/// Allow the Worklist geometry service to calculate and set the work items' geometry
	/// property to its buffered representation, if required.
	/// NOTE: This service uses a background thread to access the items of the work list.
	/// NOTE: This service therefore does not see up-to-date geometries.
	/// </summary>
	bool CacheBufferedItemGeometries { get; set; }

	/// <summary>
	/// Whether the work items should always use the draft mode (envelope geometry). If false,
	/// polyline and polygon geometries are buffered and provided in the
	/// <see cref="IWorkItem.Geometry"/> property. If <see cref="CacheBufferedItemGeometries"/>
	/// is true, all items are buffered, otherwise only the current item.
	/// </summary>
	bool AlwaysUseDraftMode { get; set; }

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

	Row GetCurrentItemSourceRow(bool readOnly = true);

	/// <summary>
	/// Loads all items using the current AOI and status settings.
	/// </summary>
	void LoadItems();

	void LoadItems(QueryFilter filter,
	               WorkItemStatus? statusFilter = null);
}
