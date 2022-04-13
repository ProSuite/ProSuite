using System.Windows.Forms;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.DatasetCategories
{
	public interface IDatasetCategoryView : IWrappedEntityControl<DatasetCategory>,
	                                        IWin32Window
	{
		IDatasetCategoryObserver Observer { get; set; }
	}
}
