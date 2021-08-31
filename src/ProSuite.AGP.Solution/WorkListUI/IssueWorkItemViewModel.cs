using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class IssueWorkItemViewModel : WorkItemViewModelBase
	{
		// todo daro: rename to IssueWorkItemViewModel or IssueWorkitemViewModel
		public IssueWorkItemViewModel([NotNull] IssueItem item, [NotNull] IWorkList worklist)
			: base(item, worklist)
		{
			IssueItem = item;
		}

		public IssueItem IssueItem { get; }

		[NotNull]
		public string InvolvedObjects => IssueItem.InvolvedObjects;

		public string QualityCondition => IssueItem.QualityCondition;

		public string ErrorDescription => IssueItem.IssueCodeDescription;
	}
}
