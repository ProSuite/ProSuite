using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	internal class GoFirstButton : Button
	{
		protected override void OnClick()
		{
			QueuedTask.Run(() => WorkListsModule.Current.GoFirst());
		}

		protected override void OnUpdate()
		{
			Enabled = WorkListsModule.Current.CanGoFirst();
		}
	}
}
