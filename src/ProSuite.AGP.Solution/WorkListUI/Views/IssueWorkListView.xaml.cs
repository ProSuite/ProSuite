using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI.Views
{
	public partial class IssueWorkListView
	{
		public IssueWorkListView([NotNull] IssueWorkListViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
		}
	}
}
