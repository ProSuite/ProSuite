using System;
using ArcGIS.Desktop.Framework.Controls;
using ProSuite.Commons.UI.Persistence.WPF;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.Commons.AGP.Help;

/// <summary>
/// Interaction logic for AboutWindow.xaml
/// </summary>
public partial class AboutWindow : ProWindow, ICloseableWindow
{
	private readonly BasicFormStateManager _formStateManager;

	public AboutWindow()
	{
		InitializeComponent();

		_formStateManager = new BasicFormStateManager(this);
		_formStateManager.RestoreState();

		// Pro styles DataGrids with alternating (striped) rows.
		// This does not look good with our subheadings, so turn it off.
		// Cannot do it in XAML as it will be overridden. Second chance
		// is here, third chance would be in the OnActivated override.
		AboutItemDataGrid.AlternationCount = 1;
		AboutItemDataGrid.AlternatingRowBackground = AboutItemDataGrid.RowBackground;
	}

	protected override void OnClosed(EventArgs e)
	{
		// Occurs post factum (closed), but size and location are still
		// available: good! Could also wire the event (instead of overriding
		// this method): and make form state persistence a one-liner in the ctor!
		_formStateManager.SaveState();
		base.OnClosed(e); // still call base because it fires the event
	}

	public void CloseWindow(bool? dialogResult = null)
	{
		if (dialogResult.HasValue)
		{
			DialogResult = dialogResult;
		}

		Close();
	}
}
