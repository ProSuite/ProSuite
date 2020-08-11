using System;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.MainSolution
{
	internal class WorkListTrialsModule : Module
	{
		private static WorkListTrialsModule _singleton;

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
			const string workListName = "Test Items";

			var workList = WorkListRegistry.Instance.Get(workListName);
			if (workList == null)
			{
				workList = TestWorkList.Create(workListName);
				WorkListRegistry.Instance.Add(workList);
			}

			return workList;
		}

		public void RedrawMap()
		{
			// TODO Should redraw only if ActiveMap has WorkList lyrs and only as little as possible
			const bool clearCache = true;
			MapView.Active?.Redraw(clearCache);
		}

		#region Overrides

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
