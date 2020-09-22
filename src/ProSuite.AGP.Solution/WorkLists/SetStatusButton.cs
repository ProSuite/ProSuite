using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class SetStatusButton : Button
	{
		protected override void OnClick()
		{
			QueuedTask.Run(() => WorkListsModule.Current.SetStatus());
		}
	}
}
