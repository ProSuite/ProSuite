using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class ShowWorkListsButton : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override void OnClick()
		{
			ViewUtils.Try(() => WorkListsModule.Current.ShowView(), _msg);
		}
	}
}
