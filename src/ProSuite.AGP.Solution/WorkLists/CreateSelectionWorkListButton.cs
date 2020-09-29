using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.Solution.WorkListUI;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class CreateSelectionWorkListButton : Button
	{ 
		protected override async void OnClick()
		{
			//need to get worklist back from queuedtask to pass it on
			var workList = await QueuedTask.Run(() =>
			{
				var env = new InMemoryWorkEnvironment();
				IWorkList wl = env.CreateWorkList();
				return wl;
			});
			WorkListsModule.Current.RegisterObserver(new WorkListViewModel(workList as SelectionWorkList));
			WorkListsModule.Current.ShowView(workList);
		}
	}
}
