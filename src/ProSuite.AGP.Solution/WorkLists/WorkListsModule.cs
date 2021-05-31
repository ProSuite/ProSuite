using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
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
		private const string PluginIdentifier = "ProSuite_WorkListDatasource";

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

		public void ShowView([NotNull] string worklistName, string displayName = null)
		{
			if (_viewsByWorklistName.TryGetValue(worklistName, out IWorkListObserver view))
			{
				view.Show(displayName);
			}
		}

		[NotNull]
		public string ShowWorklist([NotNull] WorkEnvironmentBase environment, [NotNull] string path)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));

			IWorkList worklist;

			string name = WorkListUtils.GetName(path).ToLower();
			if (_registry.Exists(name))
			{
				worklist = _registry.Get(name);
			}
			else
			{
				var factory = new XmlBasedWorkListFactory(path, name);

				if (_registry.TryAdd(factory))
				{
					_msg.Debug($"Add work list {name} from file {path}");
				}

				worklist = _registry.Get(name);
			}

			Assert.NotNull(worklist);

			if (! _viewsByWorklistName.ContainsKey(worklist.Name))
			{
				var item = ProjectItemUtils.Get<WorklistItem>(Path.GetFileName(path));

				if (item == null)
				{
					Assert.True(ProjectItemUtils.TryAdd(path, out item), $"Cannot add item {path}");
					Assert.NotNull(item);
				}

				_viewsByWorklistName.Add(worklist.Name, new WorkListObserver(worklist, item));

				Uri uri = WorkListUtils.GetDatasource(GetProject().HomeFolderPath, name,
				                                      environment.FileSuffix);
				FeatureLayer layer = AddLayer(uri, name, item.Name);

				// use item name as layer name (and as view display name as well)
				LayerUtils.ApplyRenderer(layer, environment.GetLayerDocument());
			}

			return Assert.NotNullOrEmpty(worklist.Name);
		}

		public async Task CreateWorkListAsync([NotNull] WorkEnvironmentBase environment,
		                                      [NotNull] string name)
		{
			Assert.ArgumentNotNull(environment, nameof(environment));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			Assert.False(_registry.Exists(name), $"work list {name} already exists");

			Uri uri = WorkListUtils.GetDatasource(GetProject().HomeFolderPath, name,
			                                      environment.FileSuffix);

			IWorkList worklist = await environment.CreateWorkListAsync(uri.LocalPath, name);

			if (worklist == null)
			{
				return;
			}

			// after creation go to nearest item
			worklist.GoNearest(MapView.Active.Extent);

			// Commit writes work list definition to disk.
			// Necessary for adding project item.
			worklist.Commit();

			// wiring work list events, etc. is done in OnDrawComplete
			// register work list before creating the layer
			_registry.TryAdd(worklist);

			Assert.True(ProjectItemUtils.TryAdd(uri.LocalPath, out WorklistItem item),
			            $"cannot add item {worklist.Name}");

			if (! _viewsByWorklistName.ContainsKey(worklist.Name))
			{
				_viewsByWorklistName.Add(worklist.Name, new WorkListObserver(worklist, item));
			}

			FeatureLayer layer = AddLayer(uri, name, worklist.DisplayName);

			LayerUtils.ApplyRenderer(layer, environment.GetLayerDocument());
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
		public string EnsureUniqueName()
		{
			//todo daro: use GUID as identifier?
			return $"{Guid.NewGuid()}".Replace('-', '_').ToLower();
		}

		[CanBeNull]
		private FeatureLayer AddLayer([NotNull] Uri dataSource, string worklistName,
		                              [NotNull] string layerName)
		{
			PluginDatasourceConnectionPath connector =
				new PluginDatasourceConnectionPath(PluginIdentifier, dataSource);

			using (var datastore = new PluginDatastore(connector))
			{
				using (Table table = datastore.OpenTable(worklistName))
				{
					FeatureLayer worklistLayer =
						LayerFactory.Instance.CreateFeatureLayer((FeatureClass) table,
						                                         MapView.Active.Map,
						                                         LayerPosition.AddToTop, layerName);

					LayerUtils.SetLayerSelectability(worklistLayer, false);

					if (! _uriByWorklistName.ContainsKey(worklistName))
					{
						_uriByWorklistName.Add(worklistName, LayerUtils.GetUri(worklistLayer));
					}

					return worklistLayer;
				}
			}
		}

		[NotNull]
		private static Project GetProject()
		{
			Project current = Project.Current;
			Assert.NotNull(current, "no project");
			return current;
		}

		private static string GetLocalWorklistsFolder()
		{
			return WorkListUtils.GetLocalWorklistsFolder(GetProject().HomeFolderPath);
		}

		//public void RemoveWorkListLayer(IWorkList workList)
		//{
		//	if (_layersByWorklistName.ContainsKey(workList.Name))
		//	{

		//	}
		//	if (_layerByWorkList.ContainsKey(workList))
		//	{
		//		_layerByWorkList.Remove(workList);
		//		Layer layer = MapView.Active.Map.GetLayersAsFlattenedList()
		//							 .First(l => l.Name == workList.Name);
		//		QueuedTask.Run(() => MapView.Active.Map.RemoveLayer(layer));
		//	}
		//}

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

			ProjectItemsChangedEvent.Subscribe(OnProjectItemsChanged);
			ProjectItemRemovingEvent.Subscribe(OnProjectItemRemoving);

			MapMemberPropertiesChangedEvent.Subscribe(OnMapMemberPropertiesChanged);
		}

		private void UnwireEvents()
		{
			//MapViewInitializedEvent.Unsubscribe(OnMapViewInitialized);
			LayersRemovingEvent.Unsubscribe(OnLayerRemovingAsync);
			DrawCompleteEvent.Unsubscribe(OnDrawCompleted);

			ProjectOpenedAsyncEvent.Unsubscribe(OnProjectOpendedAsync);
			ProjectSavingEvent.Unsubscribe(OnProjectSavingAsync);

			ProjectItemsChangedEvent.Unsubscribe(OnProjectItemsChanged);
			ProjectItemRemovingEvent.Unsubscribe(OnProjectItemRemoving);

			MapMemberPropertiesChangedEvent.Unsubscribe(OnMapMemberPropertiesChanged);
		}

		private void WireEvents(IWorkList workList)
		{
			workList.WorkListChanged += WorkList_WorkListChanged;
		}

		private void UnwireEvents(IWorkList workList)
		{
			workList.WorkListChanged -= WorkList_WorkListChanged;
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

				WireEvents(workList);

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

					string uri = LayerUtils.GetUri(mapMember);
					string name = WorkListUtils.ParseName(uri);

					if (! _viewsByWorklistName.TryGetValue(name, out IWorkListObserver view))
					{
						continue;
					}

					if (view.View == null)
					{
						continue;
					}

					ViewUtils.RunOnUIThread(() => { view.View.Title = mapMember.Name; });
				}
			}, _msg);
		}

		private async Task OnLayerRemovingAsync(LayersRemovingEventArgs e)
		{
			await Task.Run(() =>
			{
				foreach (IWorkList worklist in GetAssociatedWorklists(e.Layers))
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

			await Task.Run(() =>
			{
				foreach (var item in ProjectItemUtils.Get<WorklistItem>())
				{
					var factory = new XmlBasedWorkListFactory(item.Path, item.WorklistName);

					if (_registry.TryAdd(factory))
					{
						_msg.Debug($"Add work list {item.WorklistName} from file {item.Path}");
					}
				}
			});
		}

		private async Task OnProjectSavingAsync(ProjectEventArgs arg)
		{
			await Task.Run(() =>
			{
				FileSystemUtils.EnsureFolderExists(GetLocalWorklistsFolder());

				foreach (IWorkList workList in GetWorklists())
				{
					workList.Commit();
				}
			});
		}

		private void OnProjectItemsChanged(ProjectItemsChangedEventArgs e)
		{
			if (e.ProjectItemsCollection != null)
			{
				foreach (var item in e.ProjectItemsCollection.OfType<WorklistItem>())
				{
					switch (e.Action)
					{
						case NotifyCollectionChangedAction.Replace:

							Assert.NotNullOrEmpty(item.WorklistName);
							Assert.True(_registry.Exists(item.WorklistName),
							            $"Cannot find work list {item.WorklistName} in registry");

							IWorkList workList = _registry.Get(item.WorklistName);
							Assert.NotNull(workList);

							workList.Repository.UpdateStateRepository(item.Path);

							break;
						case NotifyCollectionChangedAction.Add:
						case NotifyCollectionChangedAction.Remove:
						case NotifyCollectionChangedAction.Move:
						case NotifyCollectionChangedAction.Reset:
							throw new NotImplementedException();
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
		}

		private async Task OnProjectItemRemoving(ProjectItemRemovingEventArgs e)
		{
			await ViewUtils.TryAsync(QueuedTask.Run(() =>
			{
				foreach (var item in e.ProjectItems.OfType<WorklistItem>())
				{
					string name = WorkListUtils.GetName(item.Path);
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

		private void WorkList_WorkListChanged(object sender, WorkListChangedEventArgs e)
		{
			List<long> oids = e.Items;

			if (oids == null)
			{
				return;
			}

			try
			{
				var workList = (IWorkList) sender;

				Assert.True(_layersByWorklistName.ContainsKey(workList.Name),
				            $"sender of {nameof(WorkList_WorkListChanged)} is unknown");

				if (! _layersByWorklistName.ContainsKey(workList.Name))
				{
					return;
				}

				FeatureLayer workListLayer = _layersByWorklistName[workList.Name];

				MapView.Active.Invalidate(new Dictionary<Layer, List<long>>
				                          {{workListLayer, oids}});
			}
			catch (Exception exc)
			{
				_msg.Error("Error invalidating work list layer", exc);
			}
		}

		#endregion

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

			UnwireEvents(workList);

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
