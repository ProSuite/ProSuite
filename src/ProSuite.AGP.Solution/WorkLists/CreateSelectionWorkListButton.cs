using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
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
				string name = WorkListsModule.Current.EnsureUniqueName();

				await QueuedTask.Run(
					() => WorkListsModule.Current.CreateWorkListAsync(
						new InMemoryWorkEnvironment(), name));

				WorkListsModule.Current.ShowView(name);
			}, _msg);
		}
	}
}
