using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public class ModelPresenter<T> : EntityItemPresenter<T, IViewObserver, Model>
		where T : Model
	{
		public ModelPresenter(ModelItemBase<T> item, IBoundView<T, IViewObserver> view)
			: base(item, view)
		{
			view.Observer = this;
		}
	}
}
