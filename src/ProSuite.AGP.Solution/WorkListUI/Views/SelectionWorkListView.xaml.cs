namespace ProSuite.AGP.Solution.WorkListUI.Views
{
	public partial class SelectionWorkListView
	{
		public SelectionWorkListView(WorkListViewModelBase vm)
		{
			InitializeComponent();
			DataContext = vm;
		}
	}
}
