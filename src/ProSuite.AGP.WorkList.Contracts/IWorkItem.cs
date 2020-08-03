using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	/// <summary>
	///     A WorkItem is an element in a WorkList.
	///     It has a Status, a Visited flag, a geometric extent, and
	///     a description. Subclasses may provide additional details.
	/// </summary>
	public interface IWorkItem
	{
		int OID { get; }
		bool Visited { get; set; }
		GdbRowReference Proxy { get; }
		WorkItemStatus Status { get; set; }
		
		Envelope Extent { get; }

		// todo daro: no write access here. It all happens in repository
		void SetDone(bool done = true);

		void SetVisited(bool visited = true);
	}
}
