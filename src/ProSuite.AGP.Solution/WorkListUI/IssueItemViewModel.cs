using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class IssueItemViewModel : WorkItemViewModelBase
	{
		// todo daro: extract interface from WorkListViewModelBase to pass as parameter?
		public IssueItemViewModel([NotNull] IssueItem item,
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
