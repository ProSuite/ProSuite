using System;
using System.Reflection;
using System.Threading.Tasks;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using JetBrains.Annotations;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Logging;
using Module = ArcGIS.Desktop.Framework.Contracts.Module;

namespace Clients.AGP.ProSuiteSolution.WorkListTrials
{
	[UsedImplicitly]
	internal class WorkListTrialsModule : Module
	{
		private static WorkListTrialsModule _singleton;
		private WorkListCentral _central;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Retrieve the singleton instance to this module here
		/// </summary>
		public static WorkListTrialsModule Current
		{
			get
			{
				return _singleton ?? (_singleton = (WorkListTrialsModule)FrameworkApplication.FindModule("ProSuiteSolution_WorkListTrialsModule"));
			}
		}

		public PluginDatasourceConnectionPath GetWorkListConnectionPath(string workListName)
		{
			const string pluginIdentifier = "ProSuite_WorkListDatasource";

			var baseUri = new Uri("worklist://localhost/");
			var datasourcePath = new Uri(baseUri, workListName);

			return new PluginDatasourceConnectionPath(pluginIdentifier, datasourcePath);
		}

		public IWorkList GetTestWorkList()
		{
			var name = TestWorkList.Name;

			var workList = Central.Get(name);
			if (workList == null)
			{
				workList = TestWorkList.Create(name);
				Central.Set(workList);
			}

			return workList;
		}

		public void Refresh()
		{
			// TODO Should redraw only if ActiveMap has WorkList lyrs and only as little as possible
			const bool clearCache = true;
			MapView.Active?.Redraw(clearCache);
		}

		public WorkListCentral Central
		{
			get { return _central ?? (_central = new WorkListCentral()); }
		}

		#region Overrides

		protected override bool Initialize()
		{
			_msg.Debug("Initialize()");

			// TODO Testing: always have the TestWorkList:
			Central.Set(TestWorkList.Create());

			return true; // initialization successful
		}

		protected override void Uninitialize()
		{
			_msg.Debug("Uninitialize()");
		}

		protected override Task OnReadSettingsAsync(ModuleSettingsReader settings)
		{
			_msg.Debug("OnReadSettingsAsync()");
			return base.OnReadSettingsAsync(settings);
		}

		protected override Task OnWriteSettingsAsync(ModuleSettingsWriter settings)
		{
			_msg.Debug("OnWriteSettingsAsync()");
			return base.OnWriteSettingsAsync(settings);
		}

		/// <summary>
		/// Called by Framework when ArcGIS Pro is closing
		/// </summary>
		/// <returns>False to prevent Pro from closing, otherwise True</returns>
		protected override bool CanUnload()
		{
			return true;
		}

		#endregion
	}
}
