using ProSuite.UI.QA.VerificationProgress;

namespace ProSuite.AGP.Solution.QA
{
	/// <summary>
	/// Interaction logic for VerificationProgressWindow.xaml
	/// </summary>
	public partial class VerificationProgressWindow
	{
		public VerificationProgressWindow(VerificationProgressViewModel viewModel)
		{
			InitializeComponent();

			ProgressControl.SetDataSource(viewModel);
		}
	}
}
