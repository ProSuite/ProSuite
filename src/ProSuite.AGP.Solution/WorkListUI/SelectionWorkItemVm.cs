using ProSuite.AGP.WorkList;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class SelectionWorkItemVm : WorkItemVmBase
	{
		public SelectionWorkItemVm(SelectionItem workItem) : base(workItem)
		{
			WorkItem = workItem;
		}
	}
}
