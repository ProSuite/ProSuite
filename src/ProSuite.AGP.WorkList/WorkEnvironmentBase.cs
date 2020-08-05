using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	public abstract class WorkEnvironmentBase
	{
		// todo daro: rename. initialize?
		public void OpenSelectionWorkList()
		{
			Map map = MapView.Active.Map;

			Dictionary<MapMember, List<long>> selection = map.GetSelection();

			if (selection.Count < 1)
			{
				return;
			}

			IEnumerable<BasicFeatureLayer> featureLayers = EnsureLayersInWorkList(selection);

			Dictionary<GdbTableReference, List<long>> selectionByTable =
				MapUtils.GetDistinctSelectionByTable(featureLayers, out IEnumerable<GdbWorkspaceReference> distinctWorkspaces);

			ISelectionItemRepository repository = new SelectionItemRepository(distinctWorkspaces.Select(w => (IWorkspaceContext) new WorkspaceContext(w)));

			repository.RegisterDatasets(selectionByTable);

			const string workListName = "Selection Work List";

			IWorkList list = WorkListRegistry.Instance.Get(workListName);
			if (list != null)
			{
				WorkListRegistry.Instance.Remove(workListName);
			}

			var workList = new SelectionWorkList(repository, workListName);
			WorkListRegistry.Instance.Add(workList);

			PluginDatasourceConnectionPath connector = GetWorkListConnectionPath(workListName);

			using (var datastore = new PluginDatastore(connector))
			{
				var tableNames = datastore.GetTableNames();
				foreach (var tableName in tableNames)
				{
					using (var table = datastore.OpenTable(tableName))
					{
						LayerFactory.Instance.CreateFeatureLayer((FeatureClass) table, MapView.Active.Map);
					}
				}
			}
		}

		public PluginDatasourceConnectionPath GetWorkListConnectionPath(string workListName)
		{
			const string pluginIdentifier = "ProSuite_WorkListDatasource";

			var baseUri = new Uri("worklist://localhost/");
			var datasourcePath = new Uri(baseUri, workListName);

			return new PluginDatasourceConnectionPath(pluginIdentifier, datasourcePath);
		}

		private IWorkItemRepository CreateRepository(IEnumerable<IWorkspaceContext> workspaces)
		{
			IWorkItemRepository repository = new SelectionItemRepository(workspaces);
			return repository;
		}

		private IEnumerable<BasicFeatureLayer> EnsureLayersInWorkList(Dictionary<MapMember, List<long>> selection)
		{
			return selection.Keys.OfType<BasicFeatureLayer>().Select(EnsureFeatureLayerCore);
		}

		protected abstract BasicFeatureLayer EnsureFeatureLayerCore(BasicFeatureLayer featureLayer);

		//public void OpenErrorWorkList()
		//{
		//	Map map = MapView.Active.Map;

		//	IEnumerable<Layer> layers = map.GetLayersAsFlattenedList();

		//	var repository = new ErrorItemRepository();

		//	foreach (BasicFeatureLayer layer in layers.OfType<BasicFeatureLayer>())
		//	{
		//		Register(repository, layer);
		//	}
		//}
	}

	public abstract class DatabaseWorkEnvironment : WorkEnvironmentBase { }
}
