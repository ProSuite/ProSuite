using Clients.AGP.ProSuiteSolution.WorkListUI;
using ProSuite.AGP.Solution.WorkListUI;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class WorkListViewContext
	{
		public WorkListViewContext(WorkListView view)
		{
			View = view;
			View.Closed += View_Closed;
		}

		private void View_Closed(object sender, System.EventArgs e)
		{
			ViewIsVisible = false;
		}

		//public WorkListViewModel  ViewModel { get; set; }
		public WorkListView View { get; set; }

		public bool ViewIsVisible { get; set; }



	}
}
