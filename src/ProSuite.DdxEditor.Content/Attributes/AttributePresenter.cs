using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public class AttributePresenter<T> : EntityItemPresenter<T, IViewObserver, Attribute>
		where T : Attribute
	{
		public AttributePresenter(AttributeItem<T> item,
		                          IBoundView<T, IViewObserver> view)
			: base(item, view)
		{
			view.Observer = this;
		}
	}
}
