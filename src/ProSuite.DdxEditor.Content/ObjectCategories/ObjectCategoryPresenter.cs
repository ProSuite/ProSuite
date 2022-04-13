using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class ObjectCategoryPresenter<T> :
		EntityItemPresenter<T, IViewObserver, ObjectCategory>
		where T : ObjectCategory
	{
		public ObjectCategoryPresenter(ObjectCategoryItem<T> item,
		                               IBoundView<T, IViewObserver> view)
			: base(item, view)
		{
			view.Observer = this;
		}
	}
}
