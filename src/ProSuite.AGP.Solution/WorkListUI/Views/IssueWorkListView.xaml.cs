namespace ProSuite.AGP.Solution.WorkListUI.Views
{
	public partial class IssueWorkListView
	{
		public IssueWorkListView(WorkListViewModelBase vm)
		{
			InitializeComponent();
			DataContext = vm;
		}
	}
}
