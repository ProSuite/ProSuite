using System.Windows.Forms;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.DatasetCategories
{
	public interface IDatasetCategoryView : IWrappedEntityControl<DatasetCategory>,
	                                        IWin32Window
	{
		IDatasetCategoryObserver Observer { get; set; }
	}
}