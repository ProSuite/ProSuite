using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class SelectionWorkListVm : WorkListViewModelBase
	{
		private WorkListView _view;
		private readonly bool _hasDetailSection;
		private WorkItemVmBase _currentWorkItem;

		public SelectionWorkListVm(IWorkList workList)
		{
			CurrentWorkList = workList;
			CurrentWorkList.GoNext();
			CurrentWorkItem = new SelectionWorkItemVm(CurrentWorkList.Current as SelectionItem);
			_hasDetailSection = false;
		}

		public override bool HasDetailSection => _hasDetailSection;

		public override WorkItemVmBase CurrentWorkItem
		{
			get => _currentWorkItem;
			set => _currentWorkItem = value;
		}
	}
}
