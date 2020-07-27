using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.WorkList;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.MainSolution
{
	internal class Module1 : Module
	{
		private static Module1 _singleton;

		/// <summary>
		/// Retrieve the singleton instance to this module here
		/// </summary>
		public static Module1 Current
		{
			get
			{
				return _singleton ?? (_singleton = (Module1)FrameworkApplication.FindModule("ProSuite.AGP.MainSolution_Module"));
			}
		}

		public WorkList.Contracts.WorkList GetTestWorklist()
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
