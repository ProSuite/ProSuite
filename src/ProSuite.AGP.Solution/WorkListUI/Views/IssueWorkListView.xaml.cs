namespace ProSuite.AGP.Solution.WorkListUI.Views
{
	public partial class IssueWorkListView
	{
		public IssueWorkListView(IssueWorkListVm vm)
		{
			InitializeComponent();
			DataContext = vm;
		}
	}
}
