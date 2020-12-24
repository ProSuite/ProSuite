using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class CreateSelectionWorkListButton : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override async void OnClick()
		{
			await ViewUtils.TryAsync(async () =>
			{
				var environment = new InMemoryWorkEnvironment();

				await QueuedTask.Run(() => WorkListsModule.Current.CreateWorkListAsync(environment));

				string workListName = environment.UniqueName;

				if (workListName == null)
				{
					return;
				}

				WorkListsModule.Current.ShowView(workListName);
			}, _msg);
		}
	}
}
