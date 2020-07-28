using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.AGP.MainSolution
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
}
