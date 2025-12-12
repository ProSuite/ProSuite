using System.Windows;
using System.Windows.Forms;
using Microsoft.Xaml.Behaviors;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.UI.WPF;
using TriggerBase = Microsoft.Xaml.Behaviors.TriggerBase;

namespace ProSuite.UI.Core.QA.VerificationProgress
{
	/// <summary>
	/// Interaction logic for VerificationProgressWpfControl.xaml
	/// </summary>
	public partial class VerificationProgressWpfControl : IWinFormHostable
	{
		private Form _hostWinForm;

		public VerificationProgressWpfControl()
		{
			// BUG: Could not load file or assembly 'Microsoft.Xaml.Behaviors, ...'
			//      when not loaded directly.
			// https://github.com/microsoft/XamlBehaviorsWpf/issues/86
			// https://github.com/zspitz/ANTLR4ParseTreeVisualizer/issues/35

			var _ = new DefaultTriggerAttribute(
				typeof(Trigger), typeof(TriggerBase), null);

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

			// This is too early for the host win form (must be wired separately in the setter):
			WireClosingEvent(vm);

			// In the designer:
			if (vm == null) return;

			await Assert.NotNull(vm).RunBackgroundVerificationAsync();
		}

		private void WireClosingEvent(VerificationProgressViewModel vm)
		{
			if (_hostWinForm != null)
			{
				_hostWinForm.Closing += vm.Closing;
			}
			else
			{
				var wpfWindow = Window.GetWindow(this);

				if (wpfWindow != null)
				{
					wpfWindow.Closing += vm.Closing;
				}
			}
		}

		public Form HostFormsWindow
		{
			set
			{
				_hostWinForm = value;

				VerificationProgressViewModel vm = (VerificationProgressViewModel) DataContext;

				if (_hostWinForm != null && vm != null)
				{
					_hostWinForm.Closing += vm.Closing;
				}
			}
		}
	}
}
