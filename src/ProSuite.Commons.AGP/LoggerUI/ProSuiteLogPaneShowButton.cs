using System;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.AGP.WPF;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.LoggerUI
{
	/// <summary>
	/// Button implementation to show the DockPane.
	/// </summary>
	[UsedImplicitly]
	public class ProSuiteLogPaneShowButton : Button
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected override void OnClick()
		{
			Exception e = ProSuiteLogPaneViewModel.LoggingConfigurationException;

			if (e != null)
			{
				ErrorHandler.HandleError(
					$"The logging configuration could not be initialized: {e.Message}",
					e, _msg, "Error");

				return;
			}

			ProSuiteLogPaneViewModel.ToggleDockWindowVisibility();
		}
	}
}
