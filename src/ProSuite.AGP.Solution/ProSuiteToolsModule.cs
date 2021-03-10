using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Solution.ConfigUI;
using ProSuite.AGP.Solution.LoggerUI;
using ProSuite.AGP.Solution.ProjectItem;
using ProSuite.AGP.Solution.WorkLists;
using ProSuite.AGP.WorkList;
using ProSuite.Application.Configuration;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client;
using ProSuite.Microservices.Client.AGP;
using ProSuite.QA.Configurator;
using ProSuite.QA.ServiceManager;
using ProSuite.QA.ServiceManager.Types;
using Module = ArcGIS.Desktop.Framework.Contracts.Module;

namespace ProSuite.AGP.Solution
{
	[UsedImplicitly]
	internal class ProSuiteToolsModule : Module
	{
		public static event EventHandler<ProSuiteQAConfigEventArgs> OnQAConfigurationChanged;

		private static ProSuiteQAManager _qaManager;

		public static ProSuiteQAManager QAManager
		{
			get
			{
				if (_qaManager != null)
				{
					return _qaManager;
				}

				_qaManager = new ProSuiteQAManager(
					QAConfiguration.Current.GetQAServiceProviders(
						QAProjectItem?.ServerConfigurations),
					QAConfiguration.Current.GetQASpecificationsProvider(
						QAProjectItem?.SpecificationConfiguration));
				_qaManager.OnStatusChanged += QAManager_OnStatusChanged;

				OnQAConfigurationChanged = _qaManager.OnConfigurationChanged;
				return _qaManager;
			}
		}

		private static ProSuiteProjectItemConfiguration _qaProjectItem = null;

		public static ProSuiteProjectItemConfiguration QAProjectItem
		{
			get
			{
				if (_qaProjectItem != null)
				{
					return _qaProjectItem;
				}
				//_msg.Info("Project item not available");

				_qaProjectItem = Project.Current.GetItems<ProSuiteProjectItemConfiguration>()
				                        .FirstOrDefault();
				if (_qaProjectItem == null)
				{
					_qaProjectItem = new ProSuiteProjectItemConfiguration(
						QAConfiguration.Current.DefaultQAServiceConfig,
						QAConfiguration.Current.DefaultQASpecConfig);

					UpdateServiceUI(_qaProjectItem);

					//ProSuiteProjectItemManager.Current.SaveProjectItem(Project.Current, _qaProjectItem);
				}

				return _qaProjectItem;
			}
			set
			{
				_qaProjectItem = value;
				UpdateServiceUI(_qaProjectItem);
			}
		}

		public static string CurrentQASpecificationName { get; set; }

		private static ProSuiteToolsModule _this = null;

		private static IMsg msg = null;

		private static IMsg _msg
		{
			get
			{
				if (msg == null)
					msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);
				return msg;
			}
			set => msg = value;
		}

		private const string _loggingConfigFile = "prosuite.logging.arcgispro.xml";

		public GeometryProcessingClient ToolMicroserviceClient { get; private set; }

		/// <summary>
		/// Retrieve the singleton instance to this module here
		/// </summary>
		public static ProSuiteToolsModule Current
		{
			get
			{
				return _this ?? (_this = (ProSuiteToolsModule) FrameworkApplication.FindModule(
					                 "ProSuiteSolution_Module"));
			}
		}

		private static void UpdateServiceUI(ProSuiteProjectItemConfiguration projectItem)
		{
			var localService =
				projectItem.ServerConfigurations.FirstOrDefault(
					s => (s.ServiceType == ProSuiteQAServiceType.GPLocal && s.IsValid));
			if (localService != null)
				FrameworkApplication.State.Activate(ConfigIDs.QA_GPLocal_State);
			else
				FrameworkApplication.State.Deactivate(ConfigIDs.QA_GPLocal_State);

			var serverService =
				projectItem.ServerConfigurations.FirstOrDefault(
					s => (s.ServiceType == ProSuiteQAServiceType.GPService && s.IsValid));
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
			LogMessageActionEvent.Subscribe(OnLogMessageActionRequested);

			StartToolMicroserviceClientAsync().GetAwaiter();

			return base.Initialize();
		}

		private void InitLoggerConfiguration()
		{
			LoggingConfigurator.UsePrivateConfiguration = false;
			AppLoggingConfigurator.Configure(_loggingConfigFile);

			// this will instantiate IMsg (should be after log4net configuration) 
			_msg.Debug("Logging configured");
		}

