using System.Windows;
using Clients.AGP.ProSuiteSolution.WorkListUI;
using ProSuite.AGP.Solution.WorkListUI;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class WorkListViewContext
	{
		//public WorkListViewModel  ViewModel { get; set; }
		public WorkListView view { get; set; }

		public bool ViewIsVisible { get; set; }

		//protected override Freezable CreateInstanceCore()
		//{
		//	//throw new System.NotImplementedException();
		//	return null;
		//}
	}
}
