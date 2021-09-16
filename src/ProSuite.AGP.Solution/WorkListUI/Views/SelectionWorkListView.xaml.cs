using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI.Views
{
	public partial class SelectionWorkListView
	{
		public SelectionWorkListView([NotNull] SelectionWorkListViewModel vm)
		{
			InitializeComponent();
			DataContext = vm;
		}
	}
}
