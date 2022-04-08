using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.DatasetCategories
{
	public class DatasetCategoryPresenter :
		SimpleEntityItemPresenter<DatasetCategoryItem>,
		IDatasetCategoryObserver
	{
		public DatasetCategoryPresenter(IDatasetCategoryView view, DatasetCategoryItem item)
			: base(item)
		{
			Assert.ArgumentNotNull(view, nameof(view));

			view.Observer = this;
		}
	}
}
