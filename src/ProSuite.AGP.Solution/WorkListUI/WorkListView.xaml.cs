namespace ProSuite.AGP.Solution.WorkListUI
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
