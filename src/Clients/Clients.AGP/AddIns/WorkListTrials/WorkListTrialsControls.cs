using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace Clients.AGP.ProSuiteSolution.WorkListTrials
{
	internal class ButtonCreateTestList : Button
	{
		protected override void OnClick()
		{
			QueuedTask.Run(() => { CreateTestList(); });
		}

		private void CreateTestList()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			var workListName = workList.Name;

			var connector = WorkListTrialsModule.Current.GetWorkListConnectionPath(workListName);

			using (var datastore = new PluginDatastore(connector))
			{
				var tableNames = datastore.GetTableNames();
				foreach (var tableName in tableNames)
				{
					using (var table = datastore.OpenTable(tableName))
					{
						LayerFactory.Instance.CreateFeatureLayer(
							(FeatureClass) table, MapView.Active.Map);
					}
				}
			}
		}
	}

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

			WorkListTrialsModule.Current.Refresh();
		}
	}

	internal class SplitButtonTestListNav_GoNext : Button
	{
		protected override void OnClick()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			workList.GoNext();
			workList.Current?.SetVisited();
			QueuedTask.Run(() => WorkListTrialsModule.Current.Refresh());
		}
	}

	internal class SplitButtonTestListNav_GoPrevious : Button
	{
		protected override void OnClick()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			workList.GoPrevious();
			workList.Current?.SetVisited();
			QueuedTask.Run(() => WorkListTrialsModule.Current.Refresh());
		}
	}

	internal class SplitButtonTestListNav_GoFirst : Button
	{
		protected override void OnClick()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			workList.GoFirst();
			workList.Current?.SetVisited();
			QueuedTask.Run(() => WorkListTrialsModule.Current.Refresh());
		}
	}
}
