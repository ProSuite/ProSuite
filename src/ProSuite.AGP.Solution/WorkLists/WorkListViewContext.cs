using Clients.AGP.ProSuiteSolution.WorkListUI;
using ProSuite.AGP.Solution.WorkListUI;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class WorkListViewContext
	{
		public WorkListViewContext(WorkListViewModel viewModel)
		{
			ViewModel = viewModel;
		}
		
		public WorkListViewModel ViewModel { get; }

		public bool ViewIsVisible { get; set; }

	}
}
