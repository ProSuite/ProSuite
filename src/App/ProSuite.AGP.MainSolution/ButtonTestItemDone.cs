using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace ProSuite.AGP.MainSolution
{
	internal class ButtonTestItemDone : Button
	{
		protected override void OnClick()
		{
			QueuedTask.Run(() => SetTestItemDone());
		}

		private void SetTestItemDone()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();

			var current = workList.Current;
			if (current != null)
			{
				current.SetDone();
			}

			WorkListTrialsModule.Current.RedrawMap();
		}
	}
}
