using System;
using System.Windows;
using Microsoft.Win32;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.AGP.Display;

/// <summary>
/// Interaction logic for ExportOverridesDialog.xaml
/// </summary>
public partial class ExportOverridesDialog : Window
{
	private readonly ExportOverridesOptions _options;

	public ExportOverridesDialog(ExportOverridesOptions options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));
		DataContext = options;

		InitializeComponent();

		Loaded += HandleWindowLoaded;
	}

	private void HandleWindowLoaded(object sender, RoutedEventArgs e)
	{
		// freeze window height (it was SizeToContent)
		var ht = ActualHeight;
		MinHeight = MaxHeight = ht;

		// Cannot do this in constructor (would have no effect), so do it here:
		this.ShowMinimizeButton(false);
		this.ShowMaximizeButton(false);
	}

	private void BrowseButtonClicked(object sender, RoutedEventArgs e)
	{
		var dialog = new SaveFileDialog();

		dialog.DefaultExt = ".xml";
		dialog.Filter = "XML|*.xml|All files|*.*";
		dialog.Title = "Export Overrides Configuration";

		bool? result = dialog.ShowDialog(this);
		if (result == true)
		{
			_options.ConfigFilePath = dialog.FileName;
		}
	}

	private void ExportButtonClicked(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}
}
