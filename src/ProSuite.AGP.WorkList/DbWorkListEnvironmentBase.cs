using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList;

public abstract class DbWorkListEnvironmentBase : WorkEnvironmentBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull]
	protected IWorkListItemDatastore WorkListItemDatastore { get; }

	protected DbWorkListEnvironmentBase(
		[NotNull] IWorkListItemDatastore workListItemDatastore)
	{
		Assert.ArgumentNotNull(workListItemDatastore, nameof(workListItemDatastore));

		// TODO: Separate hierarchies for db-worklists vs memory-worklists
		WorkListItemDatastore = workListItemDatastore;

		// TODO: Flag? Explicit environment validation method? 
		if (QueuedTask.OnWorker)
		{
			if (! WorkListItemDatastore.Validate(out string message))
			{
				throw new ArgumentException($"Invalid issue datastore: {message}");
			}
		}
	}

	protected async Task<IList<Table>> PrepareReferencedTables()
	{
		IList<Table> dbTables = GetTablesCore().ToList();

		if (dbTables.Count > 0)
		{
			dbTables = await WorkListItemDatastore.PrepareTableSchema(dbTables);
		}

		return dbTables;
	}

	protected override async Task<bool> TryPrepareSchemaCoreAsync()
	{
		return await WorkListItemDatastore.TryPrepareSchema();
	}

	public override void LoadAssociatedLayers(MapView mapView, IWorkList worklist)
	{
		AddToMapCore(mapView, GetTablesCore(), worklist);
	}

	// TODO: (daro) move to subclass because of IssueWorkList specific code: Attributes.IssueDescription
	protected virtual void AddToMapCore([NotNull] MapView mapView,
	                                    [NotNull] IEnumerable<Table> tables,
	                                    [NotNull] IWorkList worklist)
	{
		ILayerContainerEdit layerContainer = GetLayerContainerCore<ILayerContainerEdit>(mapView);

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
				featureLayer.SetSelectable(false);

				string defaultDefinitionQuery = GetDefaultDefinitionQuery(table);

				if (! string.IsNullOrEmpty(defaultDefinitionQuery))
				{
					featureLayer.SetDefinitionQuery(defaultDefinitionQuery);
				}

#if ARCGISPRO_GREATER_3_2
				featureLayer.SetShowLayerAtAllScales(true);
#endif
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

				// NOTE: Currently the tables are supposed to all reside in the same
				//       workspace (which is certainly the case for Issue Worklists).
				//       Therefore, we can use the table ID as a unique identifier.
				IAttributeReader attributeReader = worklist.GetAttributeReader(table.GetID());

				// NOTE: SetDisplyField is slow. In future the pr-prepared layers are stored and used.
				//       They are not going to be created by code.
				string name = attributeReader.GetName(Attributes.IssueDescription);
				LayerUtils.SetDisplayExpression(featureLayer, Assert.NotNull(name));

				continue;
			}

			IStandaloneTableContainerEdit tableContainer =
				layerContainer as IStandaloneTableContainerEdit ?? MapView.Active.Map;

			StandaloneTableFactory.Instance.CreateStandaloneTable(
				new StandaloneTableCreationParams(table), tableContainer);
		}
	}

	/// <summary>
	/// Creates the complete definition of the (invisible) map member for an associated table,
	/// such as an issue feature class. For feature classes a <see cref="CIMFeatureLayer"/> is
	/// returned, for tables a <see cref="CIMStandaloneTable"/>. All properties are set in the
	/// definition to avoid property changes after adding them to the map (see
	/// <see cref="WorkEnvironmentBase.CreateWorkListLayerDefinition"/>).
	/// </summary>
	[NotNull]
	protected CIMDefinition CreateAssociatedMapMemberDefinition([NotNull] Table table,
	                                                            [NotNull] IWorkList worklist)
	{
		string name;
		using (TableDefinition tableDefinition = table.GetDefinition())
		{
			string aliasName = tableDefinition.GetAliasName();
			name = string.IsNullOrEmpty(aliasName) ? table.GetName() : aliasName;
		}

		CIMDataConnection dataConnection = table.GetDataConnection();

		if (table is not FeatureClass featureClass)
		{
			return new CIMStandaloneTable
			       {
				       Name = name,
				       URI = CreateMapMemberUri(),
				       DataConnection = dataConnection,
				       AutoGenerateRowTemplates = false
			       };
		}

		// NOTE: Currently the tables are supposed to all reside in the same
		//       workspace, and we can use the table ID as a unique identifier.
		IAttributeReader attributeReader = worklist.GetAttributeReader(table.GetID());
		string displayField = Assert.NotNull(attributeReader.GetName(Attributes.IssueDescription));

		GeometryType shapeType;
		using (FeatureClassDefinition definition = featureClass.GetDefinition())
		{
			shapeType = definition.GetShapeType();
		}

		var featureTable = new CIMFeatureTable
		                   {
			                   DataConnection = dataConnection,
			                   DisplayExpressionInfo =
				                   new CIMExpressionInfo { Expression = displayField }
		                   };

		string definitionQuery = GetDefaultDefinitionQuery(table);

		if (! string.IsNullOrEmpty(definitionQuery))
		{
			// Same structure as created by FeatureLayer.SetDefinitionQuery():
			string queryName = GetDefaultDefinitionQueryName();

			featureTable.DefinitionExpression = definitionQuery;
			featureTable.DefinitionExpressionName = queryName;
			featureTable.DefinitionFilterChoices = new CIMDefinitionFilter[]
			                                       {
				                                       new()
				                                       {
					                                       Name = queryName,
					                                       DefinitionExpression = definitionQuery
				                                       }
			                                       };
		}

		var result = new CIMFeatureLayer
		             {
			             Name = name,
			             URI = CreateMapMemberUri(),
			             Visibility = false,
			             Expanded = false,
			             ShowLegends = true,
			             Selectable = false,
			             FeatureTable = featureTable,
			             // TODO: Support lyrx files as symbol layers.
			             // So far, just make the symbols red:
			             Renderer = new CIMSimpleRenderer
			                        {
				                        Symbol = CreateAssociatedLayerSymbol(shapeType)
					                        .MakeSymbolReference()
			                        },
			             AutoGenerateFeatureTemplates = false
		             };

#if ARCGISPRO_GREATER_3_2
		result.LayerScaleVisibilityOptions =
			new CIMLayerScaleVisibilityOptions { ShowLayerAtAllScales = true };
#endif

		return result;
	}

	/// <summary>
	/// The drawing order of associated layers, from top (lowest value) to bottom, as it results
	/// from adding the layers one by one using the automatic arrangement of Pro: points, lines,
	/// multipatches, polygons.
	/// </summary>
	protected static int GetDrawingOrder([NotNull] Table table)
	{
		if (table is not FeatureClass featureClass)
		{
			return int.MaxValue;
		}

		using FeatureClassDefinition definition = featureClass.GetDefinition();

		switch (definition.GetShapeType())
		{
			case GeometryType.Point:
			case GeometryType.Multipoint:
				return 0;
			case GeometryType.Polyline:
				return 1;
			case GeometryType.Multipatch:
				return 2;
			default:
				return 3;
		}
	}

	[NotNull]
	private static CIMSymbol CreateAssociatedLayerSymbol(GeometryType shapeType)
	{
		var red = new CIMRGBColor { R = 250 };

		switch (shapeType)
		{
			case GeometryType.Point:
			case GeometryType.Multipoint:
				return SymbolFactory.Instance.ConstructPointSymbol(red);
			case GeometryType.Polyline:
				return SymbolFactory.Instance.ConstructLineSymbol(red);
			default:
				// Multipatch layers are also drawn with polygon symbols in maps
				return SymbolFactory.Instance.ConstructPolygonSymbol(red);
		}
	}

	protected virtual string GetDefaultDefinitionQuery(Table table)
	{
		return null;
	}

	/// <summary>
	/// The name of the default definition query (see <see cref="GetDefaultDefinitionQuery"/>),
	/// as shown in the layer properties.
	/// </summary>
	[NotNull]
	protected virtual string GetDefaultDefinitionQueryName()
	{
		return "Work List Filter";
	}

	protected void RemoveFromMapCore(MapView mapView, IEnumerable<Table> tables)
	{
		// Search inside the QA group layer for the tables to remove (to allow for renaming)
		ILayerContainerEdit layerContainer = GetLayerContainerCore<ILayerContainerEdit>(mapView);

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
		return WorkListItemDatastore.GetTables();
	}
}
