using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.DomainModel.DataModel;

namespace Clients.AGP.ProSuiteSolution.WorkLists
{
	public class InMemoryWorkEnvironment : WorkEnvironmentBase
	{
		private readonly string _workListName = "Selection Work List";
		private readonly string _templateLayer = "Selection Work List.lyrx";

		protected override void ShowWorkListCore(IWorkList workList, LayerDocument layerTemplate)
		{
			WorkListsModule.Current.Show(workList, layerTemplate);
		}

		protected override IEnumerable<BasicFeatureLayer> GetLayers(Map map)
		{
			Dictionary<MapMember, List<long>> selection = map.GetSelection();

			return selection.Count >= 1
				       ? selection.Keys.OfType<BasicFeatureLayer>()
					   : Enumerable.Empty<BasicFeatureLayer>();
		}

		protected override BasicFeatureLayer EnsureMapContainsLayerCore(BasicFeatureLayer featureLayer)
		{
			// we want every feature layer
			return featureLayer;
		}

		protected override LayerDocument GetLayerDocumentCore()
		{
			string path = ConfigurationUtils.GetConfigFilePath(_templateLayer);

			LayerDocument layerDocument = LayerUtils.CreateLayerDocument(path);
			// todo daro: inline
			return layerDocument;
		}

		protected override IWorkItemRepository CreateRepositoryCore(IEnumerable<BasicFeatureLayer> featureLayers)
		{
			// todo daro: refactor!!!
			Dictionary<GdbTableIdentity, List<long>> selectionByTable = MapUtils.GetDistinctSelectionByTable(featureLayers, out IEnumerable<GdbWorkspaceIdentity> distinctWorkspaces);

			IEnumerable<IWorkspaceContext> workspaces = GetWorkspaceContexts(distinctWorkspaces);

			// todo daro: rafactor SelectionItemRepository(Dictionary<IWorkspaceContext, GdbTableIdentity>, Dictionary<GdbTableIdentity, List<long>>)
			ISelectionItemRepository repository = new SelectionItemRepository(workspaces);
			repository.RegisterDatasets(selectionByTable);

			return repository;
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository)
		{
			return new ProSuite.AGP.WorkList.Domain.SelectionWorkList(repository, _workListName);
		}
	}
}
