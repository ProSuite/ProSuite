using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class ShowWorkListsButton : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override async void OnClick()
		{
			await ViewUtils.TryAsync(WorkListsModule.Current.ShowViewAsync(), _msg);
		}
	}
}
