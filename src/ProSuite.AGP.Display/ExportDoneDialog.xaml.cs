using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WPF;

namespace ProSuite.AGP.Display;

/// <summary>
/// Interaction logic for ExportDoneDialog.xaml
/// </summary>
public partial class ExportDoneDialog : Window, INotifyPropertyChanged
{
	private string _heading;
	private string _filePath;
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public ExportDoneDialog()
	{
		InitializeComponent();

		DataContext = this; // quick'n'dirty

		Loaded += HandleWindowLoaded;
	}

	private void HandleWindowLoaded(object sender, RoutedEventArgs e)
	{
		// freeze window height (it was SizeToContent)
		var ht = ActualHeight;
		MinHeight = Math.Max(MinHeight, ht);
		MaxHeight = Math.Min(MaxHeight, ht);

		// Cannot do this in constructor (would have no effect), so do it here:
		this.ShowMinimizeButton(false);
		this.ShowMaximizeButton(false);
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
			Gateway.ShowError(ex, _msg);
		}
	}

	private void OpenFolderClicked(object sender, RoutedEventArgs e)
	{
		try
		{
			DialogResult = true; // closes the window
			ShowInExplorer(FilePath);
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	private void CopyPathClicked(object sender, RoutedEventArgs e)
	{
		try
		{
			DialogResult = true; // closes the window
			if (string.IsNullOrEmpty(FilePath)) return;
			Clipboard.SetText(FilePath);
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
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
