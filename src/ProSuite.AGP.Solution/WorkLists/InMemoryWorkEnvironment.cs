using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Carto;

namespace ProSuite.AGP.Solution.WorkLists
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
			List<BasicFeatureLayer> layers = featureLayers.ToList();

			Dictionary<Geodatabase, List<Table>> tables = MapUtils.GetDistinctTables(layers);
			Dictionary<Table, List<long>> selection = MapUtils.GetDistinctSelectionByTable(layers);

			return new SelectionItemRepository(tables, selection);
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository)
		{
			return new ProSuite.AGP.WorkList.Domain.SelectionWorkList(repository, _workListName);
		}
	}
}
