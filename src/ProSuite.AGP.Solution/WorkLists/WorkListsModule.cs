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
using ProSuite.Commons.Text;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	public class WorkListsModule : Module
	{
		private static WorkListsModule _instance;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly Dictionary<string, FeatureLayer> _layersByWorklistName =
			new Dictionary<string, FeatureLayer>();

		[NotNull] private readonly Dictionary<string, IWorkListObserver> _viewsByWorklistName =
			new Dictionary<string, IWorkListObserver>();

		[NotNull] private readonly Dictionary<string, string> _uriByWorklistName =
			new Dictionary<string, string>();

		private IWorkListRegistry _registry;
		[CanBeNull] private EditEventsRowCacheSynchronizer _synchronizer;

		public static WorkListsModule Current =>
			_instance ?? (_instance =
				              (WorkListsModule) FrameworkApplication.FindModule(
					              "ProSuite_WorkList_Module"));

		public Dictionary<string, FeatureLayer> LayersByWorklistName => _layersByWorklistName;

		public IWorkList ActiveWorkListlayer { get; internal set; }
		
		public event EventHandler<WorkItemPickArgs> WorkItemPicked;

		public void ShowView()
		{
			foreach (IWorkList worklist in GetWorklists())
			{
				// after Pro start up there is no view yet
				if (! _viewsByWorklistName.ContainsKey(worklist.Name))
				{
					WorklistItem item = ProjectItemUtils.Get<WorklistItem>(worklist.Name);
					Assert.NotNull(item);

					_viewsByWorklistName.Add(worklist.Name, new WorkListObserver(worklist, item));
				}

				IWorkListObserver view = _viewsByWorklistName[worklist.Name];

				if (_layersByWorklistName.TryGetValue(worklist.Name, out FeatureLayer layer))
				{
					view.Show(layer.Name);
				}
				else
				{
					view.Show();
				}
			}
		}

		// todo daro return Window?
		public void ShowView([NotNull] string worklistName)
		{
			if (! _viewsByWorklistName.TryGetValue(worklistName, out IWorkListObserver view))
			{
				return;
			}

			if (_layersByWorklistName.TryGetValue(worklistName, out FeatureLayer layer))
			{
				view.Show(layer.Name);
			}
			else
			{
				view.Show();
			}
		}

		[CanBeNull]
		public async Task<IWorkList> ShowWorklist([NotNull] WorkEnvironmentBase environment, [NotNull] string path)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			string name = WorkListUtils.GetWorklistName(path)?.ToLower();
			Assert.NotNullOrEmpty(name);
			
			IWorkList worklist = _registry.Get(name);

			if (worklist == null)
			{
				string displayName = WorkListUtils.GetName(path);
				return await CreateWorkListAsync(environment, name, displayName);
			}

			WorklistItem item = ProjectItemUtils.Get<WorklistItem>(Path.GetFileName(path));

			if (item == null && ! ProjectItemUtils.TryAdd(path, out item))
			{
				// work list item is null when  path is missing file extension (e.g. .iwl, .swl)
				ErrorHandler.Show($"Cannot determine work list type from file ${path}",
				                  "Unknown work list file", MessageBoxButton.OK,
				                  MessageBoxImage.Exclamation);

				return null;
			}

			// is work list layer already loaded?
			if (_layersByWorklistName.TryGetValue(worklist.Name, out FeatureLayer worklistLayer))
			{
				string message = string.Format("Work List {0} is already loaded: layer {1}",
				                               worklist.DisplayName, worklistLayer.Name);
				_msg.Info(message);

				ErrorHandler.Show(message, "Work List Layer", MessageBoxButton.OK,
				                  MessageBoxImage.Information);
			}
			else
			{
				// assertion is a no-op to avoid resharper warning
				Assert.NotNull(environment.LoadLayers().ToList());

				worklistLayer = environment.AddLayer(worklist);
			}

			if (! _viewsByWorklistName.ContainsKey(worklist.Name))
			{
				_viewsByWorklistName.Add(worklist.Name, new WorkListObserver(worklist, item));
			}

			if (! _uriByWorklistName.ContainsKey(worklist.Name))
			{
				_uriByWorklistName.Add(worklist.Name, worklistLayer.URI);
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

			string path = environment.GetDefinitionFile(worklist.DisplayName).LocalPath;

			if (! ProjectItemUtils.TryAdd(path, out WorklistItem item))
			{
				// maybe the work list is empty > makes no sense to store a
				// definition file for an empty work list
				_msg.Debug($"could not add {worklist.Name}");
				_msg.Debug($"work item count {worklist.Count()}");
			}

			FeatureLayer worklistLayer = environment.AddLayer(worklist);

			if (! _viewsByWorklistName.ContainsKey(worklist.Name))
			{
				_viewsByWorklistName.Add(worklist.Name, new WorkListObserver(worklist, item));
			}

			if (! _uriByWorklistName.ContainsKey(worklist.Name))
			{
				_uriByWorklistName.Add(worklist.Name, worklistLayer.URI);
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

		protected override void Uninitialize()
		{
			_msg.Debug($"{nameof(Uninitialize)} {nameof(WorkListsModule)}");

			_synchronizer?.Dispose();

			foreach (IWorkList workList in GetWorklists())
			{
				workList?.Dispose();
			}

			UnwireEvents();

			base.Uninitialize();
		}

		protected override Task OnReadSettingsAsync(ModuleSettingsReader settings)
		{
			// should never be null
			if (settings == null)
			{
				return base.OnReadSettingsAsync(settings);
			}

			// no crashes when:
			// settings[""] = null;
			// settings = null;

			if (! (settings.Get("worklistLayerUris") is string uris))
			{
				return base.OnReadSettingsAsync(settings);
			}

			foreach (string pair in uris.Trim().Split('#'))
			{
				string[] tokens = pair.Trim().Split(':');

				string worklistName = tokens[0];

				if (_uriByWorklistName.ContainsKey(worklistName))
				{
					continue;
				}

				_uriByWorklistName.Add(worklistName, tokens[1]);
			}

			return base.OnReadSettingsAsync(settings);
		}

		protected override Task OnWriteSettingsAsync(ModuleSettingsWriter settings)
		{
			// todo daro Methond is only called when project is dirty.
			// todo daro OnWriteSettingsAsync is called twice on creating a project.
			if (_uriByWorklistName.Count <= 0)
			{
				return base.OnWriteSettingsAsync(settings);
			}

			List<string> worklistLayerUris = new List<string>(_uriByWorklistName.Count);
			worklistLayerUris.AddRange(
				_uriByWorklistName.Select(pair => $"{pair.Key}:{pair.Value}"));

			settings.Add("worklistLayerUris", StringUtils.Concatenate(worklistLayerUris, "#"));

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

			//MapViewInitializedEvent.Subscribe(OnMapViewInitialized);
			LayersRemovingEvent.Subscribe(OnLayerRemovingAsync);
			DrawCompleteEvent.Subscribe(OnDrawCompleted);

			ProjectOpenedAsyncEvent.Subscribe(OnProjectOpendedAsync);
			ProjectSavingEvent.Subscribe(OnProjectSavingAsync);

			ProjectItemsChangedEvent.Subscribe(OnProjectItemsChangedAsync);
			ProjectItemRemovingEvent.Subscribe(OnProjectItemRemovingAsync);

			MapMemberPropertiesChangedEvent.Subscribe(OnMapMemberPropertiesChanged);
		}

		private void UnwireEvents()
		{
			//MapViewInitializedEvent.Unsubscribe(OnMapViewInitialized);
			LayersRemovingEvent.Unsubscribe(OnLayerRemovingAsync);
			DrawCompleteEvent.Unsubscribe(OnDrawCompleted);

			ProjectOpenedAsyncEvent.Unsubscribe(OnProjectOpendedAsync);
			ProjectSavingEvent.Unsubscribe(OnProjectSavingAsync);

			ProjectItemsChangedEvent.Unsubscribe(OnProjectItemsChangedAsync);
			ProjectItemRemovingEvent.Unsubscribe(OnProjectItemRemovingAsync);

			MapMemberPropertiesChangedEvent.Unsubscribe(OnMapMemberPropertiesChanged);
		}

		private async Task WorklistChanged(WorkListChangedEventArgs e)
		{
			// NOTE daro:
			// If the following code is put into a await ViewUtils.TryAsync(QueuedTask.Run(() =>{}, _msg)
			// there is a strange race condition when adding a second work list. So far the code works without it.
			// Examine it later!
			try
			{
				foreach (MapView mapView in FrameworkApplication.Panes.OfType<IMapPane>()
				                                                .Select(mapPane => mapPane.MapView))
				{
					if (mapView == null || !mapView.IsReady)
					{
						continue;
					}

					var workList = (IWorkList)e.Sender;

					Assert.True(_layersByWorklistName.ContainsKey(workList.Name),
								$"sender of {nameof(WorklistChanged)} is unknown");

					if (! _layersByWorklistName.ContainsKey(workList.Name))
					{
						return;
					}

					FeatureLayer workListLayer = _layersByWorklistName[workList.Name];

					List<long> oids = e.Items;

					if (oids != null)
					{
						// invalidate with OIDs
						mapView.Invalidate(new Dictionary<Layer, List<long>> { { workListLayer, oids } });
						continue;
					}

					Envelope extent = e.Extent ?? mapView.Extent;

					if (extent != null)
					{
						// alternatively invalidate with Envelope
						mapView.Invalidate(workListLayer, extent);
					}
				}
			}
			catch (Exception exc)
			{
				_msg.Error("Error invalidating work list layer", exc);
			}
		}

		// todo daro: move to OnMapViewInitialized?
		private void OnDrawCompleted(MapViewEventArgs e)
		{
			string uri = null;
			foreach (string name in _registry.GetNames()
			                                 .Where(name => _uriByWorklistName.TryGetValue(
				                                        name, out uri)))
			{
				// Can be null because it's from module settings and those cannot be deleted but only
				// set to null.
				if (string.IsNullOrEmpty(uri))
				{
					continue;
				}

				var worklistLayer = e.MapView.Map.FindLayer(uri) as FeatureLayer;

				// todo daro Read the custom project settings and the URI of the created work list layers.
				//			 Don't do layer name comparison.
				//			 Stop giving the work list layer the name of the work list. The map (work list uri <> work list name)
				//			 is managed with the custom project settings.
				//			 For the moment the work list layers data source remains the work list file name. It feels the right way, e.g.
				//			 in ArcGIS the data source is broken too if its name changes.
				//LayerUtils.GetLayer("work list uri");

				if (worklistLayer == null)
				{
					continue;
				}

				IWorkList workList = _registry.Get(name);
				Assert.NotNull(workList);

				// safety check, a new work list is already added
				if (_layersByWorklistName.ContainsKey(workList.Name))
				{
					continue;
				}

				_layersByWorklistName.Add(workList.Name, worklistLayer);

				WorklistChangedEvent.Subscribe(WorklistChanged, this);

				// todo daro: maybe we need a dictionary of synchronizers
				_synchronizer = new EditEventsRowCacheSynchronizer(workList);
			}
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

		private async Task OnLayerRemovingAsync(LayersRemovingEventArgs e)
		{
			await Task.Run(() =>
			{
				var flattenedLayers = new List<Layer>();

				Flatten(e.Layers, flattenedLayers);

				foreach (IWorkList worklist in GetAssociatedWorklists(flattenedLayers))
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
			});
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
			await ViewUtils.TryAsync(Task.Run(() =>
			{
				FileSystemUtils.EnsureFolderExists(GetLocalWorklistsFolder());

				foreach (IWorkList workList in GetWorklists())
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
							if (_layersByWorklistName.TryGetValue(item.WorklistName, out FeatureLayer worklistLayer))
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
			await ViewUtils.TryAsync(QueuedTask.Run(() =>
			{
				foreach (var item in e.ProjectItems.OfType<WorklistItem>())
				{
					string name = WorkListUtils.GetWorklistName(item.Path);
					Assert.NotNullOrEmpty(name);

					//Item container = Project.Current.GetProjectItemContainer(WorklistsContainer.ContainerTypeName);
					//var worklistsContainer = container as WorklistsContainer;
					//worklistsContainer?.Refresh();

					//foreach (Item cont in Project.Current.ProjectItemContainers)
					//{
					//	string contType = cont.Type;
					//	string contTypeID = cont.TypeID;
					//}

					if (_layersByWorklistName.TryGetValue(name, out FeatureLayer worklistLayer))
					{
						// this does NOT call the OnLayerRemovingAsync event handler!!o
						// OnLayerRemovingAsync is called when the layer is removes manually

						MapView.Active.Map.RemoveLayer(worklistLayer);

						foreach (IWorkList worklist in GetAssociatedWorklists(
							new Layer[] {worklistLayer}))
						{
							// no need to persist work list state, work list gets deleted
							Unload(worklist);

							Assert.True(_registry.Remove(worklist),
							            $"Cannot remove work list {worklist.Name} from registry");
						}
					}
				}
			}), _msg);

			// NOTE collapses Worklists folder, refresh of folder would be better
			//var projectItem = ProjectItemUtils.Get<MapProjectItem>().ToList();
			//List<Item> items = projectItem?.GetItems()?.ToList();
			//string folderName = "Worklists";
			//Item firstOrDefault = items?.FirstOrDefault(i => string.Equals(folderName, i.Name));
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
			string uri = LayerUtils.GetUri(mapMember);
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

		public virtual void OnWorkItemPicked(WorkItemPickArgs e)
		{
			WorkItemPicked?.Invoke(null, e);
		}

		/// <summary>
		/// Returns the work lists if there is one associated with a given work list layer
		/// </summary>
		/// <param name="layers">Any layer, doesn't have to be a work list layers</param>
		private IEnumerable<IWorkList> GetAssociatedWorklists([NotNull] IEnumerable<Layer> layers)
		{
			return _layersByWorklistName
			       .Where(pair => layers.Contains(pair.Value))
			       .Select(pair => GetWorklist(pair.Key))
			       .Where(worklist => worklist != null).ToList();
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

			WorklistChangedEvent.Unsubscribe(WorklistChanged);

			_layersByWorklistName.Remove(workList.Name);
		}

		private IEnumerable<IWorkList> GetWorklists()
		{
			return _layersByWorklistName.Select(pair => GetWorklist(pair.Key))
			                            .Where(workList => workList != null);
		}

		[CanBeNull]
		private IWorkList GetWorklist(string name)
		{
			return _registry.Get(name);
		}
	}

	public class WorkItemPickArgs : EventArgs
	{
		public List<Feature> features { get; set; }
	}
}
