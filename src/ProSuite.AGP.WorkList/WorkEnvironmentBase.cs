using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	public abstract class WorkEnvironmentBase
	{
		public void CreateWorkList()
		{
			Map map = MapView.Active.Map;

			IEnumerable<BasicFeatureLayer> featureLayers = GetLayers(map).Select(EnsureMapContainsLayerCore);

			IWorkItemRepository repository = CreateRepositoryCore(featureLayers);

			// todo daro: dispose work list to free memory !!!!
			IWorkList workList = CreateWorkListCore(repository);

			LayerDocument layerTemplate = GetLayerDocumentCore();

			ShowWorkListCore(workList, layerTemplate);
		}

		protected abstract void ShowWorkListCore(IWorkList workList, LayerDocument template);

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

		//private void AddLayer(string workListName)
		//{
		//	Uri uri = GetUri(workListName);
		//	//NOTE useless, does not work
		//	FeatureLayerCreationParams layerCreationParams =
		//		LayerUtils.CreateLayerParams(uri, layerDocument);

		//	var layer =
		//		LayerFactory.Instance.CreateLayer<FeatureLayer>(layerCreationParams,
		//		                                                MapView.Active.Map,
		//		                                                LayerPosition.AddToTop);
		//}

		// todo daro: move to module?
		protected abstract LayerDocument GetLayerDocumentCore();
	}
}