		/// <summary>
		/// Uninitialize method.  Make sure the module unsubscribes from the events.
		/// </summary>
		protected override void Uninitialize()
		{
			base.Uninitialize();

			LayersAddedEvent.Unsubscribe(OnLayerAdded);
			ProSuiteConfigChangedEvent.Unsubscribe(OnConfigurationChanged);
			LogMessageActionEvent.Unsubscribe(OnLogMessageActionRequested);

			ToolMicroserviceClient?.Disconnect();
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
			OnQAConfigurationChanged?.Invoke(
				this, new ProSuiteQAConfigEventArgs(configArgs.ServerConfigurations));
		}

		private void OnLogMessageActionRequested(LogMessageActionEventArgs logActionArgs)
		{
			if (logActionArgs.MessageAction == LogMessageAction.Details)
			{
				// TODO create dialog only once?
				var _prosuiteconfigdialog = new LogMessageDetailsDialog();
				var logDetailsViewModel = new LogMessageDetailsViewModel(logActionArgs.LogMessage);
				_prosuiteconfigdialog.DataContext = logDetailsViewModel;
				if (_prosuiteconfigdialog.ShowDialog() ?? true)
				{
					Clipboard.SetText(logDetailsViewModel.ClipboardMessage);
					_msg.Debug("Log message copied into clipboard");
				}
			}
			else
				_msg.Debug("Unkown LogMessage action");
		}

		internal static void StartQAGPServer(ProSuiteQAServiceType type)
		{
			_msg.Info($"StartQAGPServer is called");

			// TODO temporary 
			var serviceConfig = _qaProjectItem.ServerConfigurations.FirstOrDefault(
				c => c.ServiceType == ProSuiteQAServiceType.GPService);
			if (serviceConfig == null)
			{
				_msg.Error($"Server config does not exist");
				return;
			}

			var xml = @"\\vsdev2414\prosuite_server_trials\xml\polygonCovering.qa.xml";

			var qaParams =
				$"{serviceConfig.ServiceConnection},{xml},{serviceConfig.DefaultTileSize},,,{serviceConfig.DefaultOutputFolder},,,,{serviceConfig.DefaultCompressValue}";
			var response = QAManager.StartQATesting(new ProSuiteQARequest(type, qaParams));
			_msg.Info($"StartQAGPServer is ended");
		}

		#endregion

		internal static async Task StartQAGPServerAsync(ProSuiteQAServiceType type)
		{
			_msg.Info($"StartQAGPServerAsync is called");

			// TODO get envelope, selected data, selected QA spec, config,  ....
			var serviceConfig = _qaProjectItem.ServerConfigurations.FirstOrDefault(
				c => c.ServiceType == ProSuiteQAServiceType.GPLocal);
			if (serviceConfig == null)
			{
				_msg.Error($"Server config does not exist");
				return;
			}

			// temporary - give path to XML specifications
			var qaSpecificationsConnection =
				QAManager.GetQASpecificationsConnection(CurrentQASpecificationName);

			// TODO select only available workspaces 
			var qaParams =
				$"{qaSpecificationsConnection},{serviceConfig.DefaultTileSize},,,{serviceConfig.DefaultOutputFolder},,,,{serviceConfig.DefaultCompressValue}";

			var response =
				await QAManager.StartQATestingAsync(new ProSuiteQARequest(type, qaParams));
			if (response.Error == ProSuiteQAError.None)
			{
				_msg.Info($"StartQAGPServerAsync result {response?.ResponseData}");

				if (response?.ResponseData != null)
				{
					var issuesGdb = Path.Combine(response.ResponseData.ToString(), "issues.gdb");
					if (Directory.Exists(issuesGdb))
					{
						// TODO fire event to open worklist?
						await OpenIssuesWorklist(issuesGdb);
					}
				}
			}
			else
			{
				_msg.Error($"StartQAGPServerAsync is failed: ${response.Error}");
			}
		}

		public static async Task OpenIssuesWorklist([NotNull] string wlpath)
		{
			await ViewUtils.TryAsync(async () =>
			{
				throw new NotImplementedException();
				string workListName = WorkListsModule.Current.EnsureUniqueName();
				var environment = new DatabaseWorkEnvironment();

				await QueuedTask.Run(() => WorkListsModule.Current.CreateWorkListAsync(environment, workListName));

				WorkListsModule.Current.ShowView(workListName);
			}, _msg);
		}

