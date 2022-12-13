using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework;
using ProSuite.AGP.Solution.ConfigUI;
using ProSuite.AGP.Solution.LoggerUI;
using ProSuite.AGP.Solution.ProjectItem;
using ProSuite.AGP.Solution.Workflow;
using ProSuite.Application.Configuration;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.Microservices.Client.AGP;
using ProSuite.Microservices.Client.QA;
using ProSuite.QA.Configurator;
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

		private const string _microserviceToolClientConfigXml =
			"prosuite.microservice.geometry_processing.client.config.xml";

		private const string _microserviceQaClientConfigXml =
			"prosuite.microservice.qa.client.config.xml";

		private static ProSuiteProjectItemConfiguration _qaProjectItem;

		private static ProSuiteToolsModule _this;

		private static IMsg msg;
		private static MapBasedSessionContext _sessionContext;

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

		private static IMsg _msg
		{
			get
			{
				if (msg == null)
				{
					msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);
				}

				return msg;
			}
		}

		public GeometryProcessingClient ToolMicroserviceClient { get; private set; }

		public QualityVerificationServiceClient QaMicroserviceClient { get; private set; }

		/// <summary>
		/// Retrieve the singleton instance to this module here
		/// </summary>
		public static ProSuiteToolsModule Current =>
			_this ?? (_this = (ProSuiteToolsModule) FrameworkApplication.FindModule(
				          "ProSuiteSolution_Module"));

		public IMapBasedSessionContext SessionContext =>
			_sessionContext ?? (_sessionContext = new MapBasedSessionContext());

		public static event EventHandler<ProSuiteQAConfigEventArgs> OnQAConfigurationChanged;

		private static void UpdateServiceUI(ProSuiteProjectItemConfiguration projectItem = null)
		{
			if (projectItem == null)
			{
				FrameworkApplication.State.Activate(ConfigIDs.QA_GPService_State);
				FrameworkApplication.State.Activate(ConfigIDs.QA_GPLocal_State);
			}
			else
			{
				ProSuiteQAServerConfiguration localService =
					projectItem.ServerConfigurations.FirstOrDefault(
						s => s.ServiceType == ProSuiteQAServiceType.GPLocal && s.IsValid);
				if (localService != null)
				{
					FrameworkApplication.State.Activate(ConfigIDs.QA_GPLocal_State);
				}
				else
				{
					FrameworkApplication.State.Deactivate(ConfigIDs.QA_GPLocal_State);
				}

				ProSuiteQAServerConfiguration serverService =
					projectItem.ServerConfigurations.FirstOrDefault(
						s => s.ServiceType == ProSuiteQAServiceType.GPService && s.IsValid);
				if (serverService != null)
				{
					FrameworkApplication.State.Activate(ConfigIDs.QA_GPService_State);
				}
				else
				{
					FrameworkApplication.State.Deactivate(ConfigIDs.QA_GPService_State);
				}
			}
		}

		private async Task<bool> SetupBackend()
		{
			var toolsClientStarted = false;

			// NOTE: This method is used with ConfigureAwait(false) and must not throw or the application will crash!
			try
			{
				toolsClientStarted = await StartToolMicroserviceClientAsync();

				// Make sure the field is initialized:
				Assert.NotNull(SessionContext);

				_sessionContext.MicroServiceClient = await StartQaMicroserviceClientAsync();

				IQualityVerificationEnvironment verificationEnvironment =
					await _sessionContext.InitializeVerificationEnvironment();

				// this is still necessary for GP QA (Consider implementing a second VerificationService subclass for GP QA):
				QAConfiguration.Current.SetupGrpcConfiguration(verificationEnvironment);
				// enable GP Buttons 
				UpdateServiceUI();
			}
			catch (Exception e)
			{
				_msg.Error("Error setting up backend", e);
			}

			return toolsClientStarted;
		}

		private async Task<bool> StartToolMicroserviceClientAsync()
		{
			try
			{
				string executablePath;
				using (_msg.IncrementIndentation(
					       "Searching for tool microservice deployment ({0})...",
					       _microserverToolExeName))
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
				_msg.Warn($"Error starting microservice client for edit tools: {e.Message}", e);
				return false;
			}

			return true;
		}

		private async Task<QualityVerificationServiceClient> StartQaMicroserviceClientAsync()
		{
			try
			{
				string executablePath;
				using (_msg.IncrementIndentation(
					       "Searching for QA microservice deployment ({0})...",
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
					await GrpcClientConfigUtils.StartQaServiceClient(
						executablePath, configFilePath);

				QaMicroserviceClient = Assert.NotNull(result);

				return QaMicroserviceClient;
			}
			catch (Exception e)
			{
				_msg.Warn(
					$"Error starting microservice client for quality verification: {e.Message}", e);
				return null;
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

			ProSuiteConfigChangedEvent.Subscribe(OnConfigurationChanged);
			LogMessageActionEvent.Subscribe(OnLogMessageActionRequested);
			ProjectOpenedAsyncEvent.Subscribe(OnProjectOpenedAsync);

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

			ProSuiteConfigChangedEvent.Unsubscribe(OnConfigurationChanged);
			LogMessageActionEvent.Unsubscribe(OnLogMessageActionRequested);
			ProjectOpenedAsyncEvent.Unsubscribe(OnProjectOpenedAsync);

			ToolMicroserviceClient?.Disconnect();
			QaMicroserviceClient?.Disconnect();
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
			{
				_msg.Debug("Unkown LogMessage action");
			}
		}

		private async Task OnProjectOpenedAsync(ProjectEventArgs arg)
		{
			await SetupBackend();
		}

		#endregion
	}
}
