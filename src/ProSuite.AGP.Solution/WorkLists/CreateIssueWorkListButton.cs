using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class CreateIssueWorkListButton : Button
	{
		protected override async void OnClick()
		{
			var environment = new DatabaseWorkEnvironment();

			await QueuedTask.Run(async () =>
			{
				await WorkListsModule.Current.CreateWorkListAsync(environment);
			});

			WorkListsModule.Current.ShowView(environment.UniqueName);
		}
	}
}
