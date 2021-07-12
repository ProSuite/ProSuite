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
using ProSuite.AGP.QA;
using ProSuite.AGP.Solution.ConfigUI;
using ProSuite.AGP.Solution.LoggerUI;
using ProSuite.AGP.Solution.ProjectItem;
using ProSuite.AGP.Solution.QA;
using ProSuite.AGP.Solution.Workflow;
using ProSuite.AGP.Solution.WorkLists;
using ProSuite.Application.Configuration;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.Microservices.Client.AGP;
using ProSuite.Microservices.Client.QA;
using ProSuite.QA.Configurator;
using ProSuite.QA.ServiceManager;
using ProSuite.QA.ServiceManager.Types;
using Module = ArcGIS.Desktop.Framework.Contracts.Module;

namespace ProSuite.AGP.Solution
{
	[UsedImplicitly]
	internal class ProSuiteToolsModule : Module
	{
		private const string _loggingConfigFile = "prosuite.logging.arcgispro.xml";

		private const string _microserverToolExeName =
			"prosuite_microserver_geometry_processing.exe";

		private const string _microserverQaExeName = "prosuite_microserver_qa.exe";

		const string _microserviceToolClientConfigXml =
			"prosuite.microservice.geometry_processing.client.config.xml";

		private const string _microserviceQaClientConfigXml =
			"prosuite.microservice.qa.client.config.xml";

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
					QAConfiguration.Current.GetQAServiceProviders(QAProjectItem?.ServerConfigurations),
					QAConfiguration.Current.GetQASpecificationsProvider(QAProjectItem?.SpecificationConfiguration));

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

		private static ProSuiteToolsModule _this = null;

		private static IMsg msg = null;
		private static MapBasedSessionContext _sessionContext;

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

		public GeometryProcessingClient ToolMicroserviceClient { get; private set; }

		public QualityVerificationServiceClient QaMicroserviceClient { get; private set; }

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

		public IMapBasedSessionContext SessionContext =>
			_sessionContext ?? (_sessionContext = new MapBasedSessionContext());

		private static void UpdateServiceUI(ProSuiteProjectItemConfiguration projectItem = null)
		{
			if (projectItem == null)
			{
				FrameworkApplication.State.Activate(ConfigIDs.QA_GPService_State);
				FrameworkApplication.State.Activate(ConfigIDs.QA_GPLocal_State);
			}
			else
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

			// TODO: Task.Run async?
			SetupBackend().ConfigureAwait(false);

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

		internal static async Task StartQAGPServerAsync(ProSuiteQAServiceType type,
		                                                string specificationName)
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
				QAManager.GetQASpecificationsConnection(specificationName);

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

				await QueuedTask.Run(
					() => WorkListsModule.Current.CreateWorkListAsync(environment, workListName));

				WorkListsModule.Current.ShowView(workListName);
			}, _msg);
		}

		public async Task ShowSelectionWorkList()
		{
			await ViewUtils.TryAsync(async () =>
			{
				string workListName = WorkListsModule.Current.EnsureUniqueName();
				var environment = new InMemoryWorkEnvironment();

				await QueuedTask.Run(
					() => WorkListsModule.Current.CreateWorkListAsync(environment, workListName));

				WorkListsModule.Current.ShowView(workListName);
			}, _msg);
		}

		private async Task<bool> SetupBackend()
		{
			bool result = await StartToolMicroserviceClientAsync();

			QualityVerificationServiceClient client = await StartQaMicroserviceClientAsync();

			// Make sure the field is initialized:
			Assert.NotNull(SessionContext);

			_sessionContext.MicroServiceClient = client;

			// TODO: If no client channel config file exists, use XML verification provider directly:
			//verificationEnvironment =
			//	new QualityVerificationEnvironment(new XmlSpecificationProvider());
			var verificationEnvironment =
				new QualityVerificationEnvironment(SessionContext, client);

			verificationEnvironment.VerificationService =
				new VerificationServiceGrpc(client)
				{
					HtmlReportName = Constants.HtmlReportName,
					VerificationReportName = Constants.VerificationReportName
				};

			_sessionContext.VerificationEnvironment = verificationEnvironment;
			_sessionContext.VerificationEnvironment.RefreshQualitySpecifications();
		
			// TODO: This has no effect any more -> change XML based specification provider

			// this is still necessary for GP QA if actual
			QAConfiguration.Current.SetupGrpcConfiguration(verificationEnvironment);
			// enable GP Buttons 
			UpdateServiceUI(); 

			// ... to implement IQualitySpecificationReferencesProvider instead, such as
			//verificationEnvironment.FallbackSpecificationProvider = new XmlSpecificationProvider();
			// in case no Microservice is available. Additionally, implement a second VerificationService subclass.
			return result;
		}

		private async Task<bool> StartToolMicroserviceClientAsync()
		{
			try
			{
				string executablePath;
				using (_msg.IncrementIndentation(
					"Searching for tool microservice deployment ({0})...", _microserverToolExeName))
				{
					executablePath =
						ConfigurationUtils.GetProSuiteExecutablePath(_microserverToolExeName);

					if (executablePath == null)
					{
						_msg.Warn("Cannot find tool microservice deployment folder. " +
						          "Some edit Tools might be disabled.");
					}
				}

				string configFilePath =
					ConfigurationUtils.GetConfigFilePath(_microserviceToolClientConfigXml, false);

				GeometryProcessingClient result =
					await GrpcClientConfigUtils.StartGeometryProcessingClient(
						executablePath, configFilePath);

				ToolMicroserviceClient = Assert.NotNull(result);
			}
			catch (Exception e)
			{
				_msg.Warn($"Error starting microservice client: {e.Message}", e);
				return false;
			}

			return true;
		}

		private async Task<QualityVerificationServiceClient> StartQaMicroserviceClientAsync()
		{
			string executablePath;
			using (_msg.IncrementIndentation("Searching for QA microservice deployment ({0})...",
			                                 _microserverQaExeName))
			{
				executablePath =
					ConfigurationUtils.GetProSuiteExecutablePath(_microserverQaExeName);

				if (executablePath == null)
				{
					_msg.Debug("Cannot find qa microservice deployment folder.");
				}
			}

			string configFilePath =
				ConfigurationUtils.GetConfigFilePath(_microserviceQaClientConfigXml, false);

			QualityVerificationServiceClient result =
				await GrpcClientConfigUtils.StartQaServiceClient(executablePath, configFilePath);

			QaMicroserviceClient = Assert.NotNull(result);

			return QaMicroserviceClient;
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
				string specificationName =
					ProSuiteToolsModule.Current.SessionContext.VerificationEnvironment
					                   ?.CurrentQualitySpecification?.Name;

				if (specificationName != null)
				{
					await ProSuiteToolsModule.StartQAGPServerAsync(
						ProSuiteQAServiceType.GPService, specificationName);
				}
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
				string specificationName = ProSuiteToolsModule.Current.SessionContext
				                                              .VerificationEnvironment
				                                              ?.CurrentQualitySpecification?.Name;

				if (specificationName != null)
				{
					await ProSuiteToolsModule.StartQAGPServerAsync(
						ProSuiteQAServiceType.GPLocal, specificationName);
				}
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

	#endregion UI commands
}
