using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	// todo daro: refactor!!!
	public abstract class WorkEnvironmentBase
	{
		public IWorkList CreateWorkList()
		{
			Map map = MapView.Active.Map;

			IEnumerable<BasicFeatureLayer> featureLayers = GetLayers(map).Select(EnsureMapContainsLayerCore);

			IWorkItemRepository repository = CreateRepositoryCore(featureLayers);

			// todo daro: dispose work list to free memory !!!!
			IWorkList workList = CreateWorkListCore(repository);
			
			// todo daro: refactor!!!
			LayerDocument layerTemplate = GetLayerDocumentCore();

			ShowWorkListCore(workList, layerTemplate);

			return workList;
		}

		protected abstract void ShowWorkListCore(IWorkList workList, LayerDocument layerTemplate);

		protected abstract IEnumerable<BasicFeatureLayer> GetLayers(Map map);

		protected abstract IWorkList CreateWorkListCore(IWorkItemRepository repository);

		protected abstract IWorkItemRepository CreateRepositoryCore(
			IEnumerable<BasicFeatureLayer> featureLayers);

		protected abstract BasicFeatureLayer EnsureMapContainsLayerCore(BasicFeatureLayer featureLayer);

		protected static IEnumerable<IWorkspaceContext> GetWorkspaceContexts(
			IEnumerable<GdbWorkspaceIdentity> distinctWorkspaces)
		{
			return distinctWorkspaces.Select(dws => (IWorkspaceContext) new WorkspaceContext(dws));
		}

		protected abstract LayerDocument GetLayerDocumentCore();

		#region trials

		//[CanBeNull]
		//private FeatureLayerCreationParams CreateLayer([NotNull] string workListName)
		//{
		//	FeatureLayerCreationParams result = null;
		//	PluginDatastore datastore = null;
		//	Table table = null;

		//	try
		//	{
		//		PluginDatasourceConnectionPath connector = GetWorkListConnectionPath(workListName);

		//		datastore = new PluginDatastore(connector);
		//		table = datastore.OpenTable(workListName);

		//		result = LayerUtils.CreateLayerParams((FeatureClass) table);
		//	}
		//	catch (Exception exception)
		//	{
		//		Console.WriteLine(exception);
		//	}
		//	finally
		//	{
		//		datastore?.Dispose();
		//		table?.Dispose();
		//	}

		//	return result;
		//}

		//private void AddLayer(string workListName)
		//{
		//	Uri uri = GetUri(workListName);
		//	//NOTE useless, does not work
		//	FeatureLayerCreationParams layerCreationParams =
		//		LayerUtils.CreateLayerParams(uri, layerDocument);

		//	var layer =
		//		LayerFactory.Instance.CreateLayer<FeatureLayer>(layerCreationParams,
		//														MapView.Active.Map,
		//														LayerPosition.AddToTop);
		//}

		#endregion

		protected static Type GetWorkListTypeCore<T>() where T : IWorkList
		{
			return typeof(T);
		}

		protected IRepository CreateStateRepository(string path, string workListName)
		{
			return CreateStateRepositoryCore(path, workListName);
		}

		protected abstract IRepository CreateStateRepositoryCore(string path, string workListName);
	}
}
