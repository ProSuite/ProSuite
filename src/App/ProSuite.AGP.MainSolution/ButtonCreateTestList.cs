using System;
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
			const string pluginIdentifier = "ProSuite_WorkList_Datasource";

			var workList = Module1.Current.GetTestWorklist();
			var workListName = workList.Name;

			//workList.GoFirst();
			//workList.Current?.SetVisited(true);
			//workList.GoNext();
			//workList.Current?.SetStatus(WorkItemStatus.Done);

			var datasourcePath = new Uri(new Uri("worklist://localhost/"), workListName);
			var connector = new PluginDatasourceConnectionPath(pluginIdentifier, datasourcePath);

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
