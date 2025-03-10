using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.WorkList
{
	/// <summary>
	/// Encapsulates the logic (but no volatile state) for a work list type, including the creation
	/// of the work list.
	/// </summary>
	public abstract class WorkEnvironmentBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public abstract string FileSuffix { get; }

		/// <summary>
		/// The display name of the work list, used by the layer or navigator.
		/// </summary>
		protected string DisplayName { get; set; }

		/// <summary>
		/// The unique name of the work list that corresponds to the file name in
		/// the Worklists folder of the project.
		/// TODO: Implement and use for all DbStatusWorkLists and Selection worklist
		/// </summary>
		protected string UniqueName { get; set; }

		[ItemCanBeNull]
		public async Task<IWorkList> CreateWorkListAsync([NotNull] string uniqueName,
		                                                 string workListFilePath = null)
		{
			Assert.ArgumentNotNullOrEmpty(uniqueName, nameof(uniqueName));

			if (string.IsNullOrEmpty(DisplayName))
			{
				DisplayName = SuggestWorkListName() ?? uniqueName;
			}

			string definitionFilePath = GetDefinitionFileFromProjectFolder();

			if (File.Exists(definitionFilePath))
			{
				_msg.DebugFormat("Work list definition file {0} already exists",
				                 definitionFilePath);
				// Special handling (e.g. message box notifying the user) must have happened before.
				// TODO: Check that the state from the definition file is actually used.
				// NOTE: In case of DB Status work lists with changing table content the visited
				// state of the work list definition file is probably irrelevant or even incorrect
				// because the items (issues, revision points, etc.) regularly change in the
				// underlying DB table. In case a different extent / work unit has been loaded the
				// original items might not event be present any more.

				// TODO:
				// We should probably delete the definition file when the layer is unloaded
				// because a new load could be in a completely different area (work unit).
				// Or design a simplified definition file that just contains the connection to the
				// underlying table(s), ideally along with the relevant status schema?
			}

			if (File.Exists(workListFilePath))
			{
				definitionFilePath = workListFilePath;
			}

			if (! await TryPrepareSchemaCoreAsync())
			{
				// null work list
				return await Task.FromResult(default(IWorkList));
			}

			Stopwatch watch = Stopwatch.StartNew();

			IWorkItemStateRepository stateRepository =
				CreateStateRepositoryCore(definitionFilePath, uniqueName);

			_msg.DebugStopTiming(watch, "Created work list state repository in {0}",
			                     definitionFilePath);

			IWorkItemRepository itemRepository =
				await CreateItemRepositoryCore(stateRepository);

			if (itemRepository == null)
			{
				return await Task.FromResult<IWorkList>(null);
			}

			IWorkList result = CreateWorkListCore(itemRepository, uniqueName, DisplayName);

			_msg.DebugFormat("Created work list {0}", uniqueName);

			return result;
		}

		[CanBeNull]
		protected abstract string SuggestWorkListName();

		protected virtual string SuggestWorkListLayerName()
		{
			return null;
		}

		public bool DefinitionFileExistsInProjectFolder(out string definitionFile)
		{
			definitionFile = null;
			string suggestedName = SuggestWorkListName();

			if (suggestedName == null)
			{
				return false;
			}

			DisplayName = suggestedName;

			definitionFile = GetDefinitionFileFromProjectFolder();

			return definitionFile != null && File.Exists(definitionFile);
		}

		public string GetDefinitionFileFromProjectFolder()
		{
			Assert.ArgumentNotNullOrEmpty(DisplayName, nameof(DisplayName));

			string fileName = FileSystemUtils.ReplaceInvalidFileNameChars(DisplayName, '_');

			return WorkListUtils.GetDatasource(
				Project.Current.HomeFolderPath, fileName, FileSuffix);
		}

		/// <summary>
		/// Loads the work list layer, containing the navigable items based on the plugin
		/// datasource, into the map.
		/// </summary>
		/// <param name="worklist"></param>
		/// <param name="workListDefinitionFilePath"></param>
		public void LoadWorkListLayer([NotNull] IWorkList worklist,
		                              [NotNull] string workListDefinitionFilePath)
		{
			//Create the work list layer with basic properties and connect to datasource
			FeatureLayer worklistLayer =
				CreateWorklistLayer(worklist, workListDefinitionFilePath,
				                    GetLayerContainerCore<ILayerContainerEdit>());

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
		/// Loads associated layers of the work list layer into the map, if there are any.
		/// Typically, associated layers come with DB-status work lists, such as the layers of
		/// the issue feature classes.
		/// </summary>
		public virtual void LoadAssociatedLayers(IWorkList worklist) { }

		public virtual void RemoveAssociatedLayers() { }

		/// <summary>
		/// Returns the layer container (group layer or map) for the work list layer(s)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		protected abstract T GetLayerContainerCore<T>() where T : class, ILayerContainerEdit;

		protected virtual async Task<bool> TryPrepareSchemaCoreAsync()
		{
			return await Task.FromResult(true);
		}

		protected abstract IWorkList CreateWorkListCore([NotNull] IWorkItemRepository repository,
		                                                [NotNull] string uniqueName,
		                                                [CanBeNull] string displayName);

		protected abstract IWorkItemStateRepository CreateStateRepositoryCore(
			string path, string workListName);

		[ItemCanBeNull]
		protected abstract Task<IWorkItemRepository> CreateItemRepositoryCore(
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
					WorkListUtils.CreateLayerParams((FeatureClass) table, workListLayerName),
					layerContainer);

				if (result == null)
				{
					_msg.WarnFormat(
						"Failed to create work list layer for {0}. Trying one more time...",
						worklist.Name);
					result = LayerFactory.Instance.CreateLayer<FeatureLayer>(
						WorkListUtils.CreateLayerParams((FeatureClass) table, workListLayerName),
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

		public void EnsureUniqueName(string conflictingDefinitionFile)
		{
			Assert.NotNull(DisplayName);

			string directory = Assert.NotNull(Path.GetDirectoryName(conflictingDefinitionFile));
			string fileName = Path.GetFileNameWithoutExtension(conflictingDefinitionFile);
			string suffix = Path.GetExtension(conflictingDefinitionFile);

			int increment = 1;
			string newFileName = fileName;
			while (File.Exists(Path.Combine(directory, newFileName + suffix)))
			{
				newFileName = $"{fileName} {increment++}";
			}

			DisplayName = newFileName;
			UniqueName = newFileName;
		}

		public abstract bool IsSameWorkListDefinition(string existingDefinitionFile);

		public IEnumerable<BasicFeatureLayer> FindWorkListLayers(Map map)
		{
			if (! DefinitionFileExistsInProjectFolder(out string definitionFile))
			{
				yield break;
			}

			// TODO: Consider making the folder the data store and the file the work list name?
			//       Currently, the data store is the work list file and the name is probably
			//       irrelevant for opening the work list (except that it must be unique for the
			//       work list registry, which could also use the full file path or some kind of
			//       name moniker similar to IFeatureClassName). 
			string workListName =
				UniqueName ?? WorkListUtils.GetWorklistName(definitionFile)?.ToLower();
			var datastore =
				WorkListUtils.GetPluginDatastore(new Uri(definitionFile, UriKind.Absolute));

			Table table = datastore.OpenTable(workListName);
			Assert.NotNull(table);

			foreach (FeatureLayer featureLayer in MapUtils.GetFeatureLayers<FeatureLayer>(
				         map, l => DatasetUtils.IsSameTable(table, l.GetTable())))
			{
				yield return featureLayer;
			}
		}
	}
}
