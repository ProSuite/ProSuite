using System.Diagnostics;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using JetBrains.Annotations;
using ProSuite.AGP.Solution.WorkListUI;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class CreateSelectionWorkListButton : Button
	{ 
		protected async override void OnClick()
		{

			var workList = await QueuedTask.Run(() =>
			{
				var env = new InMemoryWorkEnvironment();
				IWorkList wl = env.CreateWorkList();
				return wl;
			});
			WorkListsModule.Current.WorkListAdded(workList);
			WorkListsModule.Current.ShowView(workList);

		}
	}
}
