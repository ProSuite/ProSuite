using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList;

public abstract class DbWorkListEnvironmentBase : WorkEnvironmentBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected IWorkListItemDatastore WorkListItemDatastore { get; }

	protected DbWorkListEnvironmentBase(
		[CanBeNull] IWorkListItemDatastore workListItemDatastore)
	{
		// TODO: Separate hierarchies for db-worklists vs memory-worklists
		WorkListItemDatastore = workListItemDatastore;

		if (WorkListItemDatastore != null &&
		    ! WorkListItemDatastore.Validate(out string message))
		{
			throw new ArgumentException($"Invalid issue datastore: {message}");
		}
	}

	protected override async Task<IList<Table>> PrepareReferencedTables()
	{
		IList<Table> dbTables = GetTablesCore().ToList();

		if (dbTables.Count > 0 && WorkListItemDatastore != null)
		{
			dbTables = await WorkListItemDatastore.PrepareTableSchema(dbTables);
		}

		return dbTables;
	}

	protected override async Task<bool> TryPrepareSchemaCoreAsync()
	{
		if (WorkListItemDatastore == null)
		{
			return false;
		}

		return await WorkListItemDatastore.TryPrepareSchema();
	}

	public override void LoadAssociatedLayers()
	{
		AddToMapCore(GetTablesCore());
	}

	public override bool IsSameWorkListDefinition(string existingDefinitionFilePath)
	{
		string suggestedWorkListName = Assert.NotNull(SuggestWorkListName());

		return IsSameWorkListDefinition(existingDefinitionFilePath, suggestedWorkListName);
	}

	protected void AddToMapCore(IEnumerable<Table> tables)
	{
		ILayerContainerEdit layerContainer = GetLayerContainerCore<ILayerContainerEdit>();

		foreach (var table in tables)
		{
			_msg.DebugFormat("Adding table {0} to map...", table.GetName());

			if (table is FeatureClass fc)
			{
				FeatureLayer featureLayer =
					LayerFactory.Instance.CreateLayer<FeatureLayer>(
						new FeatureLayerCreationParams(fc), layerContainer);

				if (featureLayer == null)
				{
					_msg.DebugFormat("Created layer is null! Trying again...");
					Thread.Sleep(500);
					featureLayer =
						LayerFactory.Instance.CreateLayer<FeatureLayer>(
							new FeatureLayerCreationParams(fc), layerContainer);
				}

				// See DPS/#80: Sometimes a non-reproducible null layer results from the previous method.
				Assert.NotNull(featureLayer,
				               $"The feature layer for {table.GetName()} could not be created. Please try again.");

				featureLayer.SetExpanded(false);
				featureLayer.SetVisibility(false);
				featureLayer.SetDefinitionQuery(GetDefaultDefinitionQuery(table));

				// TODO: Support lyrx files as symbol layers.
				// So far, just make the symbols red:
				CIMSimpleRenderer renderer =
					featureLayer.GetRenderer() as CIMSimpleRenderer;

				if (renderer != null)
				{
					CIMSymbolReference symbol = renderer.Symbol;
					symbol.Symbol.SetColor(new CIMRGBColor() { R = 250 });
					featureLayer.SetRenderer(renderer);
				}

				continue;
			}

			IStandaloneTableContainerEdit tableContainer =
				layerContainer as IStandaloneTableContainerEdit ?? MapView.Active.Map;

			StandaloneTableFactory.Instance.CreateStandaloneTable(
				new StandaloneTableCreationParams(table), tableContainer);
		}
	}

	protected virtual string GetDefaultDefinitionQuery(Table table)
	{
		return null;
	}

	protected void RemoveFromMapCore(IEnumerable<Table> tables)
	{
		// Search inside the QA group layer for the tables to remove (to allow for renaming)
		ILayerContainerEdit layerContainer = GetLayerContainerCore<ILayerContainerEdit>();

		var tableList = tables.ToList();

		var layersToRemove = new List<MapMember>();
		foreach (MapMember basicFeatureLayer in GetAssociatedLayers(layerContainer, tableList))
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
		[NotNull] ILayerContainer layerContainer,
		[NotNull] List<Table> associatedTables)
	{
		foreach (Layer layer in layerContainer.GetLayersAsFlattenedList())
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

		IStandaloneTableContainerEdit tableContainer =
			layerContainer as IStandaloneTableContainerEdit ?? MapView.Active.Map;

		foreach (StandaloneTable standaloneTable in tableContainer.StandaloneTables)
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
		if (WorkListItemDatastore == null)
		{
			return Enumerable.Empty<Table>();
		}

		return WorkListItemDatastore.GetTables();
	}

	protected static bool IsSameWorkListDefinition(
		[NotNull] string existingDefinitionFilePath,
		[NotNull] string suggestedNewWorkListName)
	{
		string suggestedFileName =
			FileSystemUtils.ReplaceInvalidFileNameChars(suggestedNewWorkListName, '_');

		string existingFileName = Path.GetFileNameWithoutExtension(existingDefinitionFilePath);

		return existingFileName.Equals(suggestedFileName);
	}
}