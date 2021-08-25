using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class SelectionWorkListVm : WorkListViewModelBase
	{
		public SelectionWorkListVm([NotNull] IWorkList workList) : base(workList) { }

		protected override void SetCurrentItemCore(IWorkItem item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			CurrentWorkItem = new SelectionWorkItemVm(item, CurrentWorkList);
		}
	}
}
