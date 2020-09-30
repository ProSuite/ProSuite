using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class CreateSelectionWorkListButton : Button
	{ 
		protected override async void OnClick()
		{
			var environment = new InMemoryWorkEnvironment();

			await QueuedTask.Run(() =>
			{
				WorkListsModule.Current.CreateWorkList(environment);
			});

			WorkListsModule.Current.ShowView(environment.UniqueName);
		}
	}
}
