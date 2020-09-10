using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using JetBrains.Annotations;
using ProSuite.AGP.Solution.WorkListUI;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class CreateSelectionWorkListButton : Button
	{ 
		protected override void OnClick()
		{
			

			QueuedTask.Run(() =>
			{
				var env = new InMemoryWorkEnvironment();
				env.CreateWorkList();
			});
		}
	}
}
