using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Application.Configuration;
using ProSuite.Commons.AGP.Carto;

namespace ProSuite.AGP.Solution.WorkLists
{
	public class InMemoryWorkEnvironment : WorkEnvironmentBase
	{
		private readonly string _templateLayer = "Selection Work List.lyrx";

		public override string FileSuffix => ".swl";

		protected override IEnumerable<BasicFeatureLayer> GetLayers(Map map)
		{
			Dictionary<MapMember, List<long>> selection = map.GetSelection();

			return selection.Count >= 1
				       ? selection.Keys.OfType<BasicFeatureLayer>()
				       : Enumerable.Empty<BasicFeatureLayer>();
		}

		protected override async Task<BasicFeatureLayer> EnsureStatusFieldCoreAsync(
			BasicFeatureLayer featureLayer)
		{
			return await Task.FromResult(featureLayer);
		}

		protected override IRepository CreateStateRepositoryCore(string path, string workListName)
		{
			Type type = GetWorkListTypeCore<SelectionWorkList>();

			return new XmlWorkItemStateRepository(path, workListName, type);
		}

		protected override IWorkItemRepository CreateItemRepositoryCore(
			IEnumerable<BasicFeatureLayer> featureLayers, IRepository stateRepository)
		{
			List<BasicFeatureLayer> layers = featureLayers.ToList();

			Dictionary<Geodatabase, List<Table>> tables = MapUtils.GetDistinctTables(layers);
			Dictionary<Table, List<long>> selection = MapUtils.GetDistinctSelectionByTable(layers);

			return new SelectionItemRepository(tables, selection, stateRepository);
		}

		protected override LayerDocument GetLayerDocumentCore()
		{
			string path = ConfigurationUtils.GetConfigFilePath(_templateLayer);

			return LayerUtils.CreateLayerDocument(path);
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository, string name)
		{
			return new SelectionWorkList(repository, name);
		}
	}
}
