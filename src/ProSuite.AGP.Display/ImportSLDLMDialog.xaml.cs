using System;
using System.Windows;
using Microsoft.Win32;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.AGP.Display;

/// <summary>
/// Interaction logic for ImportSLDLMDialog.xaml
/// </summary>
public partial class ImportSLDLMDialog : Window
{
	private readonly ImportSLDLMOptions _options;

	public ImportSLDLMDialog(ImportSLDLMOptions options)
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
		var dialog = new OpenFileDialog();

		dialog.DefaultExt = ".xml";
		dialog.Filter = "XML|*.xml|CSV|*.csv|All files|*.*";
		dialog.Title = "Import SLD/LM Configuration";

		bool? result = dialog.ShowDialog(this);
		if (result == true)
		{
			_options.ConfigFilePath = dialog.FileName;
		}
	}

	private void ImportButtonClicked(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}
}
