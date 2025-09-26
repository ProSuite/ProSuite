using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Models
{
	public class ModelPresenter<T> : EntityItemPresenter<T, IViewObserver, DdxModel>
		where T : DdxModel
	{
		public ModelPresenter([NotNull] ModelItemBase<T> item,
		                      [NotNull] IBoundView<T, IViewObserver> view)
			: base(item, view)
		{
			view.Observer = this;
		}
	}
}
