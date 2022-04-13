namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class CategoryPresenter<T> : ICategoryObserver where T : ObjectCategory
	{
		private readonly ObjectCategoryItem<T> _item;

		public CategoryPresenter(ObjectCategoryItem<T> item, ICategoryView view)
		{
			_item = item;
			view.Observer = this;
		}

		#region ICategoryObserver Members

		public DdxModel GetModel()
		{
			return _item.Model;
		}

		#endregion
	}
}