		public async Task ShowSelectionWorkList()
		{
			await ViewUtils.TryAsync(async () =>
			{
				string workListName = WorkListsModule.Current.EnsureUniqueName();
				var environment = new InMemoryWorkEnvironment();

				await QueuedTask.Run(() => WorkListsModule.Current.CreateWorkListAsync(environment, workListName));

				WorkListsModule.Current.ShowView(workListName);
			}, _msg);
		}

		private async Task<bool> StartToolMicroserviceClientAsync()
		{
			const string exeName = "prosuite_microserver_geometry_processing.exe";

			_msg.IncrementIndentation("Searching for microservice deployment ({0})...", exeName);

			string executablePath = ConfigurationUtils.GetProSuiteExecutablePath(exeName);

			if (executablePath == null)
			{
				_msg.Warn(
					"Cannot find microservice deployment folder. Some edit Tools will be disabled.");

				return false;
			}

			GeometryProcessingClient result = new GeometryProcessingClient(
				new ClientChannelConfig()
				{
					// TODO: Get from configuration
					//HostName = "coronet.esri-de.com",
					HostName = "localhost",
					Port = 5153
				});

			ToolMicroserviceClient = result;

			return await result.AllowStartingLocalServerAsync(executablePath).ConfigureAwait(false);
		}
	}

	#region UI commands

	internal class StartQAGPTool : Button
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

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
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

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
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		StartQAErrorsDockPane()
		{
			//Enabled = false;
		}

		protected override async void OnClick()
		{
			try
			{
				// performance test
				//QueuedTask.Run(() =>
				//{
				//	ProSuiteLogPaneViewModel.GenerateMockMessages(10000);
				//});

				// temporary solution for WorkList
				//await QueuedTask.Run(() =>
				//{
				//	var pane = FrameworkApplication.DockPaneManager.Find("esri_dataReviewer_evaluateFeaturesPane");
				//	bool visible = pane.IsVisible;
				//	pane.Activate();
				//});

				await ProSuiteToolsModule.Current.ShowSelectionWorkList();
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message);
			}
		}
	}

	internal class ShowConfigWindow : Button
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);
		private ProSuiteConfigDialog _prosuiteconfigdialog = null;

		protected override void OnClick()
		{
			//already open?
			if (_prosuiteconfigdialog != null)
				return;

			// clone 
			var tempQAConfiguration = ProSuiteToolsModule.QAProjectItem.ServerConfigurations
			                                             .ToList().ConvertAll(
				                                             x => new ProSuiteQAServerConfiguration(
					                                             x));

			_prosuiteconfigdialog = new ProSuiteConfigDialog();
			_prosuiteconfigdialog.Owner = FrameworkApplication.Current.MainWindow;
			_prosuiteconfigdialog.DataContext = new ProSuiteConfigViewModel(tempQAConfiguration);
			_prosuiteconfigdialog.Closed += (o, e) => { _prosuiteconfigdialog = null; };

			if (_prosuiteconfigdialog.ShowDialog() ?? true)
			{
				ProSuiteConfigChangedEvent.Publish(
					new ProSuiteConfigEventArgs(tempQAConfiguration, null));
			}
		}
	}

	internal class ImportWorkListFile : Button
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		// TODO algr: temporary tests
		protected override void OnClick()
		{
			var bf = new BrowseProjectFilter();
			bf.AddCanBeTypeId(
				"ProSuiteItem_ProjectItem"); //TypeID for the ".wlist" custom project item

			// for subitem allow to browse inside and add as type
			//bf.AddDoBrowseIntoTypeId("ProSuiteItem_ProjectItemWorkListFile");
			//bf.AddCanBeTypeId("ProSuiteItem_WorkListItem"); //subitem 
			bf.Name = "Work List";

			var openItemDialog = new OpenItemDialog
			                     {
				                     Title = "Add Work List",
				                     //InitialLocation = "",
				                     BrowseFilter = bf
			                     };
			bool? result = openItemDialog.ShowDialog();
			if (result != null && (result.Value == false || ! openItemDialog.Items.Any())) return;

			var item = openItemDialog.Items.FirstOrDefault();
			string filePath = item?.Path;

			QueuedTask.Run(() =>
			{
				ProjectRepository.Current.AddProjectFileItems(
					ProjectItemType.WorkListDefinition,
					new List<string>() {filePath});
			});
		}
	}

	sealed class QASpecListComboBox : ComboBox
	{
		public QASpecListComboBox()
		{
			FillCombo();
			//Enabled = false;
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
			ProSuiteToolsModule.CurrentQASpecificationName = item.Text;
		}
	}

	#endregion UI commands
}
