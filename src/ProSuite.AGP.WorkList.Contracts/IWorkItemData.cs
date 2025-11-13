using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;

namespace ProSuite.AGP.WorkList.Contracts;

public interface IWorkItemData
{
	/// <summary>
	/// The unique name of the work list (table name).
	/// </summary>
	string Name { get; }

	/// <summary>
	/// The display name to be used in the user interface.
	/// </summary>
	string DisplayName { get; }

	/// <summary>
	/// The extent of all work items in the work list.
	/// </summary>
	Envelope Extent { get; }

	/// <summary>
	/// Returns the work items matching the given filter. No where clause is supported, only
	/// OID lists.
	/// </summary>
	/// <param name="filter"></param>
	/// <returns></returns>
	IEnumerable<IWorkItem> Search(QueryFilter filter);

	/// <summary>
	/// Returns the work items matching the given spatial filter.
	/// </summary>
	/// <param name="filter"></param>
	/// <returns></returns>
	IEnumerable<IWorkItem> Search(SpatialQueryFilter filter);

	/// <summary>
	/// Returns a geometry suitable for display in the map for the given work item.
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public Geometry GetItemDisplayGeometry(IWorkItem item);

	/// <summary>
	/// The current work item or null if the work list navigator is not shown.
	/// </summary>
	public IWorkItem CurrentItem { get; }
}
