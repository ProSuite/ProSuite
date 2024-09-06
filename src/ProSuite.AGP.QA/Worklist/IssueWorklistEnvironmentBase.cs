using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.QA.WorkList
{
	public abstract class IssueWorkListEnvironmentBase : WorkEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IWorkListItemDatastore _workListItemDatastore;

		protected IssueWorkListEnvironmentBase(
			[CanBeNull] IWorkListItemDatastore workListItemDatastore)
		{
			// TODO: Separate hierarchies for db-worklists vs memory-worklists
			_workListItemDatastore = workListItemDatastore;

			if (_workListItemDatastore != null &&
			    ! _workListItemDatastore.Validate(out string message))
			{
				throw new ArgumentException($"Invalid issue datastore: {message}");
			}
		}

		protected IssueWorkListEnvironmentBase([CanBeNull] string path)
		{
			_workListItemDatastore = new FileGdbIssueWorkListItemDatastore(path);
		}

		public override string FileSuffix => ".iwl";

		public Geometry AreaOfInterest { get; set; }

		protected override async Task<IList<Table>> PrepareReferencedTables()
		{
			IList<Table> dbTables = GetTablesCore().ToList();

			if (dbTables.Count > 0 && _workListItemDatastore != null)
			{
				dbTables = await _workListItemDatastore.PrepareTableSchema(dbTables);
			}

			return dbTables;
		}

		protected override async Task<bool> TryPrepareSchemaCoreAsync()
		{
			if (_workListItemDatastore == null)
			{
				return false;
			}

			return await _workListItemDatastore.TryPrepareSchema();
		}

		public override void LoadAssociatedLayers()
		{
			AddToMapCore(GetTablesCore());
		}

		public override void RemoveAssociatedLayers()
		{
			RemoveFromMapCore(GetTablesCore());
		}

		protected override T GetLayerContainerCore<T>()
		{
			var groupLayerName = "QA";

			GroupLayer groupLayer = MapView.Active.Map.FindLayers(groupLayerName)
			                               .OfType<GroupLayer>().FirstOrDefault();

			if (groupLayer == null)
			{
				_msg.DebugFormat("Creating new group layer {0}", groupLayerName);
				return
					LayerFactory.Instance.CreateGroupLayer(
						MapView.Active.Map, 0, groupLayerName) as T;
			}

			return groupLayer as T;
		}

		private void AddToMapCore(IEnumerable<Table> tables)
		{
			var groupLayer = GetLayerContainerCore<GroupLayer>();

			foreach (var table in tables)
			{
				_msg.DebugFormat("Adding table {0} to map...", table.GetName());

				if (table is FeatureClass fc)
				{
					FeatureLayer featureLayer =
						LayerFactory.Instance.CreateLayer<FeatureLayer>(
							new FeatureLayerCreationParams(fc), groupLayer);

					if (featureLayer == null)
					{
						_msg.DebugFormat("Created layer is null! Trying again...");
						Thread.Sleep(500);
						featureLayer =
							LayerFactory.Instance.CreateLayer<FeatureLayer>(
								new FeatureLayerCreationParams(fc), groupLayer);
					}

					// See DPS/#80: Sometimes a non-reproducible null layer results from the previous method.
					Assert.NotNull(featureLayer,
					               $"The feature layer for {table.GetName()} could not be created. Please try again.");

					featureLayer.SetExpanded(false);
					featureLayer.SetVisibility(false);
					featureLayer.SetDefinitionQuery(GetDefaultDefinitionQuery(table));

					// TODO: Support lyrx files as symbol layers.
					// So far, just make the symbols red:
					CIMSimpleRenderer renderer = featureLayer.GetRenderer() as CIMSimpleRenderer;

					if (renderer != null)
					{
						CIMSymbolReference symbol = renderer.Symbol;
						symbol.Symbol.SetColor(new CIMRGBColor() { R = 250 });
						featureLayer.SetRenderer(renderer);
					}

					continue;
				}

				StandaloneTableFactory.Instance.CreateStandaloneTable(
					new StandaloneTableCreationParams(table), groupLayer);
			}
		}

		protected virtual string GetDefaultDefinitionQuery(Table table)
		{
			return null;
		}

		private void RemoveFromMapCore(IEnumerable<Table> tables)
		{
			// Search inside the QA group layer for the tables to remove (to allow for renaming)
			GroupLayer groupLayer = GetLayerContainerCore<GroupLayer>();

			var tableList = tables.ToList();

			var layersToRemove = new List<MapMember>();
			foreach (MapMember basicFeatureLayer in GetAssociatedLayers(groupLayer, tableList))
			{
				layersToRemove.Add(basicFeatureLayer);
			}

			QueuedTask.Run(() =>
			{
				Map activeMap = MapUtils.GetActiveMap();

				activeMap.RemoveLayers(layersToRemove
				                       .Where(mm => mm is Layer)
				                       .Cast<Layer>());

				activeMap.RemoveStandaloneTables(layersToRemove
				                                 .Where(mm => mm is StandaloneTable)
				                                 .Cast<StandaloneTable>());
			});
		}

		private static IEnumerable<MapMember> GetAssociatedLayers(
			[NotNull] GroupLayer groupLayer,
			[NotNull] List<Table> associatedTables)
		{
			foreach (Layer layer in groupLayer.Layers)
			{
				if (layer is not BasicFeatureLayer featureLayer)
				{
					continue;
				}

				FeatureClass layerClass = featureLayer.GetFeatureClass();

				foreach (Table table in associatedTables)
				{
					if (DatasetUtils.IsSameTable(table, layerClass))
					{
						yield return featureLayer;
					}
				}
			}

			foreach (StandaloneTable standaloneTable in groupLayer.StandaloneTables)
			{
				Table table = standaloneTable.GetTable();

				if (associatedTables.Any(t => DatasetUtils.IsSameTable(t, table)))
				{
					yield return standaloneTable;
				}
			}
		}

		protected virtual IEnumerable<Table> GetTablesCore()
		{
			if (_workListItemDatastore == null)
			{
				return Enumerable.Empty<Table>();
			}

			return _workListItemDatastore.GetTables();
		}

		protected override IWorkList CreateWorkListCore(IWorkItemRepository repository,
		                                                string uniqueName,
		                                                string displayName)
		{
			return new IssueWorkList(repository, uniqueName, AreaOfInterest, displayName);
		}

		protected override IWorkItemStateRepository CreateStateRepositoryCore(
			string path, string workListName)
		{
			Type type = GetWorkListTypeCore<IssueWorkList>();

			return new XmlWorkItemStateRepository(path, workListName, type);
		}

		protected override IWorkItemRepository CreateItemRepositoryCore(
			IList<Table> tables, IWorkItemStateRepository stateRepository)
		{
			Stopwatch watch = Stopwatch.StartNew();

			var sourceClasses = new List<Tuple<Table, string>>();

			foreach (Table table in tables)
			{
				string defaultDefinitionQuery = GetDefaultDefinitionQuery(table);

				sourceClasses.Add(Tuple.Create(table, defaultDefinitionQuery));
			}

			var result =
				new IssueItemRepository(sourceClasses, stateRepository, _workListItemDatastore);

			_msg.DebugStopTiming(watch, "Created issue work item repository");

			return result;
		}
	}
}
