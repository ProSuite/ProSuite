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
using ProSuite.Commons.AGP.Core.Geodatabase;

namespace ProSuite.AGP.WorkList.Selection
{
	public abstract class SelectionWorkListEnvironmentBase : WorkEnvironmentBase
	{
		public override string FileSuffix => ".swl";

		protected override T GetContainerCore<T>()
		{
			return MapView.Active.Map as T;
		}

		protected override void AddToMapCore(IEnumerable<Table> tables) { }

		protected override IEnumerable<Table> GetTablesCore()
		{
			return Enumerable.Empty<FeatureClass>();
		}

		protected override async Task<Table> EnsureStatusFieldCoreAsync(Table table)
		{
			return await Task.FromResult(table);
		}

		protected override IRepository CreateStateRepositoryCore(string path, string workListName)
		{
			Type type = GetWorkListTypeCore<SelectionWorkList>();

			return new XmlSelectionItemStateRepository(path, workListName, type);
		}

		protected override IWorkItemRepository CreateItemRepositoryCore(
			IEnumerable<Table> tables, IRepository stateRepository)
		{
			// todo daro inline
			Dictionary<MapMember, List<long>> oidsByLayer =
				MapView.Active.Map.GetSelection().ToDictionary();

			Dictionary<Table, List<long>> selection =
				MapUtils.GetDistinctSelectionByTable(oidsByLayer);

			return new SelectionItemRepository(DatasetUtils.Distinct(selection.Keys),
			                                   selection,
			                                   stateRepository);
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository,
		                                                string uniqueName,
		                                                string displayName)
		{
			return new SelectionWorkList(repository, uniqueName, displayName);
		}
	}
}
