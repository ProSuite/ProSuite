namespace ProSuite.AGP.Solution.WorkListUI.Views
{
	public partial class WorkListView
	{
		public WorkListView(SelectionWorkListVm vm)
		{
			InitializeComponent();
			DataContext = vm;
		}
	}
}
