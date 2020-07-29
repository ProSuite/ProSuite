using System;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Mapping.Events;
using System.Collections.Generic;
using ProSuite.Commons.QA.ServiceManager;
using ProSuite.Commons.QA.ServiceManager.Types;
using Clients.AGP.ProSuiteSolution.Layers;
using QAConfigurator;
using ProSuite.Commons.Logging;

namespace Clients.AGP.ProSuiteSolution
{
	internal class ProSuiteToolsModule : Module
	{
		// TODO temporary AGS server connection
		private readonly static string TEST_SERVER_PATTERN = "2424";
		private static ServerConnectionProjectItem _agsServerConnection
		{
			get
			{
				return Project.Current.GetItems<ServerConnectionProjectItem>().Where(conn => conn.Name.Contains(TEST_SERVER_PATTERN)).FirstOrDefault();
			}
		}

		private static ProSuiteQAManager _qaManager = null;
		public static ProSuiteQAManager QAManager
		{
			get
			{
				if( _qaManager == null )
				{
					_qaManager = QAConfiguration.QAManager;
					_qaManager.OnStatusChanged += QAManager_OnStatusChanged;
				}
				return _qaManager;
			}
		}

		// for info which could be stored in project
		private static ProSuiteQAProjectItem _qaProjectItem = null;
		public static ProSuiteQAProjectItem QAProjectItem
		{
			get
			{
				if (_qaProjectItem == null)
				{
					_qaProjectItem = Project.Current.GetItems<ProSuiteQAProjectItem>().FirstOrDefault();
					if (_qaProjectItem == null)
					{
						_qaProjectItem = GetDefaultProjectItem();
						// add qa info to project
						QueuedTask.Run(() =>
						{
							Project.Current.AddItem(_qaProjectItem);
						});
					}
				}
				return _qaProjectItem;
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

		// TODO UI for editing QA parameters?
		private static ProSuiteQAProjectItem GetDefaultProjectItem()
		{
			// TODO XML specifications files?
			return new ProSuiteQAProjectItem();
		}

		private static ProSuiteToolsModule _this = null;

		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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

		#region Overrides
		/// <summary>
		/// Initialize logic for the custom module
		/// </summary>
		/// <returns></returns>
		protected override bool Initialize()
		{
			InitLoggerConfiguration();

			//ProSuitePro.ProSuiteManager.QAManager.OnStatusChanged += QAManager_OnStatusChanged;

			//ProjectItemsChangedEvent.Subscribe(OnProjectItemsChanged);
			//LayersAddedEvent.Subscribe(OnLayerAdded);

			return base.Initialize();
		}
		private void InitLoggerConfiguration()
		{
			LoggingConfigurator.UsePrivateConfiguration = false;
			LoggingConfigurator.Configure(_loggingConfigFile);

			_msg.ReportMemoryConsumptionOnError = true;
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
			//LayersAddedEvent.Unsubscribe(OnLayerAdded);
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
	
			var agsItem = obj?.ProjectItem as ServerConnectionProjectItem;
			if (agsItem != null)
			{
				QAProjectItem.ServerConnection = _agsServerConnection?.Path;
			}
		}

		private void OnLayerAdded(LayerEventsArgs obj)
		{
			_msg.Info($"OnLayerAdded event");

			// TODO update available QA specifications - bind combo box to list?
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
				//ProSuiteToolsModule.StartQAGPServer(ProSuiteQAServiceType.GPLocal);
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

		protected override void OnClick()
		{
			try
			{
				//QueuedTask.Run(() =>
				//{
					var pane = FrameworkApplication.DockPaneManager.Find("esri_dataReviewer_evaluateFeaturesPane");
					bool visible = pane.IsVisible;
					pane.Activate();
				//});

				//MessageBox.Show("QA error handler view is not yet implemented");
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message);
			}

		}
	}

	internal class ShowLogWindow : Button
	{
		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected override void OnClick()
		{
			// TODO - is REST an alternative solution? 
			// https://vsdev2414.esri-de.com/server/rest/services/PROSUITE_QA/verification/GPServer/verifydataset/execute?object_class=%5C%5Cvsdev2414%5Cprosuite_server_trials%5Ctestdata.gdb%5Cpolygons&tile_size=10000&parameters=&verification_extent=&env%3AoutSR=&env%3AprocessSR=&returnZ=false&returnM=false&returnTrueCurves=false&returnFeatureCollection=false&context=&f=json

			//ProSuiteLogger.Logger.Log(LogType.Info, "Open configuration", "Click");
			// TODO test hyperlink ....
			_msg.Debug("Click");
		}
	}

	sealed class QASpecListComboBox : ArcGIS.Desktop.Framework.Contracts.ComboBox
	{
		public QASpecListComboBox()
		{
			FillCombo();
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
