using ProSuite.Commons.AG.Gdb;

namespace ProSuite.AGP.WorkList.Contracts
{
	public interface IWorkItem
	{
		long Oid { get; }
		WorkItemVisited Visited { get; }
		GdbRowReference Reference { get; }
	}
}
