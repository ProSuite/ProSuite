using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using ProSuite.Commons.AGP.WPF;

namespace ProSuite.AGP.Display;

/// <summary>
/// Interaction logic for ExportDoneDialog.xaml
/// </summary>
public partial class ExportDoneDialog : Window, INotifyPropertyChanged
{
	private string _heading;
	private string _filePath;

	public ExportDoneDialog()
	{
		InitializeComponent();

		DataContext = this; // quick'n'dirty
	}

	public string FilePath
	{
		get => _filePath;
		set
		{
			if (! string.Equals(_filePath, value))
			{
				_filePath = value;
				OnPropertyChanged();
			}
		}
	}

	public string Heading
	{
		get => _heading;
		set
		{
			if (! string.Equals(_heading, value))
			{
				_heading = value;
				OnPropertyChanged();
			}
		}
	}

	private void FilePathClicked(object sender, RoutedEventArgs e)
	{
		try
		{
			OpenWithDefaultApplication(FilePath);
		}
		catch (Exception ex)
		{
			ErrorHandler.HandleError($"Error opening {FilePath}", ex);
		}
	}

	private void OpenFolderClicked(object sender, RoutedEventArgs e)
	{
		try
		{
			ShowInExplorer(FilePath);
		}
		catch (Exception ex)
		{
			ErrorHandler.HandleError($"Error opening {FilePath}", ex);
		}
	}

	private void CopyPathClicked(object sender, RoutedEventArgs e)
	{
		try
		{
			if (string.IsNullOrEmpty(FilePath)) return;
			Clipboard.SetText(FilePath);
		}
		catch (Exception ex)
		{
			ErrorHandler.HandleError($"Error copying text to clipboard: {ex.Message}", ex);
		}
	}

	private void OkButtonClicked(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}

	private static void OpenWithDefaultApplication(string filePath)
	{
		using var process = new Process();

		process.StartInfo.FileName = "explorer.exe";
		process.StartInfo.Arguments = string.Concat('"', filePath, '"'); // TODO escape filePath!

		process.Start();
	}

	private static void ShowInExplorer(string filePath)
	{
		Process.Start("explorer.exe", $"/select,{filePath}");
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
