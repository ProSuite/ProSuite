using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace Clients.AGP.ProSuiteSolution.WorkLists
{
	[UsedImplicitly]
	internal class GoNextButton : Button
	{
		protected override void OnClick()
		{
			QueuedTask.Run(() => WorkListsModule.Current.GoNext());
		}
	}
}