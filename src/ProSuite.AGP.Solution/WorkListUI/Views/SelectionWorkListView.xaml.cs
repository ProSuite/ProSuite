namespace ProSuite.AGP.Solution.WorkListUI.Views
{
	public partial class WorkListView
	{
		public WorkListView(WorkListViewModelBase vm)
		{
			InitializeComponent();
			DataContext = vm;
		}
	}
}
