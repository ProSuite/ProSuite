using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItem
	{
		/// <summary>
		/// The work item id.
		/// </summary>
		long OID { get; }

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

		bool Visited { get; set; }

		/// <summary>
		/// The reference to the GdbObject that represents the work item's source row.
		/// This should become obsolete or at least Nullable or member of a derived IGdbWorkItem
		/// interface in the future, once work lists do not necessarily represent GDB Rows.
		/// </summary>
		GdbRowIdentity GdbRowProxy { get; }

		WorkItemStatus Status { get; set; }

		[CanBeNull]
		Envelope Extent { get; }

		[CanBeNull]
		Geometry Geometry { get; set; }

		[CanBeNull]
		string Description { get; }

		GeometryType? GeometryType { get; }

		// TODO: (daro) rename to HasExtent
		bool HasGeometry { get; }
		// TODO: (daro) rename to HasGeometry
		bool HasFeatureGeometry { get; }

		void QueryPoints(out double xmin, out double ymin,
		                 out double xmax, out double ymax,
		                 out double zmax, double minimumSize);

		void QueryPoints(out double xmin, out double ymin,
		                 out double xmax, out double ymax,
		                 out double zmax);
	}
}
