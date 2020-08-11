using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace ProSuite.AGP.MainSolution
{
	internal class SplitButtonTestListNav_GoNext : Button
	{
		protected override void OnClick()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			workList.GoNext();
			workList.Current?.SetVisited();
			QueuedTask.Run(() => WorkListTrialsModule.Current.RedrawMap());
		}
	}

	internal class SplitButtonTestListNav_GoPrevious : Button
	{
		protected override void OnClick()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			workList.GoPrevious();
			workList.Current?.SetVisited();
			QueuedTask.Run(() => WorkListTrialsModule.Current.RedrawMap());
		}
	}

	internal class SplitButtonTestListNav_GoFirst : Button
	{
		protected override void OnClick()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			workList.GoFirst();
			workList.Current?.SetVisited();
			QueuedTask.Run(() => WorkListTrialsModule.Current.RedrawMap());
		}
	}
}
