using ProSuite.Commons.Essentials.Assertions;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	internal class VectorDatasetPresenter<E> : IVectorDatasetObserver<E>
		where E : VectorDataset
	{
		private readonly VectorDatasetItem<E> _item;

		public VectorDatasetPresenter(IVectorDatasetView<E> view,
		                              VectorDatasetItem<E> item)
		{
			Assert.ArgumentNotNull(view, nameof(view));
			Assert.ArgumentNotNull(item, nameof(item));

			_item = item;

			view.Observer = this;
		}

		#region IVectorDatasetObserver<E> Members

		public DdxModel GetModel()
		{
			return _item.DatasetModel;
		}

		#endregion
	}
}
