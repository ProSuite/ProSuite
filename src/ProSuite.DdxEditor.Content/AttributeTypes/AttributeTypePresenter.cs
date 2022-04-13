using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.AttributeTypes
{
	public class AttributeTypePresenter<T> :
		EntityItemPresenter<T, IViewObserver, AttributeType>
		where T : AttributeType
	{
		public AttributeTypePresenter([NotNull] AttributeTypeItem<T> item,
		                              [NotNull] IBoundView<T, IViewObserver> view)
			: base(item, view)
		{
			view.Observer = this;
		}
	}
}
