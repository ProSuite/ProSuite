using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public class DatasetPresenter<T> : EntityItemPresenter<T, IViewObserver, Dataset>
		where T : Dataset
	{
		public DatasetPresenter(DatasetItem<T> item, IBoundView<T, IViewObserver> view)
			: base(item, view)
		{
			view.Observer = this;
		}
	}
}