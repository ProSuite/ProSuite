using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.WorkList;

namespace Clients.AGP.ProSuiteSolution
{
	internal class CreateSelectionWorkListButton : Button
	{
		protected override void OnClick()
		{
			QueuedTask.Run(() => CreateSelectionWorkList());
		}

		private void CreateSelectionWorkList()
		{
			var env = new InMemoryWorkEnvironment();
			env.OpenWorkList();
		}
	}
}
