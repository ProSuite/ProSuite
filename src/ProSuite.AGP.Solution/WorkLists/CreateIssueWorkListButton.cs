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
				await QueuedTask.Run(
					() => WorkListsModule.Current.CreateWorkListAsync(
						new DatabaseWorkEnvironment(), WorkListsModule.Current.EnsureUniqueName()));

				WorkListsModule.Current.ShowView(WorkListsModule.Current.EnsureUniqueName());
			}, _msg);
		}
	}
}
