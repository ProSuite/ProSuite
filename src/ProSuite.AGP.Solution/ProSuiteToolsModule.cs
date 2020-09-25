using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Catalog;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using Clients.AGP.ProSuiteSolution;
using Clients.AGP.ProSuiteSolution.Commons;
using Clients.AGP.ProSuiteSolution.ConfigUI;
using ProSuite.AGP.Solution.LoggerUI;
using ProSuite.AGP.Solution.ProjectItem;
using ProSuite.AGP.Solution.WorkListTrials;
using ProSuite.AGP.Solution.WorkListUI;
using ProSuite.Commons.Logging;
using ProSuite.QA.Configurator;
using ProSuite.QA.ServiceManager;
using ProSuite.QA.ServiceManager.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProSuite.AGP.Solution
{
	internal class ProSuiteToolsModule : Module
	{
		public static event EventHandler<ProSuiteQAConfigEventArgs> OnQAConfigurationChanged;

		private static ProSuiteQAManager _qaManager = null;
		public static ProSuiteQAManager QAManager
		{
			get
			{
				if (_qaManager == null)
				{
					_qaManager = new ProSuiteQAManager(
						QAConfiguration.Current.GetQAServiceProviders(QAProjectItem?.ServerConfigurations),
						QAConfiguration.Current.GetQASpecificationsProvider(QAProjectItem?.SpecificationConfiguration));
					_qaManager.OnStatusChanged += QAManager_OnStatusChanged;
					
					OnQAConfigurationChanged = _qaManager.OnConfigurationChanged;
				}
				return _qaManager;
			}
		}

		private static ProSuiteProjectItemConfiguration _qaProjectItem = null;
		public static ProSuiteProjectItemConfiguration QAProjectItem
		{
			get
			{
				if (_qaProjectItem == null)
				{
					_msg.Info("Project item not available");

					_qaProjectItem = Project.Current.GetItems<ProSuiteProjectItemConfiguration>().FirstOrDefault();
					if (_qaProjectItem == null)
					{
						//_qaProjectItem = new ProSuiteProjectItem(QAConfiguration.Current.DefaultQAServiceConfig,
						//                                         QAConfiguration.Current.DefaultQASpecConfig);

						//ProSuiteProjectItemManager.Current.SaveProjectItem(Project.Current, _qaProjectItem);
					}
				}
				return _qaProjectItem;
			}
			set
			{
				_qaProjectItem = value;
				UpdateServiceUI(_qaProjectItem);
			}
		}

		private static IList<FeatureLayer> _errorLayers;
		public static IList<FeatureLayer> ErrorLayers
		{
			get { return _errorLayers; }
			set
			{
				//SetProperty(ref _errorLayers, value, () => ErrorLayers);
				_errorLayers = value;
				//if (_errorLayers == null) return;
			}
		}

		private static ProSuiteToolsModule _this = null;

		private static IMsg msg = null;
		private static IMsg _msg
		{
			get
			{
				if (msg == null)
					msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
				return msg;
			}
			set => msg = value;
		}
		private const string _loggingConfigFile = "prosuite.logging.agp.xml";

		/// <summary>
		/// Retrieve the singleton instance to this module here
		/// </summary>
		public static ProSuiteToolsModule Current
		{
			get
			{
				return _this ?? (_this = (ProSuiteToolsModule)FrameworkApplication.FindModule("ProSuiteSolution_Module"));
			}
		}

		private static void UpdateServiceUI(ProSuiteProjectItemConfiguration projectItem)
		{

			var localService = projectItem.ServerConfigurations.FirstOrDefault(s => (s.ServiceType == ProSuiteQAServiceType.GPLocal && s.IsValid));
			if (localService != null)
				FrameworkApplication.State.Activate(ConfigIDs.QA_GPLocal_State);
			else
				FrameworkApplication.State.Deactivate(ConfigIDs.QA_GPLocal_State);

			var serverService = projectItem.ServerConfigurations.FirstOrDefault(s => (s.ServiceType == ProSuiteQAServiceType.GPService && s.IsValid));
			if (serverService != null)
				FrameworkApplication.State.Activate(ConfigIDs.QA_GPService_State);
			else
				FrameworkApplication.State.Deactivate(ConfigIDs.QA_GPService_State);

		}

		#region Overrides
		/// <summary>
		/// Initialize logic for the custom module
		/// </summary>
		/// <returns></returns>
		protected override bool Initialize()
		{
			InitLoggerConfiguration();

			//ProjectItemsChangedEvent.Subscribe(OnProjectItemsChanged);

			LayersAddedEvent.Subscribe(OnLayerAdded);
			ProSuiteConfigChangedEvent.Subscribe(OnConfigurationChanged);

			return base.Initialize();
		}

		private void InitLoggerConfiguration()
		{
			LoggingConfigurator.UsePrivateConfiguration = false;
			LoggingConfigurator.Configure(_loggingConfigFile);

			// this will instantiate IMsg (should be after log4net configuration) 
			_msg.Debug("Logging configured");

		}

		/// <summary>
		/// Uninitialize method.  Make sure the module unsubscribes from the events.
		/// </summary>
		protected override void Uninitialize()
		{
			base.Uninitialize();

			//ProSuitePro.ProSuiteManager.QAManager.OnStatusChanged -= QAManager_OnStatusChanged;
			//ProjectItemsChangedEvent.Unsubscribe(OnProjectItemsChanged);
			LayersAddedEvent.Unsubscribe(OnLayerAdded);
			ProSuiteConfigChangedEvent.Subscribe(OnConfigurationChanged);
		}

		/// <summary>
		/// Called by Framework when ArcGIS Pro is closing
		/// </summary>
		/// <returns>False to prevent Pro from closing, otherwise True</returns>
		protected override bool CanUnload()
		{
			//TODO - add your business logic
			//return false to ~cancel~ Application close
			return true;
		}
		#endregion


		#region Event handlers 

		private static void QAManager_OnStatusChanged(object sender, ProSuiteQAServiceEventArgs e)
		{
			//_msg.Info($"ProSuiteModule: {e.State}");
		}

		private void OnProjectItemsChanged(ProjectItemsChangedEventArgs obj)
		{
			//_msg.Info($"OnProjectItemsChanged Name: {obj.ProjectItem.Name} Action: {obj.Action}");
		}

		private void OnLayerAdded(LayerEventsArgs obj)
		{
			_msg.Info($"OnLayerAdded event");

			// TODO update available QA specifications - bind combo box to list?
		}

		private void OnConfigurationChanged(ProSuiteConfigEventArgs configArgs)
		{
			// save changed configuration
			QAProjectItem.ServerConfigurations = configArgs.ServerConfigurations;
			QAProjectItem.SpecificationConfiguration = configArgs.SpecificationsConfiguration;

			UpdateServiceUI(QAProjectItem);

			// notify QAManager than config is changed via
			OnQAConfigurationChanged?.Invoke(this, new ProSuiteQAConfigEventArgs(configArgs.ServerConfigurations));
		}

		internal static void StartQAGPServer(ProSuiteQAServiceType type)
		{
			_msg.Info($"StartQAGPServer is called");
			var response = QAManager.StartQATesting(new ProSuiteQARequest(type));
			_msg.Info($"StartQAGPServer is ended");
		}

		#endregion

		internal static async Task StartQAGPServerAsync(ProSuiteQAServiceType type)
		{
			_msg.Info($"StartQAGPServerAsync is called");

			// TODO get envelope, selected data, selected QA spec
			var response = await QAManager.StartQATestingAsync(new ProSuiteQARequest(type));
			if (response.Error == ProSuiteQAError.None)
			{
				_msg.Info($"StartQAGPServerAsync result {response?.ResponseData}");

				if (response?.ResponseData != null)
				{
					// TODO response data can be not only string
					ErrorLayers = LayerUtils.AddFeaturesToMap("QA Error issues",
						response?.ResponseData.ToString(),
						"issues.gdb",
						new List<string>() { "IssuePoints", "IssueLines", "IssueMultiPatches", "IssuePolygons" },
						true);
				}
			}
			else
			{
				_msg.Error($"StartQAGPServerAsync is failed");
			}
		}
	}

	internal class StartQAGPTool : Button
	{
		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected override async void OnClick()
		{
			try
			{
				await ProSuiteToolsModule.StartQAGPServerAsync(ProSuiteQAServiceType.GPService);
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message);
			}
		}
	}

	internal class StartQAGPExtent : Button
	{
		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected override async void OnClick()
		{
			try
			{
				await ProSuiteToolsModule.StartQAGPServerAsync(ProSuiteQAServiceType.GPLocal);
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message);
			}
		}
	}

	internal class StartQAErrorsDockPane : Button
	{
		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		StartQAErrorsDockPane()
		{
			//Enabled = false;
		}

		protected override void OnClick()
		{
			try
			{
				// because of VS2019 problems simple test here
				QueuedTask.Run(() =>
				{
					ProSuiteLogPaneViewModel.GenerateMockMessages(10000);
				});

				// temporary solution for WorkList
				//QueuedTask.Run(() =>
				//{
				//var pane = FrameworkApplication.DockPaneManager.Find("esri_dataReviewer_evaluateFeaturesPane");
				//	bool visible = pane.IsVisible;
				//	pane.Activate();
				//});

				//MessageBox.Show("QA error handler view is not yet implemented");
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message);
			}
		}

	}

	internal class ShowConfigWindow : Button
	{
		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private ProSuiteConfigDialog _prosuiteconfigdialog = null;

		protected override void OnClick()
		{
			//already open?
			if (_prosuiteconfigdialog != null)
				return;

			// clone 
			var tempQAConfiguration = ProSuiteToolsModule.QAProjectItem.ServerConfigurations.ToList().ConvertAll(x => new ProSuiteQAServerConfiguration(x));

			_prosuiteconfigdialog = new ProSuiteConfigDialog();
			_prosuiteconfigdialog.Owner = FrameworkApplication.Current.MainWindow;
			_prosuiteconfigdialog.DataContext = new ProSuiteConfigViewModel(tempQAConfiguration);
			_prosuiteconfigdialog.Closed += (o, e) => { _prosuiteconfigdialog = null; };

			if (_prosuiteconfigdialog.ShowDialog() ?? true)
			{
				ProSuiteConfigChangedEvent.Publish(new ProSuiteConfigEventArgs(tempQAConfiguration, null));
			}
		}
	}

	internal class AddWorkListFile : Button
	{
		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		// TODO algr: temporary tests
		protected override void OnClick()
		{
			var bf = new BrowseProjectFilter();
			bf.AddCanBeTypeId("ProSuiteItem_ProjectItemWorkListFile"); //TypeID for the ".wlist" custom project item

			// for subitem allow to browsw inside and add as type
			//bf.AddDoBrowseIntoTypeId("ProSuiteItem_ProjectItemWorkListFile");
			//bf.AddCanBeTypeId("ProSuiteItem_WorkListItem"); //subitem 
			bf.Name = "Work List";

			var openItemDialog = new OpenItemDialog
			        {
	                     Title = "Add Work List",
	                     InitialLocation = @"c:\data",
	                     BrowseFilter = bf
                     };
			bool? result = openItemDialog.ShowDialog();
			if (result != null && (result.Value == false || !openItemDialog.Items.Any())) return;

			var item = openItemDialog.Items.ToArray()[0];
			var filePath = item.Path;

			// tests 
			ProSuiteProjectItemManager.Current.AddFileToProject(filePath, Project.Current, ProjectItemType.WorkListDefinition);
		}
	}


	internal class ShowWorkListWindow : Button
	{
		private WorkListView _worklist = null;

		protected override async void OnClick()
		{
			await QueuedTask.Run(() => {CreateTestList(); });
			 
			//already open?
			if (_worklist != null)
				return;
			_worklist = new WorkListView();
			_worklist.Owner = FrameworkApplication.Current.MainWindow;
			_worklist.Closed += (o, e) => { _worklist = null; };
			_worklist.Show();
			//uncomment for modal
			//_worklist.ShowDialog();
		}

		private static void CreateTestList()
		{
			var workList = WorkListTrialsModule.Current.GetTestWorkList();
			var workListName = workList.Name;

			var connector = WorkListTrialsModule.Current.GetWorkListConnectionPath(workListName);

			using (var datastore = new PluginDatastore(connector))
			{
				var tableNames = datastore.GetTableNames();
				foreach (var tableName in tableNames)
				{
					using (var table = datastore.OpenTable(tableName))
					{
						LayerFactory.Instance.CreateFeatureLayer(
							(FeatureClass)table, MapView.Active.Map);

						//TODO set renderer using error worklist layer file
						//var layerDocument = new LayerDocument(@"C:\git\EsriCH.ArcGISPro.Trials\WorkListPrototype\TopgisConfiguration\TestData\Work List edited.lyrx");
						//CIMLayerDocument cimLayerDocument = layerDocument.GetCIMLayerDocument();
						//var rendererFromLayerFile = ((CIMFeatureLayer)cimLayerDocument.LayerDefinitions[0]).Renderer as CIMUniqueValueRenderer;

						//featureLayer?.SetRenderer(rendererFromLayerFile);
					}
				}
			}
		}
	}



	sealed class QASpecListComboBox : ArcGIS.Desktop.Framework.Contracts.ComboBox
	{
		public QASpecListComboBox()
		{
			FillCombo();
			Enabled = false;
		}

		private void FillCombo()
		{
			foreach (var qaSpec in ProSuiteToolsModule.QAManager.GetSpecificationNames())
			{
				Add(new ComboBoxItem(qaSpec));
			}

			// Select first item
			SelectedItem = ItemCollection.FirstOrDefault();
		}

		protected override void OnSelectionChange(ComboBoxItem item)
		{
			
		}
		
	}
}
