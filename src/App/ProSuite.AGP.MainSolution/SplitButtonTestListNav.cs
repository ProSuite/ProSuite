using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace ProSuite.AGP.MainSolution
{
	internal class SplitButtonTestListNav_GoNext : Button
	{
		protected override void OnClick()
		{
			var workList = Module1.Current.GetTestWorklist();
			workList.GoNext();
			workList.Current?.SetVisited(true);
			QueuedTask.Run(() => Module1.Current.RedrawMap());
		}
	}

	internal class SplitButtonTestListNav_GoPrevious : Button
	{
		protected override void OnClick()
		{
			var workList = Module1.Current.GetTestWorklist();
			workList.GoPrevious();
			workList.Current?.SetVisited(true);
			QueuedTask.Run(() => Module1.Current.RedrawMap());
		}
	}

	internal class SplitButtonTestListNav_GoFirst : Button
	{
		protected override void OnClick()
		{
			var workList = Module1.Current.GetTestWorklist();
			workList.GoFirst();
			workList.Current?.SetVisited(true);
			QueuedTask.Run(() => Module1.Current.RedrawMap());
		}
	}
}
