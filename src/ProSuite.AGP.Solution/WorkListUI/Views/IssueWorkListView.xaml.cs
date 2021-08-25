using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI.Views
{
	public partial class IssueWorkListView
	{
		public IssueWorkListView([NotNull] IssueWorkListVm vm)
		{
			InitializeComponent();
			DataContext = vm;
		}
	}
}
