using System;
using System.Windows;
using Microsoft.Win32;

namespace ProSuite.AGP.Display;

/// <summary>
/// Interaction logic for ExportSLDLMDialog.xaml
/// </summary>
public partial class ExportSLDLMDialog : Window
{
	private readonly ExportSLDLMOptions _options;

	public ExportSLDLMDialog(ExportSLDLMOptions options)
	{
		_options = options ?? throw new ArgumentNullException(nameof(options));
		DataContext = options;

		InitializeComponent();
	}

	private void BrowseButtonClicked(object sender, RoutedEventArgs e)
	{
		var dialog = new SaveFileDialog();

		dialog.DefaultExt = ".xml";
		dialog.Filter = "XML|*.xml|CSV|*.csv|All files|*.*";
		dialog.Title = "Export SLD/LM Configuration";

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
