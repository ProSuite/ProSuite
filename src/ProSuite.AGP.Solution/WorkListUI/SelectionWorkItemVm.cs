using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class SelectionWorkItemVm : WorkItemVmBase
	{
		public SelectionWorkItemVm([NotNull] IWorkItem workItem, [NotNull] IWorkList worklist) :
			base(workItem, worklist) { }
	}
}
