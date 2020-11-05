using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
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
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	public class WorkListsModule : Module, IWorkListContext
	{
		private const string PluginIdentifier = "ProSuite_WorkListDatasource";
		private const string WorklistsFolder = "Worklists";
		private const string FileSuffix = ".xml.wl";

		private static WorkListsModule _instance;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly Dictionary<IWorkList, FeatureLayer> _layerByWorkList =
			new Dictionary<IWorkList, FeatureLayer>();

		[NotNull] private readonly IList<IWorkListObserver> _observers =
			new List<IWorkListObserver>();

		private IWorkListRegistry _registry;
		[CanBeNull] private EditEventsRowCacheSynchronizer _synchronizer;

		public static WorkListsModule Current =>
			_instance ?? (_instance =
				              (WorkListsModule) FrameworkApplication.FindModule(
					              "ProSuite_WorkList_Module"));

		public Dictionary<IWorkList, FeatureLayer> LayerByWorkList
		{
			get { return _layerByWorkList; }
		}

		public event EventHandler<WorkItemPickArgs> WorkItemPicked;

		public void RegisterObserver([NotNull] IWorkListObserver observer)
		{
			_observers.Add(observer);
		}

		public bool UnregisterObserver([NotNull] IWorkListObserver observer)
		{
			return _observers.Remove(observer);
		}

		public void ShowView([NotNull] string uniqueName)
		{
			IWorkList list = _registry.Get(uniqueName);
			// NOTE send a show work list request to all observers. Let the observer decide whether to show the work list.
			foreach (IWorkListObserver observer in _observers)
			{
				observer.Show(list);
			}
		}

		public void CreateWorkList([NotNull] WorkEnvironmentBase environment)
		{
			try
			{
				IWorkList workList = environment.CreateWorkList(this);

				// wiring work list events, etc. is done in OnDrawComplete
				// register work list before creating the layer
				_registry.Add(workList);

				CreateLayer(environment, workList.Name);

				RegisterObserver(new WorkListViewModel(workList));
			}
			catch (Exception e)
			{
				_msg.Error("Create work list failed", e);
			}
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

			_layerByWorkList.Keys.ToList().ForEach(workList => workList.Dispose());

			UnwireEvents();

			base.Uninitialize();
		}

		#endregion

		#region IWorkListContext overrides

		[NotNull]
		public string GetPath([NotNull] string workListName)
		{
			// todo daro: use ConfigurationUtils?
			return GetUri(workListName).LocalPath;
		}

		[NotNull]
		public string EnsureUniqueName([NotNull] string workListName)
		{
			//todo daro: use GUID as identifier?
			return $"{workListName}_{Guid.NewGuid()}";
		}

		#endregion

		private void CreateLayer([NotNull] WorkEnvironmentBase environment,
		                         [NotNull] string name)
		{
			// todo daro: inline
			LayerDocument layerTemplate = environment.GetLayerDocument();
			LayerUtils.ApplyRenderer(AddLayer(name), layerTemplate);
		}

		[CanBeNull]
		private FeatureLayer AddLayer([NotNull] string workListName)
		{
			PluginDatasourceConnectionPath connector = GetWorkListConnectionPath(workListName);

			// todo daro: disposing our own datastore and table?!?
			using (var datastore = new PluginDatastore(connector))
			{
				using (Table table = datastore.OpenTable(workListName))
				{
					FeatureLayer workListLayer =
						LayerFactory.Instance.CreateFeatureLayer((FeatureClass) table,
						                                         MapView.Active.Map,
						                                         LayerPosition.AddToTop);

					Commons.LayerUtils.SetLayerSelectability(workListLayer, false);

					return workListLayer;
				}
			}
		}
		
		[NotNull]
		private PluginDatasourceConnectionPath GetWorkListConnectionPath(
			[NotNull] string workListName)
		{
			return new PluginDatasourceConnectionPath(PluginIdentifier, GetUri(workListName));
		}

		[NotNull]
		private Uri GetUri([NotNull] string workListName)
		{
			//var baseUri = new Uri("worklist://localhost/");
			string folder = GetLocalWorklistsFolder();
			EnsureFolderExists(folder);

			return new Uri(Path.Combine(folder, $"{workListName}{FileSuffix}"));
		}

		[NotNull]
		private static IEnumerable<string> GetDefinitionFiles()
		{
			string folder = GetLocalWorklistsFolder();
			EnsureFolderExists(folder);

			return Directory.GetFiles(folder, $"*{FileSuffix}", SearchOption.TopDirectoryOnly);
		}

		[NotNull]
		private static string GetLocalWorklistsFolder()
		{
			Project current = Project.Current;
			// todo daro: remove assertion
			Assert.NotNull(current, "no project");

			return Path.Combine(current.HomeFolderPath, WorklistsFolder);
		}

		private static void EnsureFolderExists([NotNull] string path)
		{
			if (Directory.Exists(path))
			{
				return;
			}

			DirectoryInfo info = Directory.CreateDirectory(path);
			_msg.Debug($"Create folder {info.FullName}");
		}

		//public void RemoveWorkListLayer(IWorkList workList)
		//{
		//	if (_layerByWorkList.ContainsKey(workList))
		//	{
		//		_layerByWorkList.Remove(workList);
		//		Layer layer = MapView.Active.Map.GetLayersAsFlattenedList()
		//		                     .First(l => l.Name == workList.Name);
		//		QueuedTask.Run(() => MapView.Active.Map.RemoveLayer(layer));
		//	}
		//}

		#region Events

		private void WireEvents()
		{
			LayersRemovingEvent.Subscribe(OnLayerRemovingAsync);
			DrawCompleteEvent.Subscribe(OnDrawCompleted);

			ProjectOpenedAsyncEvent.Subscribe(OnProjectOpendedAsync);
			ProjectSavingEvent.Subscribe(OnProjectSavingAsync);
		}

		private void UnwireEvents()
		{
			LayersRemovingEvent.Unsubscribe(OnLayerRemovingAsync);
			DrawCompleteEvent.Unsubscribe(OnDrawCompleted);

			ProjectOpenedAsyncEvent.Unsubscribe(OnProjectOpendedAsync);
			ProjectSavingEvent.Unsubscribe(OnProjectSavingAsync);
		}

		private void WireEvents(IWorkList workList)
		{
			workList.WorkListChanged += WorkList_WorkListChanged;
		}

		private void UnwireEvents(IWorkList workList)
		{
			workList.WorkListChanged -= WorkList_WorkListChanged;
		}

		private void OnDrawCompleted(MapViewEventArgs e)
		{
			IReadOnlyList<Layer> layers = e.MapView.Map.GetLayersAsFlattenedList();
			
			foreach (string name in _registry.GetNames())
			{
				// todo daro: need a more robust layer identifier
				// check first whether work list layer is in TOC
				FeatureLayer workListLayer = layers.OfType<FeatureLayer>()
				                                   .FirstOrDefault(layer => string.Equals(layer.Name, name));

				if (workListLayer == null)
				{
					continue;
				}

				IWorkList workList = _registry.Get(name);
				Assert.NotNull(workList);

				// safety check, a new work list is already added
				if (_layerByWorkList.ContainsKey(workList))
				{
					continue;
				}

				_layerByWorkList.Add(workList, workListLayer);

				WireEvents(workList);

				// todo daro: maybe we need a dictionary of synchronizers
				_synchronizer = new EditEventsRowCacheSynchronizer(workList);
			}
		}

		private async Task OnLayerRemovingAsync(LayersRemovingEventArgs e)
		{
			// todo daro: revise usage of Task
			await Task.Run(() =>
			{
				foreach (IWorkList workList in _layerByWorkList
				                               .Where(pair => e.Layers.Contains(pair.Value))
				                               .Select(pair => pair.Key).ToList())
				{
					// ensure folder exists before commit
					EnsureFolderExists(GetLocalWorklistsFolder());

					workList.Commit();

					UnwireEvents(workList);

					_layerByWorkList.Remove(workList);
					_registry.Remove(workList);

					//foreach (Window window in Application.Current.Windows)
					//{
					//	if (window.Title == workList.Name)
					//	{
					//		var vm = window.DataContext as WorkListViewModel;
					//		UnregisterObserver(vm);
					//		window.Close();
					//	}
					//}

					// todo daro: Dispose work list or whatever is needed.
				}
			});
		}

		private async Task OnProjectOpendedAsync(ProjectEventArgs e)
		{
			// todo daro:
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

			// todo daro: later this is replaced with custom project items

			
			// todo daro QueuedTask needed?
			await QueuedTask.Run(() =>
			{
				// todo daro: use ConfigurationUtils?
				// todo daro: revise!
				foreach (string path in GetDefinitionFiles())
				//foreach (var path in ProjectRepository.Current.GetProjectFileItems(ProjectItemType.WorkListDefinition))
				{
					string workListName = WorkListUtils.GetName(path);
					var factory = new XmlBasedWorkListFactory(path, workListName);

					Assert.True(_registry.TryAdd(factory), $"work list {factory.Name} already added");
				}
			});
		}

		private async Task OnProjectSavingAsync(ProjectEventArgs arg)
		{
			// todo daro: revise usage of Task
			await Task.Run(() =>
			{
				EnsureFolderExists(GetLocalWorklistsFolder());

				foreach (IWorkList workList in _layerByWorkList.Keys)
				{
					workList.Commit();
				}
			});
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

				// todo daro: remove assertion
				Assert.True(_layerByWorkList.ContainsKey(workList),
				            $"sender of {nameof(WorkList_WorkListChanged)} is unknown");

				if (! _layerByWorkList.ContainsKey(workList))
				{
					return;
				}

				FeatureLayer workListLayer = _layerByWorkList[workList];

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
	}

	public class WorkItemPickArgs : EventArgs
	{
		public List<Feature> features { get; set; }
	}
}
