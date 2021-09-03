using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class SelectionWorkItemViewModel : WorkItemViewModelBase
	{
		// todo daro: rename to SelectionItemViewModel
		public SelectionWorkItemViewModel([NotNull] IWorkItem workItem, [NotNull] WorkListViewModelBase viewModel) :
			base(workItem, viewModel) { }
	}
}
