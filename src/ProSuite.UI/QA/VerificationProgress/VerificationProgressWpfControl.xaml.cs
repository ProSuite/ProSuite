using System.Windows;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.UI.WPF;
using UserControl = System.Windows.Controls.UserControl;

namespace ProSuite.UI.QA.VerificationProgress
{
	/// <summary>
	/// Interaction logic for VerificationProgressWpfControl.xaml
	/// </summary>
	public partial class VerificationProgressWpfControl : UserControl, IWinFormHostable
	{
		private Form _hostWinForm;

		public VerificationProgressWpfControl()
		{
			InitializeComponent();
		}

		public void SetDataSource(VerificationProgressViewModel viewModel)
		{
			if (viewModel.CloseAction == null)
			{
				viewModel.CloseAction = () =>
				                        {
					                        if (_hostWinForm != null)
					                        {
						                        _hostWinForm.Close();
					                        }
					                        else
					                        {
						                        var wpfWindow = Window.GetWindow(this);

						                        Assert.NotNull(wpfWindow).Close();
					                        }
				                        };
			}

			DataContext = viewModel;
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			VerificationProgressViewModel vm = (VerificationProgressViewModel) DataContext;

			await Assert.NotNull(vm).RunBackgroundVerificationAsync();
		}

		public Form HostFormsWindow
		{
			set { _hostWinForm = value; }
		}
	}
}
