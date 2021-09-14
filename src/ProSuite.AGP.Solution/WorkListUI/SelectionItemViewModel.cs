using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class SelectionItemViewModel : WorkItemViewModelBase
	{
		public SelectionItemViewModel([NotNull] IWorkItem workItem,
		                              [NotNull] IWorkList workList) :
			base(workItem, workList) { }
	}
}
