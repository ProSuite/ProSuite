using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList.Selection
{
	public abstract class SelectionWorkListEnvironmentBase : WorkEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override string FileSuffix => ".swl";

		protected override string GetDisplayName()
		{
			string currentName = Path.GetFileNameWithoutExtension(Project.Current.Name);
			var now = DateTime.Now.ToString("yyyy_MM_dd_HHmmss");

			return $"{currentName}_{now}";
		}

		protected override T GetLayerContainerCore<T>()
		{
			return MapView.Active.Map as T;
		}

		protected override IWorkItemStateRepository CreateStateRepositoryCore(
			string path, string workListName)
		{
			Type type = GetWorkListTypeCore<SelectionWorkList>();

			return new XmlSelectionItemStateRepository(path, workListName, type);
		}

		protected override Task<IWorkItemRepository> CreateItemRepositoryCore(
			IWorkItemStateRepository stateRepository)
		{
			Map map = MapView.Active.Map;

			string path = stateRepository.WorkListDefinitionFilePath;

			if (File.Exists(path))
			{
				XmlWorkListDefinition definition = XmlWorkItemStateRepository.Import(path);

				var tables = FindMatchingTables(map, definition).ToList();

				if (tables.Count == 0)
				{
					// TODO: (daro) show work list display name?
					_msg.Debug($"There are no referenced table from '{path}' in the map");

					var message = $"There are no referenced table from '{Path.GetFileName(path)}' in the map";
					var caption = "Cannot open work List";

					Gateway.ShowMessage(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);

					return Task.FromResult<IWorkItemRepository>(null);
				}

				return Task.FromResult(WorkListUtils.CreateSelectionItemRepository(tables, stateRepository, definition));
			}
			else
			{
				Dictionary<MapMember, List<long>> oidsByLayer = SelectionUtils.GetSelection(map);

				Dictionary<Table, List<long>> selection =
					MapUtils.GetDistinctSelectionByTable(oidsByLayer);

				return Task.FromResult<IWorkItemRepository>(
					new SelectionItemRepository(selection, stateRepository));
			}
		}

		private static IEnumerable<Table> FindMatchingTables(
			Map map, XmlWorkListDefinition definition)
		{
			var featureLayers = MapUtils.GetFeatureLayers<BasicFeatureLayer>(map).ToList();

			foreach (XmlTableReference tableReference in definition.Workspaces.SelectMany(w => w.Tables))
			{
				foreach (BasicFeatureLayer layer in featureLayers)
				{
					Table table = layer.GetTable();

					var tableId = new GdbTableIdentity(table);
					long id = WorkListUtils.GetUniqueTableIdAcrossWorkspaces(tableId);

					if (id == tableReference.Id)
					{
						yield return table;
					}
				}
			}
		}

		public override bool IsSameWorkListDefinition(string existingDefinitionFile)
		{
			// We currently cannot compare the current selection with the one in the file
			// so for the time being, always make a new one.
			return false;
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository,
		                                                string uniqueName,
		                                                string displayName)
		{
			return new SelectionWorkList(repository, uniqueName, displayName);
		}
	}
}
