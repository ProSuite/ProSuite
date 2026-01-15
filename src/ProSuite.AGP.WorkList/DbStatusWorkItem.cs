using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList;

public class DbStatusWorkItem : WorkItem
{
	public DbStatusWorkItem(long uniqueTableId,
	                        GdbRowIdentity identity,
	                        WorkItemStatus status)
		: base(uniqueTableId, identity)
	{
		Status = status;
	}
}
