using System.Windows;

namespace ProSuite.AGP.Editing.PickerUI
{
	/// <summary>
	/// Interaction logic for PickerWindow.xaml
	/// </summary>
	public partial class PickerWindow : Window
	{
		public PickerWindow(PickerViewModel vm)
		{
			InitializeComponent();
			
			if (vm is PickerViewModel pickerViewModel)
			{
				DataContext = pickerViewModel;
			}
		}
	}
}
