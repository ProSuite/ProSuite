using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Domain
{
	public abstract class WorkItem : IWorkItem
	{
		public abstract long OID { get; }
		public abstract string Description { get; }
		public abstract WorkItemStatus Status { get; protected set; }
		public abstract WorkItemVisited Visited { get; protected set; } // TODO bool
		public abstract Geometry Shape { get; }
		public abstract Envelope Extent { get; }

		//public abstract GdbRowReference Proxy { get; } // TODO really needed?

		public abstract void SetDone(bool done = true);
		public abstract void SetVisited(bool visited = true);
	}
}
