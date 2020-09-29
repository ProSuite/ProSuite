namespace ProSuite.AGP.Solution.WorkListUI
{
	/// <summary>
	/// Interaction logic for WorkList.xaml
	/// </summary>
	public partial class WorkListView
	{
		//public WorkListView()
		//{
		//	InitializeComponent();
		//	DataContext = new WorkListViewModel();
		//}

		public WorkListView(WorkListViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
		}
	}
}
