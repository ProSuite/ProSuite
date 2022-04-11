using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Framework.Items
{
	public class WrappedEntityItemPresenter<E, BASE> :
		EntityItemPresenter<E, IViewObserver, BASE> where E : BASE
		                                            where BASE : Entity
	{
		[NotNull] private readonly IEntityControlWrapper<E> _view;

		public WrappedEntityItemPresenter([NotNull] EntityItem<E, BASE> item,
		                                  [NotNull] IEntityControlWrapper<E> view)
			: base(item, view)
		{
			_view = view;
			view.Observer = this;
		}

		protected override void OnBoundTo(E entity)
		{
			_view.OnBoundTo(entity);
		}
	}
}
