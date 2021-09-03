using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class SelectionWorkListViewModel : WorkListViewModelBase
	{
		public SelectionWorkListViewModel([NotNull] IWorkList workList) : base(workList) { }

		protected override void SetCurrentItemCore(IWorkItem item)
		{
			Assert.ArgumentNotNull(item, nameof(item));

			CurrentItemViewModel = new SelectionWorkItemViewModel(item, this);
		}
	}
}
