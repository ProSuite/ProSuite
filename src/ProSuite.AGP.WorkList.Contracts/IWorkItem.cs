using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts;

public interface IWorkItem
{
	/// <summary>
	/// The work item id.
	/// </summary>
	long OID { get; set; }

	/// <summary>
	/// Object ID of the work item's source row
	/// </summary>
	long ObjectID { get; }

	/// <summary>
	/// The (potentially synthetic) table id of the work item's source row. This is not
	/// necessarily the same as the Table.GetID() value of the source row. It is unique
	/// and stable across sessions and corresponds to ISourceClass.GetUniqueTableId().
	/// </summary>
	long UniqueTableId { get; }

	/// <summary>
	/// The reference to the GdbObject that represents the work item's source row.
	/// This should become obsolete or at least Nullable or member of a derived IGdbWorkItem
	/// interface in the future, once work lists do not necessarily represent GDB Rows.
	/// </summary>
	GdbRowIdentity GdbRowProxy { get; }

	bool Visited { get; set; }

	WorkItemStatus Status { get; set; }

	[CanBeNull]
	Envelope Extent { get; }

	/// <summary>
	/// The buffered wireframe geometry of the work item, if applicable.
	/// </summary>
	[CanBeNull]
	Geometry BufferedGeometry { get; }

	/// <summary>
	/// The geometry type of the work item's source feature, if any.
	/// </summary>
	[CanBeNull]
	GeometryType? SourceGeometryType { get; set; }

	bool HasExtent { get; }

	bool HasBufferedGeometry { get; }

	void SetBufferedGeometry(Geometry geometry);

	void SetExtent(Envelope extent);

	string GetDescription();
}
