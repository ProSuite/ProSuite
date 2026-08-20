using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Logging;

namespace ProSuite.Commons.AGP.LoggerUI;

/// <summary>
/// WPF host for the (WinForms) <see cref="LogWindowControl"/> so it can be used as the
/// content of an ArcGIS Pro dock pane. This is the WinForms-based alternative to the native
/// WPF <see cref="LogDockPane"/> / <see cref="LogDockPaneViewModelBase"/>.
/// </summary>
public partial class WinFormsLogDockPane
{
	public WinFormsLogDockPane()
	{
		InitializeComponent();
	}

	[NotNull]
	public LogWindowControl LogWindow => _logWindowControl;
}
