using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using Clients.AGP.ProSuiteSolution.WorkListUI;
using ProSuite.AGP.Solution.WorkListUI;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkLists
{
	[UsedImplicitly]
	public class WorkListsModule : Module
	{
		private static WorkListsModule _instance;

		private WorkListRegistry _registry;
		private IList<IWorkListObserver> _observers;

		private Dictionary<IWorkList, FeatureLayer> _layerByWorkList = new Dictionary<IWorkList, FeatureLayer>();

		public static WorkListsModule Current =>
			_instance ?? (_instance =
				              (WorkListsModule) FrameworkApplication.FindModule(
					              "ProSuite_WorkList_Module"));

		#region Module overrides

		protected override bool Initialize()
		{
			_registry = WorkListRegistry.Instance;
			_observers = new List<IWorkListObserver>();
			_observers.Add(new WorkListObserver());

			WireEvents();

			return base.Initialize();
		}

		protected override void Uninitialize()
		{
			UnwireEvents();

			base.Uninitialize();
		}

		// todo daro: which one? Uninitialize? CanUnload?
		protected override bool CanUnload()
		{
			return base.CanUnload();
		}

		#endregion

		public void RegisterObserver(IWorkListObserver observer)
		{
			_observers.Add(observer);
		}

		public bool UnregisterObserver(IWorkListObserver observer)
		{
			return _observers.Remove(observer);
		}

		public IWorkList Get(string name)
		{
			return _registry.Get(name);
		}

		public IEnumerable<IWorkList> GetAll()
		{
			return _registry.GetAll();
		}

		public void ShowView(IWorkList workList)
		{
			// NOTE send a show work list request to all observers. Let the observer decide whether to show the work list.
			foreach (var observer in _observers)
			{
				observer.Show(workList);
				// ShowWorkListWindow(workList, observer);
			}
		}

		private void ShowWorkListWindow(IWorkList workList, IWorkListObserver observer)
		{
			if (observer is WorkListViewModel)
			{
				//FrameworkApplication.Current.Dispatcher.Invoke(() =>
				//	{
				//		////this does not work (viewModel is an observer and is passed to the views datacontext)
				//		//WorkListViewModel vm = observer as WorkListViewModel;
				//		//WorkListView view = new WorkListView(vm);
				//		//view.Owner = FrameworkApplication.Current.MainWindow;
				//		//view.Show();

				//		////View shows up, but of course without a viewModel as dataContext
				//		//WorkListView view = new WorkListView();
				//		//view.Owner = FrameworkApplication.Current.MainWindow;
				//		//view.Show();
			}
			//);

		}

		public void WorkListAdded(IWorkList workList)
		{
			foreach (var observer in _observers)
			{
				observer.WorkListAdded(workList);
			}
		}
		

		public void Show(IWorkList workList, LayerDocument layerTemplate)
		{
			try
			{
				// NOTE Register work list before opening plugin datasource
				// NOTE Register work list in registry and notify all observers about the registration of a new work list.

				if (_registry.GetAll().Any(wl => wl.Name == workList.Name) == false)
				{
					_registry.Add(workList);
					//foreach (var observer in _observers )
					//{
					//	observer.WorkListAdded(workList);
					//}
				}

				FeatureLayer workListLayer = AddLayer(workList.Name);
				LayerUtils.ApplyRenderer(workListLayer, layerTemplate);

				if (! _layerByWorkList.ContainsKey(workList))
				{
					_layerByWorkList.Add(workList, workListLayer);
				}

				WireEvents(workList);

				//ShowView(workList);
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}
		}

		#region only for development

		public void GoNext()
		{
			_layerByWorkList.Keys.ToList().ForEach(wl => wl.GoNext());
		}

		public void GoFirst()
		{
			_layerByWorkList.Keys.ToList().ForEach(wl => wl.GoFirst());
		}

		#endregion

		private void WireEvents()
		{
			LayersRemovingEvent.Subscribe(OnLayerRemoving);
		}

		private void UnwireEvents()
		{
			LayersRemovingEvent.Unsubscribe(OnLayerRemoving);
		}

		private void WireEvents(IWorkList workList)
		{
			workList.WorkListChanged += WorkList_WorkListChanged;
		}

		private void UnwireEvents(IWorkList workList)
		{
			workList.WorkListChanged -= WorkList_WorkListChanged;
		}

		private Task OnLayerRemoving(LayersRemovingEventArgs e)
		{
			foreach (var layer in e.Layers
			                       .OfType<FeatureLayer>()
			                       .Where(layer => _layerByWorkList.ContainsValue(layer)))
			{
				IWorkList workList = _layerByWorkList.FirstOrDefault(pair => pair.Value == layer).Key;

				UnwireEvents(workList);

				_layerByWorkList.Remove(workList);
				_registry.Remove(workList);

				// todo daro: Dispose work list or whatever is needed.
			}

			// todo daro: revise
			return Task.FromResult(0);
		}

		private void WorkList_WorkListChanged(object sender, WorkListChangedEventArgs e)
		{
			List<long> features = e.Items;

			if (features == null)
			{
				return;
			}

			try
			{
				var workList = (IWorkList) sender;
				if (! _layerByWorkList.ContainsKey(workList))
				{
					return;
				}

				FeatureLayer workListLayer = _layerByWorkList[workList];

				MapView.Active.Invalidate(new Dictionary<Layer, List<long>> { { workListLayer, features } });
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}
		}

		[CanBeNull]
		private FeatureLayer AddLayer(string workListName)
		{
			FeatureLayer workListLayer = null;
			// todo daro: exception handling
			try
			{
				PluginDatasourceConnectionPath connector = GetWorkListConnectionPath(workListName);

				// todo daro: disposing our own datastore and table?!?
				using (var datastore = new PluginDatastore(connector))
				{
					using (Table table = datastore.OpenTable(workListName))
					{
						workListLayer =
							LayerFactory.Instance.CreateFeatureLayer((FeatureClass)table,
							                                         MapView.Active.Map,
							                                         LayerPosition.AddToTop);
					}
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}

			return workListLayer;
		}

		private static PluginDatasourceConnectionPath GetWorkListConnectionPath(string workListName)
		{
			const string pluginIdentifier = "ProSuite_WorkListDatasource";

			Uri datasourcePath = GetUri(workListName);

			return new PluginDatasourceConnectionPath(pluginIdentifier, datasourcePath);
		}

		private static Uri GetUri(string workListName)
		{
			var baseUri = new Uri("worklist://localhost/");
			return new Uri(baseUri, workListName);
		}
	}
}
