using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class IssueWorkItemVm : WorkItemVmBase
	{
		public IssueWorkItemVm(IssueItem workItem) : base(workItem)
		{
			IssueItem = workItem;
		}

		public IssueItem IssueItem { get; }

		public string InvolvedObjects
		{
			get => IssueItem.InvolvedObjects;
			set { }
		}

		public string QualityCondition
		{
			get { return WorkItem == null ? string.Empty :  IssueItem.QualityCondition; }
			set { }
		}
	}
}
