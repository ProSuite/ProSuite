using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class CreateIssueWorkListButton : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override async void OnClick()
		{
			await ViewUtils.TryAsync(async () =>
			{
				// has to be outside QueuedTask because of OpenItemDialog
				var environment = new DatabaseWorkEnvironment();

				string name = WorkListsModule.Current.EnsureUniqueName();

				await QueuedTask.Run(
					() => WorkListsModule.Current.CreateWorkListAsync(environment, name));

				WorkListsModule.Current.ShowView(name);
			}, _msg);
		}
	}
}
