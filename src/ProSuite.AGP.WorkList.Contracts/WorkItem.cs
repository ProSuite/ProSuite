using ArcGIS.Core.Geometry;
using ProSuite.Commons.AG.Gdb;
using Envelope = ArcGIS.Core.Geometry.Envelope;

namespace ProSuite.AGP.WorkList.Contracts
{
	//public interface IWorkItem
	//{
	//	long OID { get; }
	//	WorkItemVisited Visited { get; }
	//	GdbRowReference Proxy { get; }
	//}

	/// <summary>
	/// A WorkItem is an element in a WorkList.
	/// It has a Status, a Visited flag, a geometric extent, and
	/// a description. Subclasses may provide additional details.
	/// </summary>
	public abstract class WorkItem
	{
		public abstract int OID { get; }
		public abstract string Description { get; }
		public abstract WorkItemStatus Status { get; protected set; }
		public abstract WorkItemVisited Visited { get; protected set; } // TODO bool
		public abstract Geometry Shape { get; }
		public abstract Envelope Extent { get; }
		public abstract GdbRowReference Proxy { get; }

		public abstract void SetStatus(WorkItemStatus status);
		public abstract void SetVisited(bool visited);
	}
}
