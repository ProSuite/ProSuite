using ArcGIS.Desktop.Framework;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class SelectionWorkListVm : WorkListViewModelBase
	{
		private WorkListView _view;

		public SelectionWorkListVm(IWorkList workList)
		{
			CurrentWorkList = workList;
			CurrentWorkList.GoNext();
			CurrentWorkItem = new WorkItemVm(CurrentWorkList.Current);
		}

		protected override WorkListView View
		{
			get => _view;
			set => _view = value;
		}

		public override void Show(IWorkList workList)
		{
			View = new WorkListView(this);
			View.Owner = FrameworkApplication.Current.MainWindow;
			View.Title = workList.Name;
			View.Show();
		}
	}
}
