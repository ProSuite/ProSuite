using Clients.AGP.ProSuiteSolution.WorkListUI;

namespace ProSuite.AGP.Solution.WorkListUI
{
	/// <summary>
	/// Interaction logic for WorkList.xaml
	/// </summary>
	public partial class WorkListView : ArcGIS.Desktop.Framework.Controls.ProWindow
	{
		public WorkListView()
		{
			InitializeComponent();
			DataContext = new WorkListViewModel();
		}

		public WorkListView(WorkListViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
		}
	}
}
