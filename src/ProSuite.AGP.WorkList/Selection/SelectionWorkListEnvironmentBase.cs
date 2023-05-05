using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Carto;

namespace ProSuite.AGP.WorkList.Selection
{
	public abstract class SelectionWorkListEnvironmentBase : WorkEnvironmentBase
	{
		public override string FileSuffix => ".swl";

		protected override ILayerContainerEdit GetContainer()
		{
			return MapView.Active.Map;
		}

		protected override IEnumerable<BasicFeatureLayer> GetLayersCore(
			IEnumerable<FeatureClass> featureClasses)
		{
			MapView mapView = MapView.Active;

			if (mapView == null)
			{
				return Enumerable.Empty<BasicFeatureLayer>();
			}

			Dictionary<MapMember, List<long>> selection = mapView.Map.GetSelection();

			return selection.Count >= 1
				       ? selection.Keys.OfType<BasicFeatureLayer>()
				       : Enumerable.Empty<BasicFeatureLayer>();
		}

		protected override IEnumerable<FeatureClass> GetFeatureClassesCore()
		{
			return Enumerable.Empty<FeatureClass>();
		}

		protected override async Task<FeatureClass> EnsureStatusFieldCoreAsync(
			FeatureClass featureClass)
		{
			return await Task.FromResult(featureClass);
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

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository,
		                                                string uniqueName,
		                                                string displayName)
		{
			return new SelectionWorkList(repository, uniqueName, displayName);
		}
	}
}
