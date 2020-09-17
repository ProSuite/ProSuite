using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain.Persistence.Json
{
	public class JsonWorkItemState : IWorkItemState
	{
		public int OID { get; set; }
		public bool Visited { get; set; }
		public WorkItemStatus Status { get; set; }
	}
}
