using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Solution.ProjectItem;
using ProSuite.AGP.Solution.WorkListUI;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	public class WorkListsModule : Module
	{
		private static WorkListsModule _instance;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// todo daro Layer instead of FeatureLayer
		[NotNull] private readonly Dictionary<string, List<FeatureLayer>> _layersByWorklistName =
			new Dictionary<string, List<FeatureLayer>>();

		[NotNull] private readonly Dictionary<string, IWorkListObserver> _viewsByWorklistName =
			new Dictionary<string, IWorkListObserver>();

		private IWorkListRegistry _registry;
		[CanBeNull] private EditEventsRowCacheSynchronizer _synchronizer;

		public static WorkListsModule Current =>
			_instance ?? (_instance =
				              (WorkListsModule) FrameworkApplication.FindModule(
					              "ProSuite_WorkList_Module"));

		public IWorkList ActiveWorkListlayer { get; internal set; }
		
		public event EventHandler<WorkItemPickArgs> WorkItemPicked;

		public async Task ShowViewAsync()
		{
			var workLists = await QueuedTask.Run(() => GetLoadedWorkLists().ToList());

			foreach (IWorkList worklist in workLists)
			{
				// after Pro start up there is no view yet
				if (! _viewsByWorklistName.ContainsKey(worklist.Name))
				{
					WorklistItem item = ProjectItemUtils.Get<WorklistItem>(worklist.DisplayName);
					Assert.NotNull(item);

					_viewsByWorklistName.Add(worklist.Name, new WorkListObserver(worklist, item));
				}

				IWorkListObserver view = _viewsByWorklistName[worklist.Name];

				if (_layersByWorklistName.TryGetValue(worklist.Name, out List<FeatureLayer> worklistLayers))
				{
					// find the work list layer in the active map
					Layer activeWorklistLayer = GetActiveWorklistLayers(worklistLayers).First();

					view.Show(activeWorklistLayer.Name);
				}
				else
				{
					view.Show();
				}
			}
		}
		
		public void ShowView([NotNull] IWorkList worklist)
		{
			Assert.ArgumentCondition(_registry.Exists(worklist.Name),
			                         $"work list {worklist.Name} does not exist");

			if (! _viewsByWorklistName.TryGetValue(worklist.Name, out IWorkListObserver view))
			{
				return;
			}
			
			view.Show(worklist.DisplayName);
		}

		[CanBeNull]
		public async Task<IWorkList> ShowWorklist([NotNull] WorkEnvironmentBase environment, [NotNull] string path)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			string name = WorkListUtils.GetWorklistName(path)?.ToLower();
			Assert.NotNullOrEmpty(name);
			
			IWorkList worklist = _registry.Get(name);

			// show a work list that is not a project item and thus unknown
			if (worklist == null)
			{
				string displayName = WorkListUtils.GetName(path);
				return await CreateWorkListAsync(environment, name, displayName);
			}

			WorklistItem item = ProjectItemUtils.Get<WorklistItem>(Path.GetFileName(path));

			if (item == null && ! ProjectItemUtils.TryAdd(path, out item))
			{
				// work list item is null when  path is missing file extension (e.g. .iwl, .swl)
				var message = $"Cannot determine work list type from file ${path}";

				_msg.Debug(message);

				ErrorHandler.Show(message, "Unknown work list file", MessageBoxButton.OK,
				                  MessageBoxImage.Exclamation);
				return null;
			}

			// is work list layer already loaded?
			if (_layersByWorklistName.TryGetValue(worklist.Name, out List<FeatureLayer> worklistLayers))
			{
				// check whether layer is part of active map
				foreach (Layer worklistLayer in GetActiveWorklistLayers(worklistLayers))
				{
					string message = string.Format("Work List {0} is already loaded: layer {1}",
				                                 worklist.DisplayName, worklistLayer.Name);
					_msg.Info(message);

					ErrorHandler.Show(message, "Work List Layer", MessageBoxButton.OK,
					                  MessageBoxImage.Information);
				}
			}
			else
			{
				// assertion is a no-op to avoid resharper warning
				Assert.NotNull(environment.LoadLayers().ToList());
				environment.AddLayer(worklist, path);
			}

			if (! _viewsByWorklistName.ContainsKey(worklist.Name))
			{
				_viewsByWorklistName.Add(worklist.Name, new WorkListObserver(worklist, item));
			}

			return worklist;
		}

		public async Task<IWorkList> CreateWorkListAsync([NotNull] WorkEnvironmentBase environment,
		                                                 [NotNull] string uniqueName,
		                                                 [CanBeNull] string displayName = null)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));
			Assert.ArgumentNotNullOrEmpty(uniqueName, nameof(uniqueName));

			Assert.False(_registry.Exists(uniqueName), $"work list {uniqueName} already exists");

			IWorkList worklist = await environment.CreateWorkListAsync(uniqueName, displayName);

			if (worklist == null)
			{
				return null;
			}

			// after creation go to nearest item
			worklist.GoNearest(MapView.Active.Extent);

			// Commit writes work list definition to disk.
			// Necessary for adding project item.
			worklist.Commit();

			// wiring work list events, etc. is done in OnDrawComplete
			// register work list before creating the layer
			_registry.TryAdd(worklist);
			
			string fileName = string.IsNullOrEmpty(displayName) ? uniqueName : displayName;

			string path = environment.GetDefinitionFile(fileName);

			if (! ProjectItemUtils.TryAdd(path, out WorklistItem item))
			{
				// maybe the work list is empty > makes no sense to store a
				// definition file for an empty work list
				_msg.Debug($"could not add {worklist.Name}");
				_msg.Debug($"work item count {worklist.Count()}");
			}

			environment.AddLayer(worklist, path);

			if (! _viewsByWorklistName.ContainsKey(worklist.Name))
			{
				_viewsByWorklistName.Add(worklist.Name, new WorkListObserver(worklist, item));
			}

			return worklist;
		}

		#region Module overrides

		protected override bool Initialize()
		{
			_msg.Debug($"{nameof(Initialize)} {nameof(WorkListsModule)}");

			_registry = WorkListRegistry.Instance;

			WireEvents();

			return base.Initialize();
		}

		protected override async void Uninitialize()
		{
			_msg.Debug($"{nameof(Uninitialize)} {nameof(WorkListsModule)}");

			_synchronizer?.Dispose();

			await ViewUtils.TryAsync(QueuedTask.Run(() =>
			{
				foreach (IWorkList workList in GetLoadedWorkLists())
				{
					workList?.Dispose();
				}
			}), _msg);

			UnwireEvents();

			base.Uninitialize();
		}

		// ReSharper disable once RedundantOverriddenMember
		protected override Task OnReadSettingsAsync(ModuleSettingsReader settings)
		{
			return base.OnReadSettingsAsync(settings);
		}
		
		// NOTE daro Method is only called when project is dirty.
		// NOTE daro OnWriteSettingsAsync is called twice on creating a project.
		// ReSharper disable once RedundantOverriddenMember
		protected override Task OnWriteSettingsAsync(ModuleSettingsWriter settings)
		{
			return base.OnWriteSettingsAsync(settings);
		}

		#endregion

		[NotNull]
		public static string EnsureUniqueName()
		{
			//todo daro: use GUID as identifier?
			return $"{Guid.NewGuid()}".Replace('-', '_').ToLower();
		}

		private static string GetLocalWorklistsFolder()
		{
			return WorkListUtils.GetLocalWorklistsFolder(Project.Current.HomeFolderPath);
		}

		#region Events

		private void WireEvents()
		{
			// creating a project
			// 1. OnProjectOpened
			// 2. OnWriteSettings
			// 3. OnMapViewInitialized
			// 4. OnDrawCompleted

			//MapViewInitializedEvent.Subscribe(OnMapViewInitialized
			MapClosedEvent.Subscribe(OnMapClosedAsync);

			LayersAddedEvent.Subscribe(OnLayerAdded);
			LayersRemovingEvent.Subscribe(OnLayerRemovingAsync);
			DrawCompleteEvent.Subscribe(OnDrawCompletedAsync);

			ProjectOpenedAsyncEvent.Subscribe(OnProjectOpendedAsync);
			ProjectSavingEvent.Subscribe(OnProjectSavingAsync);

			ProjectItemsChangedEvent.Subscribe(OnProjectItemsChangedAsync);
			ProjectItemRemovingEvent.Subscribe(OnProjectItemRemovingAsync);

			MapMemberPropertiesChangedEvent.Subscribe(OnMapMemberPropertiesChanged);
		}

		private void UnwireEvents()
		{
			//MapViewInitializedEvent.Unsubscribe(OnMapViewInitialized);

			MapClosedEvent.Unsubscribe(OnMapClosedAsync);
			LayersAddedEvent.Unsubscribe(OnLayerAdded);
			LayersRemovingEvent.Unsubscribe(OnLayerRemovingAsync);
			DrawCompleteEvent.Unsubscribe(OnDrawCompletedAsync);

			ProjectOpenedAsyncEvent.Unsubscribe(OnProjectOpendedAsync);
			ProjectSavingEvent.Unsubscribe(OnProjectSavingAsync);

			ProjectItemsChangedEvent.Unsubscribe(OnProjectItemsChangedAsync);
			ProjectItemRemovingEvent.Unsubscribe(OnProjectItemRemovingAsync);

			MapMemberPropertiesChangedEvent.Unsubscribe(OnMapMemberPropertiesChanged);
		}

		private async Task WorklistChanged(WorkListChangedEventArgs e)
		{
			// todo daro ViewUtils!
			// NOTE daro:
			// If the following code is put into a await ViewUtils.TryAsync(QueuedTask.Run(() =>{}, _msg)
			// there is a strange race condition when adding a second work list. So far the code works without it.
			// Examine it later!
			try
			{
				var workList = (IWorkList) e.Sender;

				Assert.True(_layersByWorklistName.ContainsKey(workList.Name),
				            $"sender of {nameof(WorklistChanged)} is unknown");

				List<FeatureLayer> worklistLayers = _layersByWorklistName[workList.Name];

				foreach (MapView mapView in FrameworkApplication.Panes.OfType<IMapPane>()
				                                                .Select(mapPane => mapPane.MapView))
				{
					if (mapView == null || ! mapView.IsReady)
					{
						continue;
					}

					foreach (FeatureLayer worklistLayer in worklistLayers)
					{
						List<long> oids = e.Items;

						if (oids != null)
						{
							// invalidate with OIDs
							mapView.Invalidate(new Dictionary<Layer, List<long>> { { worklistLayer, oids } });
							continue;
						}

						Envelope extent = e.Extent ?? mapView.Extent;

						if (extent != null)
						{
							// alternatively invalidate with Envelope
							mapView.Invalidate(worklistLayer, extent);
						}
					}
				}
			}
			catch (Exception exc)
			{
				_msg.Error("Error invalidating work list layer", exc);
			}
		}

		// todo daro: move to OnMapViewInitialized?
		private async void OnDrawCompletedAsync(MapViewEventArgs e)
		{
			IReadOnlyList<Layer> layers = e.MapView.Map.GetLayersAsFlattenedList();

			await QueuedTask.Run(() =>
			{
				foreach (string name in _registry.GetNames())
				{
					foreach (FeatureLayer worklistLayer in GetWorklistLayers(layers, name).Cast<FeatureLayer>())
					{
						Assert.NotNull(worklistLayer);

						if (_layersByWorklistName.TryGetValue(name, out List<FeatureLayer> worklistLayers))
						{
							// safety check, a new work list is already added
							if (worklistLayers.Contains(worklistLayer))
							{
								continue;
							}

							worklistLayers.Add(worklistLayer);
						}
						else
						{
							_layersByWorklistName.Add(
								name, new List<FeatureLayer> { worklistLayer });
						}

						IWorkList workList = _registry.Get(name);
						Assert.NotNull(workList);

						WorkListChangedEvent.Subscribe(WorklistChanged, this);

						// todo daro: maybe we need a dictionary of synchronizers
						_synchronizer = new EditEventsRowCacheSynchronizer(workList);
					}
				}
			});
		}

		// todo daro Use MapViewInitialized or DrawComplete?
		//private void OnMapViewInitialized(MapViewEventArgs e)
		//{
		//	// https://community.esri.com/t5/arcgis-pro-sdk-questions/how-to-wait-for-mapview-to-load/td-p/831290
		//	// https://community.esri.com/t5/arcgis-pro-sdk-questions/initialize-module-when-map-loaded/m-p/816108#M2596

		//	// fires every time a view is initialized
		//	// todo daro obj.MapView or MapView.Active?

		//	foreach (var pair in _uriByWorklistName)
		//	{
		//		// ReSharper disable once UnusedVariable
		//		string worklistName = pair.Key;
		//		// ReSharper disable once UnusedVariable
		//		string uri = pair.Value;
		//	}
		//}

		private void OnMapMemberPropertiesChanged(MapMemberPropertiesChangedEventArgs e)
		{
			ViewUtils.Try(() =>
			{
				List<MapMember> mapMembers = e.MapMembers.ToList();
				List<MapMemberEventHint> eventHints = e.EventHints.ToList();

				if (mapMembers.Count == 0)
				{
					return;
				}

				Assert.AreEqual(mapMembers.Count, eventHints.Count,
				                $"Unequal count of {nameof(MapMember)} and {nameof(MapMemberEventHint)}");

				for (var index = 0; index < mapMembers.Count; index++)
				{
					if (eventHints[index] != MapMemberEventHint.Name)
					{
						continue;
					}

					MapMember mapMember = mapMembers[index];

					RenameView(mapMember);
				}
			}, _msg);
		}

		private async void OnMapClosedAsync(MapClosedEventArgs e)
		{
			await ViewUtils.TryAsync(QueuedTask.Run(() =>
			{
				// ToList() is important because items are being removed
				// from _layersByWorklistName
				foreach (IWorkList workList in GetLoadedWorkLists().ToList())
				{
					Unload(workList);
				}
			}), _msg);
		}

		private async Task OnLayerRemovingAsync(LayersRemovingEventArgs e)
		{
			await ViewUtils.TryAsync(QueuedTask.Run(() =>
			{
				var flattenedLayers = new List<Layer>();

				Flatten(e.Layers, flattenedLayers);

				foreach (IWorkList worklist in GetWorklists(flattenedLayers))
				{
					Unload(worklist);

					// persist work list state
					worklist.Commit();

					// Note daro: don't dispose work list here. Given the following situation.
					// Remove work list layer would dispose the source geodatabase (in GdbItemRepository).
					// Add work list layer again with same source geodatabase is going to throw an
					// exception, e.g. on SetStatus
					//workList.Dispose();
				}
			}), _msg);
		}

		private void OnLayerAdded(LayerEventsArgs e)
		{
		}

		private async Task OnProjectOpendedAsync(ProjectEventArgs e)
		{
			// 1) Check all existing project items.
			// 2) Add a work list factory to registry for every work list project item found in custom project items
			//	  (Use work list factory because we do not want to create a work list for EVERY work list project item.
			//	   Only create a work list (from a work list factory / work list custom project item) if a layer requests a work
			//	   list as a data source
			// order of method calls:
			// 1. Module.Initialize
			// 2. OnProjectOpenedAsync
			// 3. OnProjectOpened
			// 4. Pluggable Datasource Open()

			await ViewUtils.TryAsync(QueuedTask.Run(() =>
			{
				// A work list file might have been deleted in the file system (outside Pro)
				// and the file is still a project item because Pro got not notified.
				// or
				// Open a project, open another project, re-open the first project > the work list factory
				// has already been added. Assert.True(_registry.TryAdd(factory)) would fail.
				// => don't us an assertion here, just try to add 
				foreach (var item in ProjectItemUtils.Get<WorklistItem>())
				{
					if (File.Exists(item.Path))
					{
						var factory = new XmlBasedWorkListFactory(item.Path, item.WorklistName);

						if (_registry.TryAdd(factory))
						{
							_msg.Debug($"Add work list {item.WorklistName} from file {item.Path}");
						}
					}
					else
					{
						ProjectItemUtils.Remove(item);
					}
				}
			}), _msg);
		}

		private async Task OnProjectSavingAsync(ProjectEventArgs e)
		{
			await ViewUtils.TryAsync(QueuedTask.Run(() =>
			{
				FileSystemUtils.EnsureFolderExists(GetLocalWorklistsFolder());

				foreach (IWorkList workList in GetLoadedWorkLists())
				{
					workList.Commit();
				}
			}), _msg);
		}

		private async void OnProjectItemsChangedAsync(ProjectItemsChangedEventArgs e)
		{
			await ViewUtils.TryAsync(QueuedTask.Run(() =>
			{
				if (e.ProjectItemsCollection == null)
				{
					return;
				}

				foreach (WorklistItem item in e.ProjectItemsCollection.OfType<WorklistItem>())
				{
					switch (e.Action)
					{
						case NotifyCollectionChangedAction.Replace:

							Assert.NotNullOrEmpty(item.WorklistName);
							Assert.True(_registry.Exists(item.WorklistName),
							            $"Cannot find work list {item.WorklistName} in registry");

							// TODO daro: replace work list in registry. it's not enough to set new path
							// there should always be the guid as name in the registry
							// name of *.iwl file is ok for display name
							// work list name is also the uri of layer and its data source
							// work list display name is the layer alias

							Assert.True(_registry.Remove(item.WorklistName),
							            $"failed to remove work list {item.WorklistName}");

							var factory = new XmlBasedWorkListFactory(item.Path, item.WorklistName);

							Assert.True(_registry.TryAdd(factory),
							            $"Cannot add work list {item.WorklistName}");

							_msg.Debug($"Add work list {item.WorklistName} from file {item.Path}");

							// The following situation: the work list layer (e.g. named "foo") is already in TOC
							if (_layersByWorklistName.TryGetValue(item.WorklistName, out List<FeatureLayer> layers))
							{
								foreach (FeatureLayer worklistLayer in layers)
								{
									PluginDatastore datastore = WorkListUtils.GetPluginDatastore(new Uri(item.Path));
									string connectionString = datastore.GetConnectionString();

									var connection = new CIMStandardDataConnection
									                 {
										                 WorkspaceConnectionString = connectionString,
										                 WorkspaceFactory = WorkspaceFactory.Custom,
										                 DatasetType = esriDatasetType.esriDTFeatureClass,
										                 Dataset = item.WorklistName
									                 };

									worklistLayer.SetDataConnection(connection);
								}
							}

							break;
						case NotifyCollectionChangedAction.Add:
						case NotifyCollectionChangedAction.Remove:

						// Item is never removed. It is always deleted.
						case NotifyCollectionChangedAction.Move:
						case NotifyCollectionChangedAction.Reset:
							throw new NotImplementedException();
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}), _msg);
		}

		private async Task OnProjectItemRemovingAsync(ProjectItemRemovingEventArgs e)
		{
			// NOTE collapses Worklists folder, refresh of folder would be better
			//var projectItem = ProjectItemUtils.Get<MapProjectItem>().ToList();
			//List<Item> items = projectItem?.GetItems()?.ToList();
			//string folderName = "Worklists";
			//Item firstOrDefault = items?.FirstOrDefault(i => string.Equals(folderName, i.Name));

			await ViewUtils.TryAsync(QueuedTask.Run(() =>
			{
				foreach (var item in e.ProjectItems.OfType<WorklistItem>())
				{
					string name = WorkListUtils.GetWorklistName(item.Path);
					Assert.NotNullOrEmpty(name);

					if (! _layersByWorklistName.TryGetValue(name, out List<FeatureLayer> layers))
					{
						continue;
					}

					foreach (Map map in FrameworkApplication.Panes.OfType<IMapPane>().Select(mapPane => mapPane.MapView.Map))
					{
						// Removing a layer that's not part of a layer container throws an exception.
						// Check whether the layers are part of the map.
						foreach (Layer worklistLayer in GetActiveWorklistLayers(layers, map))
						{
							// this does NOT call the OnLayerRemovingAsync event handler!!
							// OnLayerRemovingAsync is called when the layer is removes manually
							map.RemoveLayer(worklistLayer);
						}
					}

					IWorkList worklist = _registry.Get(name);
					Assert.NotNull(worklist);

					// no need to persist work list state, work list gets deleted
					Unload(worklist);

					Assert.True(_registry.Remove(worklist),
					            $"Cannot remove work list {worklist.Name} from registry");
				}
			}), _msg);
		}

		#endregion

		private static void Flatten([NotNull] IEnumerable<Layer> layers,
		                            [NotNull] ICollection<Layer> flattenedLayers)
		{
			foreach (Layer layer in layers)
			{
				if (layer is ILayerContainer container)
				{
					Flatten(container.Layers, flattenedLayers);
				}
				else
				{
					flattenedLayers.Add(layer);
				}
			}
		}

		private void RenameView(MapMember mapMember)
		{
			string uri = MapUtils.GetUri(mapMember);
			string name = WorkListUtils.ParseName(uri);

			if (! _viewsByWorklistName.TryGetValue(name, out IWorkListObserver view))
			{
				return;
			}

			if (view.View == null)
			{
				return;
			}

			ViewUtils.RunOnUIThread(() => { view.View.Title = mapMember.Name; });
		}
		
		// todo daro to WorkListUtils
		/// <summary>
		/// Finds all work list layers in the active map.
		/// </summary>
		/// <param name="worklistLayers">all work list layers</param>
		private static IEnumerable<Layer> GetActiveWorklistLayers(
			[NotNull] IEnumerable<FeatureLayer> worklistLayers)
		{

			return GetActiveWorklistLayers(worklistLayers, MapUtils.GetActiveMap());
		}

		
		/// <summary>
		/// Finds all work list layers in the layer container.
		/// </summary>
		/// <param name="worklistLayers">all work list layers</param>
		/// <param name="container">map or group layer</param>
		/// <returns></returns>
		private static IEnumerable<Layer> GetActiveWorklistLayers(
			[NotNull] IEnumerable<FeatureLayer> worklistLayers,
			[NotNull] ILayerContainer container)
		{

			return worklistLayers.Select(layer => container.FindLayer(layer.URI))
			                     .Where(layer => layer != null);
		}

		// todo daro to WorkListUtils
		private static IEnumerable<Layer> GetWorklistLayers([NotNull] IEnumerable<Layer> layers,
		                                                    [NotNull] string worklistName)
		{
			return layers.Where(layer =>
			{
				var connection = layer.GetDataConnection() as CIMStandardDataConnection;

				return string.Equals(worklistName, connection?.Dataset,
				                     StringComparison.OrdinalIgnoreCase);
			});
		}
		
		[NotNull]
		private IEnumerable<IWorkList> GetWorklists([NotNull] IEnumerable<Layer> layers)
		{
			return layers.Select(GetWorklist).Where(worklist => worklist != null);
		}

		[CanBeNull]
		private IWorkList GetWorklist(Layer layer)
		{
			var connection = layer.GetDataConnection() as CIMStandardDataConnection;

			return connection == null ? null : _registry.Get(connection.Dataset);
		}

		/// <summary>
		/// Work lists with a work list layer in the TOC.
		/// </summary>
		[NotNull]
		private IEnumerable<IWorkList> GetLoadedWorkLists()
		{
			IEnumerable<FeatureLayer> loadedLayers =
				_layersByWorklistName.Values.SelectMany(layers => layers);

			return GetWorklists(loadedLayers);
		}

		public virtual void OnWorkItemPicked(WorkItemPickArgs e)
		{
			WorkItemPicked?.Invoke(null, e);
		}

		private void Unload([NotNull] IWorkList workList)
		{
			if (_viewsByWorklistName.ContainsKey(workList.Name))
			{
				_viewsByWorklistName[workList.Name].Close();
				_viewsByWorklistName.Remove(workList.Name);
			}

			// ensure folder exists before commit
			FileSystemUtils.EnsureFolderExists(GetLocalWorklistsFolder());

			WorkListChangedEvent.Unsubscribe(WorklistChanged);

			_layersByWorklistName.Remove(workList.Name);
		}

		public bool IsWorklistLayer([NotNull] FeatureLayer featureLayer)
		{
			return _layersByWorklistName.Values.SelectMany(layer => layer).Contains(featureLayer);
		}
	}

	public class WorkItemPickArgs : EventArgs
	{
		public List<Feature> Features { get; set; }
	}
}
