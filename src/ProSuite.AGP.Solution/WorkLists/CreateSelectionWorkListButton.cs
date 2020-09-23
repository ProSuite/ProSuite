using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using JetBrains.Annotations;

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
