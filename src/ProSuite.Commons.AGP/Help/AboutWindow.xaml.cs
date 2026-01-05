using System;
using System.Windows;
using ProSuite.Commons.UI.Persistence.WPF;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.Commons.AGP.Help;

/// <summary>
/// Interaction logic for AboutWindow.xaml
/// </summary>
public partial class AboutWindow : Window, ICloseableWindow
{
	private readonly BasicFormStateManager _formStateManager;

	public AboutWindow()
	{
		InitializeComponent();

		_formStateManager = new BasicFormStateManager(this);
		_formStateManager.RestoreState();
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
