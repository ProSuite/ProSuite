using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Solution.WorkListUI.Views
{
	public partial class SelectionWorkListView
	{
		public SelectionWorkListView([NotNull] WorkListViewModelBase vm)
		{
			InitializeComponent();
			DataContext = vm;
		}
	}
}
