using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

/// <summary>
/// Interface that encapsulates the persistence of work items, i.e. both the access to the
/// source classes in the geodatabase and the volatile state in the work list definition
/// files.
/// </summary>
public interface IWorkItemRepository
{
	IList<ISourceClass> SourceClasses { get; }

	IWorkItemStateRepository WorkItemStateRepository { get; }

	/// <summary>
	/// The spatial reference of the work items, or null if the source classes do not have
	/// a spatial reference.
	/// </summary>
	SpatialReference SpatialReference { get; }

	/// <summary>
	/// The area of interest for the work items, or null if no area of interest is defined.
	/// Source features are filtered by this geometry if it is set.
	/// </summary>
	[CanBeNull]
	Geometry AreaOfInterest { get; set; }

	/// <summary>
	/// The extent of all work items, or null if the extent has not been computed yet.
	/// </summary>
	Envelope Extent { get; set; }

	long Count();

	IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(QueryFilter filter);

	IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(QueryFilter filter,
	                                                        WorkItemStatus? statusFilter,
	                                                        bool excludeGeometry = false);

	IEnumerable<KeyValuePair<IWorkItem, Geometry>> GetItems(Table table,
	                                                        QueryFilter filter,
	                                                        WorkItemStatus? statusFilter,
	                                                        bool excludeGeometry = false);

	void Commit();

	void SetCurrentIndex(int currentIndex);

	int GetCurrentIndex();

	void UpdateState(IWorkItem item);

	/// <summary>
	/// Persists the work item state.
	/// DbStatusWorkItemRepository performs an edit operation.
	/// SelectionItemRepository persists the state in-memory
	/// and stores it in the definition file.
	/// </summary>
	Task SetStatusAsync(IWorkItem item, WorkItemStatus status);

	/// <summary>
	/// Update the table schema once the domain information is available. This only necessary
	/// if the field values of the source classes depend on the DDX attribute roles instead of
	/// being hard-coded.
	/// </summary>
	/// <param name="tableSchemaInfo"></param>
	void UpdateTableSchemaInfo(IWorkListItemDatastore tableSchemaInfo);

	bool CanUseTableSchema(IWorkListItemDatastore workListItemSchema);

	Row GetSourceRow([NotNull] ISourceClass sourceClass, long oid, bool recycle = true);

	long GetNextOid();

	void Refresh(IWorkItem item);
}
