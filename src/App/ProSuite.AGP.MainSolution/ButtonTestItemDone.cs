using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.WorkList.Contracts;

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
			var workList = Module1.Current.GetTestWorklist();

			var current = workList.Current;
			if (current != null)
			{
				current.SetStatus(WorkItemStatus.Done);
			}

			Module1.Current.RedrawMap();
		}
	}
}
