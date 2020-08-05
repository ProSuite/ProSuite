using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	public abstract class WorkEnvironmentBase
	{
		public void OpenWorkList()
		{
			List<BasicFeatureLayer> featureLayers = GetLayers().ToList();

			IWorkItemRepository repository = CreateRepositoryCore(featureLayers);

			IWorkList workList = CreateWorkListCore(repository);

			AddLayer(workList.Name);
		}

		protected abstract IEnumerable<BasicFeatureLayer> GetLayers();

		protected abstract IWorkList CreateWorkListCore(IWorkItemRepository repository);

		protected abstract IWorkItemRepository CreateRepositoryCore(
			IEnumerable<BasicFeatureLayer> featureLayers);

		protected IWorkList CreateWorkList(IWorkItemRepository repository, string workListName)
		{
			IWorkList list = WorkListRegistry.Instance.Get(workListName);
			if (list != null)
			{
				WorkListRegistry.Instance.Remove(workListName);
			}

			IWorkList workList = CreateWorkListCore(repository);
			WorkListRegistry.Instance.Add(workList);

			return workList;
		}

		protected static IEnumerable<IWorkspaceContext> GetWorkspaceContexts(
			IEnumerable<GdbWorkspaceIdentity> distinctWorkspaces)
		{
			return distinctWorkspaces.Select(dws => (IWorkspaceContext) new WorkspaceContext(dws));
		}

		private PluginDatasourceConnectionPath GetWorkListConnectionPath(string workListName)
		{
			const string pluginIdentifier = "ProSuite_WorkListDatasource";

			var baseUri = new Uri("worklist://localhost/");
			var datasourcePath = new Uri(baseUri, workListName);

			return new PluginDatasourceConnectionPath(pluginIdentifier, datasourcePath);
		}

		private void AddLayer(string workListName)
		{
			PluginDatasourceConnectionPath connector = GetWorkListConnectionPath(workListName);

			using (var datastore = new PluginDatastore(connector))
			{
				IReadOnlyList<string> tableNames = datastore.GetTableNames();
				foreach (string tableName in tableNames)
				{
					using (Table table = datastore.OpenTable(tableName))
					{
						LayerFactory.Instance.CreateFeatureLayer(
							(FeatureClass) table, MapView.Active.Map);
					}
				}
			}
		}

		protected abstract BasicFeatureLayer EnsureFeatureLayerCore(BasicFeatureLayer featureLayer);
	}
}
