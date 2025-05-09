using System;
using System.Windows;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Persistence.WPF;

namespace ProSuite.Commons.AGP.Carto;

/// <summary>
/// Interaction logic for SymbolDisplaySettingsWindow.xaml
/// </summary>
public partial class SymbolDisplaySettingsWindow
{
	private readonly BasicFormStateManager _formStateManager;
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public SymbolDisplaySettingsWindow()
	{
		InitializeComponent();

		_formStateManager = new BasicFormStateManager(this);
		_formStateManager.RestoreState();
	}

	protected override void OnClosed(EventArgs args)
	{
		// Occurs post factum (closed), but size and location are still
		// available. Could also wire the event (instead of overriding
		// this method) and make form state persistence a ctor one-liner.

		_formStateManager.SaveState();

		base.OnClosed(args); // still call base because it fires the event
	}

	private void OkButtonClicked(object sender, RoutedEventArgs args)
	{
		try
		{
			DialogResult = true;
		}
		catch (Exception ex)
		{
			_msg.Error($"OK clicked: {ex.Message}", ex);
		}
	}
}
