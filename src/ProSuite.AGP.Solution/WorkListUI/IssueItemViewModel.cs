using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class IssueItemViewModel : WorkItemViewModelBase
	{
		public IssueItemViewModel([NotNull] IssueItem item,
		                          [NotNull] IWorkList workList)
			: base(item, workList)
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
