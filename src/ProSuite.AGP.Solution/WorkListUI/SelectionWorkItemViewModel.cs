using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class SelectionWorkItemViewModel : WorkItemViewModelBase
	{
		public SelectionWorkItemViewModel([NotNull] IWorkItem workItem, [NotNull] IWorkList worklist) :
			base(workItem, worklist) { }
	}
}
