using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList;

// TODO: (daro) rename. Environment is long living.
/// <summary>
/// Encapsulates the logic (but no volatile state) for a work list type, including the creation
/// of the work list.
/// </summary>
public abstract class WorkEnvironmentBase : IWorkEnvironment
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected abstract string FileSuffix { get; }
	protected virtual string WorklistsFolder => "WorkLists";

	/// <summary>
	/// The unique name of the work list that corresponds to the file name in
	/// the Worklists folder of the project.
	/// TODO: Implement and use for all DbStatusWorkLists and Selection worklist
	/// </summary>
	protected string UniqueName { get; set; }

	public bool AllowBackgroundLoading { get; set; }

	public async Task UpdateConfigurationAsync(IWorkList workList)
	{
		// Consider separating volatile and one-time only state and make two methods
		ConfigureWorkList(workList);

		if (workList is DbStatusWorkList dbStatusWorkList)
		{
			dbStatusWorkList.UpdateDefinitionExpressions();
		}

		await QueuedTask.Run(() => workList.Invalidate());
	}

	protected virtual Geometry GetAreaOfInterest()
	{
		return null;
	}

	[ItemCanBeNull]
	public async Task<IWorkList> CreateWorkListAsync([NotNull] string uniqueName)
	{
		Assert.ArgumentNotNullOrEmpty(uniqueName, nameof(uniqueName));

		string directory = Path.Combine(Project.Current.HomeFolderPath, WorklistsFolder);

		if (! Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		string fileName = FileSystemUtils.ReplaceInvalidFileNameChars(GetDisplayName(), '_');

		string filePath = EnsureValidDefinitionFilePath(directory, fileName, FileSuffix);
		Assert.NotNull(filePath);

		if (File.Exists(filePath))
		{
			_msg.DebugFormat("Work list definition file {0} already exists", filePath);

			// Special handling (e.g. message box notifying the user) must have happened before.
			// TODO: Check that the state from the definition file is actually used.
			// NOTE: In case of DB Status work lists with changing table content the visited
			// state of the work list definition file is probably irrelevant or even incorrect
			// because the items (issues, revision points, etc.) regularly change in the
			// underlying DB table. In case a different extent / work unit has been loaded the
			// original items might not event be present anymore.

			// TODO:
			// We should probably delete the definition file when the layer is unloaded
			// because a new load could be in a completely different area (work unit).
			// Or design a simplified definition file that just contains the connection to the
			// underlying table(s), ideally along with the relevant status schema?
		}

		return await CreateWorkListAsync(uniqueName, filePath);
	}

	[ItemCanBeNull]
	public async Task<IWorkList> CreateWorkListAsync([NotNull] string uniqueName,
	                                                 [NotNull] string workListFile)
	{
		Assert.ArgumentNotNullOrEmpty(uniqueName, nameof(uniqueName));
		Assert.ArgumentNotNullOrEmpty(workListFile, nameof(workListFile));

		if (! await TryPrepareSchemaCoreAsync())
		{
			// null work list
			_msg.WarnFormat("Work list schema preparation failed for {0}", uniqueName);
			return await Task.FromResult(default(IWorkList));
		}

		var watch = Stopwatch.StartNew();

		string displayName = Path.GetFileNameWithoutExtension(workListFile);

		IWorkItemStateRepository stateRepository =
			CreateStateRepositoryCore(workListFile, uniqueName, displayName);

		stateRepository.LoadAllStates();

		_msg.DebugStopTiming(watch, "Created work list state repository in {0}",
		                     workListFile);

		IWorkItemRepository itemRepository =
			await QueuedTask.Run(async () =>
				                     await CreateItemRepositoryCoreAsync(stateRepository));

		if (itemRepository == null)
		{
			return await Task.FromResult<IWorkList>(null);
		}

		IWorkList result =
			Assert.NotNull(CreateWorkListCore(itemRepository, uniqueName, displayName));

		_msg.Debug($"Created {result}");

		ConfigureWorkList(result);

		if (result is DbStatusWorkList dbStatusWorkList)
		{
			// This will add the filter definition expressions by the DatabaseSourceClass
			dbStatusWorkList.UpdateDefinitionExpressions();
		}

		_msg.Debug($"Configured {result}. Start loading items...");

		if (AllowBackgroundLoading)
		{
			WorkListUtils.LoadItemsInBackground(result);
			WorkListUtils.CountItemsInBackground(result);
		}
		else
		{
			await QueuedTask.Run(() =>
			{
				result.LoadItems();
				result.Count();
			});
		}

		return result;
	}

	[CanBeNull]
	protected virtual string SuggestWorkListLayerName()
	{
		return null;
	}

	protected virtual void ConfigureWorkList(IWorkList workList) { }

	/// <summary>
	/// Loads the work list layer and its associated layers into the specified map view.
	/// </summary>
	public virtual void LoadLayers(MapView mapView,
	                               IWorkList worklist,
	                               string workListDefinitionFilePath)
	{
		LoadWorkListLayer(mapView, worklist, workListDefinitionFilePath);
		LoadAssociatedLayers(mapView, worklist);
	}

	/// <summary>
	/// Loads the work list layer, containing the navigable items based on the plugin
	/// datasource, into the specified map view.
	/// </summary>
	/// <param name="mapView"></param>
	/// <param name="worklist"></param>
	/// <param name="workListDefinitionFilePath"></param>
	public void LoadWorkListLayer(MapView mapView,
	                              [NotNull] IWorkList worklist,
	                              [NotNull] string workListDefinitionFilePath)
	{
		ILayerContainerEdit layerContainer = GetLayerContainerCore<ILayerContainerEdit>(mapView);

		CIMFeatureLayer layerDefinition =
			CreateWorkListLayerDefinition(worklist, workListDefinitionFilePath);

		CreateLayer(layerDefinition, new[] { layerDefinition }, null, layerContainer);
	}

	/// <summary>
	/// Creates the complete layer definition of the work list layer, based on the layer
	/// definition in the work list symbology template layer file. All properties are set in
	/// the definition rather than on the created layer: In an edit session, Pro synchronizes
	/// (and saves) the editing templates of all open maps for every added layer and for most
	/// layer property changes, which makes each change after creation very expensive.
	/// </summary>
	[NotNull]
	protected CIMFeatureLayer CreateWorkListLayerDefinition(
		[NotNull] IWorkList worklist,
		[NotNull] string workListDefinitionFilePath)
	{
		CIMDataConnection dataConnection;

		using (Table table = OpenTable(workListDefinitionFilePath, worklist.Name))
		{
			Assert.NotNull(table, $"Cannot open work list table {worklist.Name}");

			dataConnection = table.GetDataConnection();
		}

		CIMFeatureLayer result = GetWorkListTemplateLayerDefinition();

		result.Name = SuggestWorkListLayerName() ?? worklist.DisplayName;
		result.Description = worklist.Name;
		result.URI = CreateMapMemberUri();
		result.Visibility = true;

		result.FeatureTable ??= new CIMFeatureTable();
		result.FeatureTable.DataConnection = dataConnection;

		// The work list data source is read-only:
		result.FeatureTable.Editable = false;

		// Hard-coded properties:
		result.ScaleSymbols = false;
		result.Selectable = false;
		result.Snappable = false;

		// The explore tool should ignore the work list layer:
		result.ShowPopups = false;

		// Avoid incorrect features after changes in tables of DbStatusWorkLists
		result.DisplayCacheType = DisplayCacheType.None;
		result.FeatureCacheType = FeatureCacheType.None;

		// The work list layer is read-only, editing templates are not needed
		result.AutoGenerateFeatureTemplates = false;

#if ARCGISPRO_GREATER_3_2
		result.LayerScaleVisibilityOptions =
			new CIMLayerScaleVisibilityOptions { ShowLayerAtAllScales = true };
#endif

		return result;
	}

	/// <summary>
	/// Adds the specified top-level layer, including all its sub-layers and standalone tables
	/// in a single layer document to the container. The layers are added in one step, which
	/// results in a single LayersAdded event.
	/// </summary>
	[NotNull]
	protected static Layer CreateLayer([NotNull] CIMBaseLayer topLayer,
	                                   [NotNull] IEnumerable<CIMDefinition> layerDefinitions,
	                                   [CanBeNull] IEnumerable<CIMDefinition> tableDefinitions,
	                                   [NotNull] ILayerContainerEdit layerContainer)
	{
		var layerDocument = new CIMLayerDocument
		                    {
			                    Layers = new[] { topLayer.URI },
			                    LayerDefinitions = layerDefinitions.ToArray(),
			                    TableDefinitions = tableDefinitions?.ToArray()
		                    };

		var creationParams = new LayerCreationParams(layerDocument)
		                     {
			                     MapMemberPosition = MapMemberPosition.AddToTop
		                     };

		Layer result = LayerFactory.Instance.CreateLayer<Layer>(creationParams, layerContainer);

		if (result == null)
		{
			_msg.WarnFormat("Failed to create layer {0}. Trying one more time...", topLayer.Name);

			result = LayerFactory.Instance.CreateLayer<Layer>(creationParams, layerContainer);
		}

		Assert.NotNull(
			result,
			$"Creation of layer {topLayer.Name} failed even after the second time. Please try again manually.");

		LoadLegendOfExpandedLayers(result);

		return result;
	}

	/// <summary>
	/// Loads the legend of the expanded layers. Pro does not always load the legend of an expanded
	/// layer that is added inside a collapsed group layer: the tree node is shown as expanded, but
	/// the legend patches only appear after collapsing and expanding the layer again.
	/// Loading the legend does not change the layer definition.
	/// </summary>
	private static void LoadLegendOfExpandedLayers([NotNull] Layer layer)
	{
		IEnumerable<Layer> layers = layer is ILayerContainer layerContainer
			                            ? layerContainer.GetLayersAsFlattenedList()
			                            : new[] { layer };

		foreach (Layer expandedLayer in layers.Where(l => l is not CompositeLayer && l.IsExpanded))
		{
			expandedLayer.LoadLegend();
		}
	}

	[NotNull]
	protected static CIMGroupLayer CreateGroupLayerDefinition(
		[NotNull] string name,
		[NotNull] IEnumerable<string> layerUris,
		[CanBeNull] IEnumerable<string> standaloneTableUris = null)
	{
		var result = new CIMGroupLayer
		             {
			             Name = name,
			             URI = CreateMapMemberUri(),
			             Visibility = true,
			             Expanded = false,
			             ShowLegends = true,
			             Layers = layerUris.ToArray(),
			             StandaloneTables = standaloneTableUris?.ToArray()
		             };

#if ARCGISPRO_GREATER_3_2
		result.LayerScaleVisibilityOptions =
			new CIMLayerScaleVisibilityOptions { ShowLayerAtAllScales = true };
#endif

		return result;
	}

	[NotNull]
	protected static string CreateMapMemberUri()
	{
		return $"CIMPATH=worklists/{Guid.NewGuid():N}.json";
	}

	/// <summary>
	/// Loads associated layers of the work list layer into the specified map view.
	/// Typically, associated layers come with DB-status work lists, such as the layers of
	/// the issue feature classes.
	/// </summary>
	public virtual void LoadAssociatedLayers(MapView mapView, IWorkList worklist) { }

	public virtual void RemoveAssociatedLayers(MapView mapView) { }

	public virtual bool UseMapForWorkLists(MapView mapView)
	{
		return true;
	}

	/// <summary>
	/// Returns the layer container (group layer or map) for the work list layer(s) to be added
	/// to the specified map view.
	/// </summary>
	/// <param name="mapView"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	protected abstract T GetLayerContainerCore<T>(MapView mapView)
		where T : class, ILayerContainerEdit;

	protected virtual async Task<bool> TryPrepareSchemaCoreAsync()
	{
		return await Task.FromResult(true);
	}

	protected abstract IWorkList CreateWorkListCore([NotNull] IWorkItemRepository repository,
	                                                [NotNull] string uniqueName,
	                                                [NotNull] string displayName);

	protected abstract IWorkItemStateRepository CreateStateRepositoryCore(
		string path, string workListName, string displayName);

	[ItemCanBeNull]
	protected abstract Task<IWorkItemRepository> CreateItemRepositoryCoreAsync(
		IWorkItemStateRepository stateRepository);

	protected abstract string GetWorkListSymbologyTemplateLayerPath();

	protected virtual Type GetWorkListTypeCore<T>() where T : IWorkList
	{
		return typeof(T);
	}

	#region Private

	/// <summary>
	/// Gets the layer definition from the work list symbology template layer file, or a
	/// minimal definition if the template contains no single feature layer.
	/// </summary>
	[NotNull]
	private CIMFeatureLayer GetWorkListTemplateLayerDefinition()
	{
		LayerDocument templateLayer = GetWorkListSymbologyTemplateLayer();

		// The document is de-serialized for each call, the definition can be modified.
		CIMFeatureLayer result = LayerUtils.GetSingleLayerCIM<CIMFeatureLayer>(templateLayer);

		if (result != null)
		{
			return result;
		}

		_msg.DebugFormat("No single feature layer found in work list symbology template layer");

		// The work list layer is always a polygon layer (see WorkItemTable):
		CIMPolygonSymbol symbol =
			SymbolFactory.Instance.ConstructPolygonSymbol(ColorFactory.Instance.GreyRGB);

		return new CIMFeatureLayer
		       {
			       Expanded = true,
			       ShowLegends = true,
			       Renderer = new CIMSimpleRenderer { Symbol = symbol.MakeSymbolReference() }
		       };
	}

	protected virtual Table OpenTable([NotNull] string path, [NotNull] string tableName)
	{
		using PluginDatastore datastore =
			WorkListUtils.GetPluginDatastore(new Uri(path, UriKind.Absolute));

		return datastore.OpenTable(tableName);
	}

	private LayerDocument GetWorkListSymbologyTemplateLayer()
	{
		string filePath = GetWorkListSymbologyTemplateLayerPath();

		_msg.DebugFormat("Using work list symbology template layer from {0}", filePath);

		return LayerUtils.OpenLayerDocument(filePath);
	}

	#endregion

	public abstract string GetDisplayName();

	/// <summary>
	/// Check whether definition file exists in PROJECT\WorkLists folder.
	/// ATTENTION: result is dependent on implementation of GetDisplayName()!
	/// </summary>
	/// <param name="worklistFilePath">Full path to work list file</param>
	public bool WorkListFileExistsInProjectFolder(out string worklistFilePath)
	{
		string directory = Path.Combine(Project.Current.HomeFolderPath, WorklistsFolder);
		Assert.True(FileSystemUtils.EnsureDirectoryExists(directory),
		            $"Cannot create {directory}");

		string fileName = FileSystemUtils.ReplaceInvalidFileNameChars(GetDisplayName(), '_');
		worklistFilePath = EnsureValidDefinitionFilePath(directory, fileName, FileSuffix);

		return worklistFilePath != null && File.Exists(worklistFilePath);
	}

	// TODO: (daro) rename to EnsureUniqueWorkListFile
	/// <summary>
	/// Ensures that work list file is always unique. Override this
	/// method to always use the same work list file.
	/// </summary>
	protected virtual string EnsureValidDefinitionFilePath(
		string directory, string fileName, string suffix)
	{
		int increment = 1;
		string newFileName = fileName;

		while (File.Exists(Path.Combine(directory, $"{newFileName}{suffix}")))
		{
			newFileName = $"{fileName} {increment++}";
		}

		return Path.Combine(directory, $"{newFileName}{suffix}");
	}
}
