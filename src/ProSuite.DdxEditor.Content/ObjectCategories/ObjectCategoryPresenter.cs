using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public class ObjectCategoryPresenter<T> :
		EntityItemPresenter<T, IViewObserver, ObjectCategory>
		where T : ObjectCategory
	{
		public ObjectCategoryPresenter([NotNull] ObjectCategoryItem<T> item,
		                               [NotNull] IBoundView<T, IViewObserver> view)
			: base(item, view)
		{
			view.Observer = this;
		}
	}
}
