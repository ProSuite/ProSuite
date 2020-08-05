using System.Collections.Generic;
using System.Linq;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.DomainModel.DataModel;

namespace ProSuite.AGP.WorkList
{
	public class InMemoryWorkEnvironment : WorkEnvironmentBase
	{
		const string _workListName = "Selection Work List";

		protected override IEnumerable<BasicFeatureLayer> GetLayers()
		{
			Map map = MapView.Active.Map;

			Dictionary<MapMember, List<long>> selection = map.GetSelection();

			if (selection.Count < 1)
			{
				yield break;
			}

			foreach (BasicFeatureLayer featureLayer in selection
			                                           .Keys.OfType<BasicFeatureLayer>()
			                                           .Select(EnsureFeatureLayerCore))
			{
				yield return featureLayer;
			}
		}

		protected override BasicFeatureLayer EnsureFeatureLayerCore(BasicFeatureLayer featureLayer)
		{
			return featureLayer;
		}

		protected override IWorkItemRepository CreateRepositoryCore(IEnumerable<BasicFeatureLayer> featureLayers)
		{
			Dictionary<GdbTableIdentity, List<long>> selectionByTable =
				MapUtils.GetDistinctSelectionByTable(featureLayers, out IEnumerable<GdbWorkspaceIdentity> distinctWorkspaces);

			IEnumerable<IWorkspaceContext> workspaces = GetWorkspaceContexts(distinctWorkspaces);

			ISelectionItemRepository repository = new SelectionItemRepository(workspaces);
			repository.RegisterDatasets(selectionByTable);

			return repository;
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository)
		{
			return CreateWorkList(repository, _workListName);
		}
	}
}
