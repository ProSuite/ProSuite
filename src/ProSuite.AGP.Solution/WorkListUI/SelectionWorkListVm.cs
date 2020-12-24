using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class SelectionWorkListVm : WorkListViewModelBase
	{
		private WorkItemVmBase _currentWorkItem;

		public SelectionWorkListVm(IWorkList workList)
		{
			CurrentWorkList = workList;
			CurrentWorkList.GoNext();
			CurrentWorkItem = new SelectionWorkItemVm(CurrentWorkList.Current as SelectionItem);
		}

		public override WorkItemVmBase CurrentWorkItem
		{
			get => new SelectionWorkItemVm(CurrentWorkList.Current as SelectionItem);
			set
			{
				SetProperty(ref _currentWorkItem, value, () => CurrentWorkItem);
				Status = CurrentWorkItem.Status;
				Visited = CurrentWorkItem.Visited;
				CurrentIndex = CurrentWorkList.CurrentIndex;
				Count = GetCount();
			}
		}
	}
}
