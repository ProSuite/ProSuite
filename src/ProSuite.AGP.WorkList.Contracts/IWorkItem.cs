using ArcGIS.Core.Geometry;

namespace ProSuite.AGP.WorkList.Contracts
{
	/// <summary>
	/// A WorkItem is an element in a WorkList.
	/// It has a Status, a Visited flag, a geometric extent, and
	/// a description. Subclasses may provide additional details.
	/// </summary>
	public interface IWorkItem
	{
		long OID { get; }
		string Description { get; }
		WorkItemStatus Status { get; }
		WorkItemVisited Visited { get; } // TODO bool
		Geometry Shape { get; }
		Envelope Extent { get; }

		void SetDone(bool done = true);
		void SetVisited(bool visited = true);
	}
}
