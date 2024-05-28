using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItem
	{
		/// <summary>
		/// The work item id
		/// </summary>
		long OID { get; }

		bool Visited { get; set; }
		GdbRowIdentity Proxy { get; }
		WorkItemStatus Status { get; set; }

		[CanBeNull]
		Envelope Extent { get; }

		[CanBeNull]
		string Description { get; }

		GeometryType? GeometryType { get; }
		bool HasGeometry { get; set; }

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

		void QueryPoints(out double xmin, out double ymin,
		                 out double xmax, out double ymax,
		                 out double zmax, double minimumSize);

		void QueryPoints(out double xmin, out double ymin,
		                 out double xmax, out double ymax,
		                 out double zmax);
	}
}
