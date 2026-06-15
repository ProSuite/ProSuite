using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain.Persistence;

public interface IWorkItemState
{
	long OID { get; set; }
	bool Visited { get; set; }
	WorkItemStatus Status { get; set; }
}
