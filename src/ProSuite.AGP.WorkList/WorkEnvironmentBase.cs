using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
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

			_msg.Debug($"Configured {result}. Start loading items...");

			if (AllowBackgroundLoading)
			{
				WorkListUtils.LoadItemsInBackground(result);
				WorkListUtils.CountItemsInBackground(result);
			}
			else
			{
				await QueuedTask.Run(() => { result.LoadItems(); });
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
			//Create the work list layer with basic properties and connect to datasource
			FeatureLayer worklistLayer =
				CreateWorklistLayer(worklist, workListDefinitionFilePath,
				                    GetLayerContainerCore<ILayerContainerEdit>(mapView));

			//Set some hard-coded properties
			worklistLayer.SetScaleSymbols(false);
			worklistLayer.SetSelectable(false);
			worklistLayer.SetSnappable(false);

#if ARCGISPRO_GREATER_3_2
			worklistLayer.SetShowLayerAtAllScales(true);
#endif

			// The explore tool should ignore the work list layer:
			worklistLayer.SetShowPopups(false);

			// Avoid incorrect features after changes in tables of DbStatusWorkLists
			worklistLayer.SetCacheOptions(LayerCacheType.None);

			//Set renderer based on symbology from template layer
			LayerDocument templateLayer = GetWorkListSymbologyTemplateLayer();
			var renderer = LayerUtils.GetRenderer(templateLayer, worklistLayer);
			if (renderer != null)
			{
				worklistLayer.SetRenderer(renderer);
			}
			//else: no compatible renderer found in layer file
		}

		/// <summary>
		/// Loads associated layers of the work list layer into the specified map view.
		/// Typically, associated layers come with DB-status work lists, such as the layers of
		/// the issue feature classes.
		/// </summary>
		public virtual void LoadAssociatedLayers(MapView mapView, IWorkList worklist) { }

		public virtual void RemoveAssociatedLayers(MapView mapView) { }

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

		[NotNull]
		private FeatureLayer CreateWorklistLayer(
			[NotNull] IWorkList worklist,
			[NotNull] string path,
			[NotNull] ILayerContainerEdit layerContainer)
		{
			Table table = null;

			try
			{
				table = OpenTable(path, worklist.Name);
				Assert.NotNull(table);

				string workListLayerName = SuggestWorkListLayerName() ?? worklist.DisplayName;

				FeatureLayer result = LayerFactory.Instance.CreateLayer<FeatureLayer>(
					LayerUtils.CreateLayerParams((FeatureClass) table, workListLayerName),
					layerContainer);

				if (result == null)
				{
					_msg.WarnFormat(
						"Failed to create work list layer for {0}. Trying one more time...",
						worklist.Name);
					result = LayerFactory.Instance.CreateLayer<FeatureLayer>(
						LayerUtils.CreateLayerParams((FeatureClass) table, workListLayerName),
						layerContainer);
				}

				Assert.NotNull(
					result,
					"Layer creation failed even after the second time. Please try again manually.");

				return result;
			}
			finally
			{
				table?.Dispose();
			}
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
}
