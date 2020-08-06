using System;
using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace Clients.AGP.ProSuiteSolution.WorkLists
{
	[UsedImplicitly]
	public class WorkListsModule : Module
	{
		private static WorkListsModule _instance;

		private WorkListCentral _central;
		private FeatureLayer _workListLayer;
		private string _workListName;
		private IWorkList _workList;

		public static WorkListsModule Current =>
			_instance ?? (_instance =
				              (WorkListsModule) FrameworkApplication.FindModule(
								  "ProSuite_WorkList_Module"));

		public WorkListCentral Central => _central ?? (_central = new WorkListCentral());

		public void Show(IWorkList workList, LayerDocument template)
		{
			_workList = workList;
			_workListName = workList.Name;

			try
			{
				// NOTE register work list before opening plugin datasource
				Central.Set(workList);

				//FeatureLayerCreationParams creationParams = CreateLayer(_workListName);
				//_workListLayer = AddLayer(creationParams);

				_workListLayer = AddLayer(_workListName);

				LayerUtils.ApplyRenderer(_workListLayer, template);

				Central.Show(workList);

				WireEvents();
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}
		}

		private void WireEvents()
		{
			_workList.WorkListChanged += _workList_WorkListChanged;
		}

		public void GoNext()
		{
			// todo daro: exception handling
			try
			{
				Assert.NotNull(Central.Get(_workListName)).GoNext();
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}
		}

		public void GoFirst()
		{
			try
			{
				Assert.NotNull(Central.Get(_workListName)).GoFirst();
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}
		}

		private static FeatureLayer AddLayer(LayerCreationParams workListLayer)
		{
			// todo daro: inline
			FeatureLayer featureLayer = null;
			try
			{
				featureLayer = LayerFactory.Instance.CreateLayer<FeatureLayer>(workListLayer, MapView.Active.Map, LayerPosition.AddToTop);
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}
			return featureLayer;
		}

		[CanBeNull]
		private FeatureLayer AddLayer(string workListName)
		{
			FeatureLayer workListLayer = null;
			// todo daro: exception handling
			try
			{
				PluginDatasourceConnectionPath connector = GetWorkListConnectionPath(workListName);

				using (var datastore = new PluginDatastore(connector))
				{
					foreach (string name in datastore.GetTableNames())
					{
						using (Table table = datastore.OpenTable(name))
						{
							workListLayer = LayerFactory.Instance.CreateFeatureLayer((FeatureClass)table,
																					 MapView.Active.Map,
																					 LayerPosition.AddToTop);
						}
					}
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}
			return workListLayer;
		}

		[CanBeNull]
		private FeatureLayerCreationParams CreateLayer([NotNull] string workListName)
		{
			FeatureLayerCreationParams result = null;
			PluginDatastore datastore = null;
			Table table = null;

			try
			{
				PluginDatasourceConnectionPath connector = GetWorkListConnectionPath(workListName);

				datastore = new PluginDatastore(connector);
				table = datastore.OpenTable(workListName);

				result = LayerUtils.CreateLayerParams((FeatureClass)table);
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}
			finally
			{
				datastore?.Dispose();
				table?.Dispose();
			}

			return result;
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

		private void _workList_WorkListChanged(object sender, WorkListChangedEventArgs e)
		{
			List<long> features = e.Items;

			if (features == null)
			{
				return;
			}

			try
			{
				MapView.Active.Invalidate(new Dictionary<Layer, List<long>> { { _workListLayer, features } });
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
			}
		}
	}
}
