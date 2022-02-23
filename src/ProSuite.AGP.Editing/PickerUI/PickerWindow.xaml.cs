using System;
using System.ComponentModel;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.PickerUI
{
	/// <summary>
	/// Interaction logic for PickerWindow.xaml
	/// </summary>
	public partial class PickerWindow
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private bool IsClosing { get; set; }

		public PickerWindow(PickerViewModel vm)
		{
			InitializeComponent();

			if (vm is PickerViewModel pickerViewModel)
			{
				DataContext = pickerViewModel;
			}
		}

		private void PickerWindow_Deactivated(object sender, EventArgs e)
		{
			try
			{
				if (! IsClosing)
				{
					Close();
				}
			}
			catch (Exception exception)
			{
				_msg.Error("Error deactivating picker window", exception);
			}
		}

		private void PickerWindow_Closing(object sender, CancelEventArgs e)
		{
			IsClosing = true;
		}
	}
}
