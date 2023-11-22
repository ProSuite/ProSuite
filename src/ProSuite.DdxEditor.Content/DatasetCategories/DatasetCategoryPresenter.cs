using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.DatasetCategories
{
	public class DatasetCategoryPresenter :
		SimpleEntityItemPresenter<DatasetCategoryItem>,
		IDatasetCategoryObserver
	{
		public DatasetCategoryPresenter([NotNull] DatasetCategoryItem item,
		                                [NotNull] IDatasetCategoryView view)
			: base(item)
		{
			Assert.ArgumentNotNull(view, nameof(view));

			view.Observer = this;
		}
	}
}
