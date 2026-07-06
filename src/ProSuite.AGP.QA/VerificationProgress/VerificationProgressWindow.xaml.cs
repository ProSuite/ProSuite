using System.Windows;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.UI.Core.QA.VerificationProgress;

namespace ProSuite.AGP.QA.VerificationProgress
{
	/// <summary>
	/// Interaction logic for VerificationProgressWindow.xaml
	/// </summary>
	public partial class VerificationProgressWindow : Window
	{
		public static VerificationProgressWindow Create(
			VerificationProgressViewModel progressViewModel)
		{
			VerificationProgressWindow window = new VerificationProgressWindow();

			window.SetDataSource(progressViewModel);

			window.Owner = Application.Current.MainWindow;

			// Restore focus to the owner window when this window is closed.
			// This happens automatically for windows derived from ProWindow,
			// but not for WPF windows derived from Window.
			window.Closed += (_, _) => window.Owner?.Activate();

			return window;
		}

		public VerificationProgressWindow()
		{
			InitializeComponent();
		}

		private void SetDataSource([NotNull] VerificationProgressViewModel viewModel)
		{
			ProgressControl.SetDataSource(viewModel);
		}
	}
}
