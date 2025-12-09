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
