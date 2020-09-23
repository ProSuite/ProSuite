using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using Clients.AGP.ProSuiteSolution.WorkListUI;
using JetBrains.Annotations;
using ProSuite.AGP.Solution.WorkListUI;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class CreateSelectionWorkListButton : Button
	{ 
		protected async override void OnClick()
		{

			//need to get worklist back from queuedtask to pass it on
			var workList = await QueuedTask.Run(() =>
			{
				var env = new InMemoryWorkEnvironment();
				IWorkList wl = env.CreateWorkList();
				return wl;
			});
			WorkListsModule.Current.RegisterObserver(new WorkListViewModel(workList as SelectionWorkList));
			//WorkListsModule.Current.WorkListAdded(workList);
			WorkListsModule.Current.ShowView(workList);
		}
	}
}
