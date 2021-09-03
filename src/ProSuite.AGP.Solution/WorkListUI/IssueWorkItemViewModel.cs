using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class IssueWorkItemViewModel : WorkItemViewModelBase
	{
		// todo daro: rename to IssueItemViewModel
		// todo daro: extract interface from WorkListViewModelBase to pass as parameter?
		public IssueWorkItemViewModel([NotNull] IssueItem item,
		                              [NotNull] WorkListViewModelBase viewModel)
			: base(item, viewModel)
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
