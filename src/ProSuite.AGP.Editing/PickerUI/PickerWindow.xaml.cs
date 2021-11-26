using System;
using System.Windows.Input;

namespace ProSuite.AGP.Editing.PickerUI
{
	/// <summary>
	/// Interaction logic for PickerWindow.xaml
	/// </summary>
	public partial class PickerWindow
	{
		public PickerWindow(PickerViewModel vm)
		{
			InitializeComponent();

			if (vm is PickerViewModel pickerViewModel)
			{
				DataContext = pickerViewModel;
			}
		}

		private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			Close();
		}

		private void Window_Deactivated(object sender, EventArgs e)
		{
			try
			{
				Close();
			}
			catch (Exception)
			{
				// ignored
			}
		}
	}
}
