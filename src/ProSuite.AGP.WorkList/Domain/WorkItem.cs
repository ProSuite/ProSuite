using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain
{
	public abstract class WorkItem : IWorkItem
	{
		public abstract int OID { get; } // empirical: must be int (not long) or Pro fails
		public abstract string Description { get; }
		public abstract WorkItemStatus Status { get; protected set; }
		public abstract WorkItemVisited Visited { get; protected set; } // TODO bool
		public abstract Envelope Extent { get; }

		public abstract void SetDone(bool done = true);
		public abstract void SetVisited(bool visited = true);

		public override string ToString()
		{
			return $"OID={OID} {Description}";
		}
	}
}
