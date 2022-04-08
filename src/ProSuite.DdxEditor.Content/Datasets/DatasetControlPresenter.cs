using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public class DatasetControlPresenter
	{
		public delegate DatasetCategory FindDatasetCategory(
			IWin32Window owner, params ColumnDescriptor[] columns);

		public DatasetControlPresenter([NotNull] IDatasetView view,
		                               [NotNull] FindDatasetCategory findDatasetCategory)
		{
			view.FindDatasetCategoryDelegate =
				() => findDatasetCategory(view, new ColumnDescriptor("Name"));
		}
	}
}
