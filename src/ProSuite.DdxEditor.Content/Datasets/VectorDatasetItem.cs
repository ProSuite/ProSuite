using ProSuite.Commons.DomainModels;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public class VectorDatasetItem<T> : ObjectDatasetItem<T> where T : VectorDataset
	{
		private readonly DdxModel _datasetModel;

		public VectorDatasetItem(CoreDomainModelItemModelBuilder modelBuilder, T dataset,
		                         IRepository<Dataset> repository,
		                         DdxModel model)
			: base(modelBuilder, dataset, repository, model)
		{
			_datasetModel = model;
		}

		public DdxModel DatasetModel => _datasetModel;

		protected override void AddEntityPanels(
			ICompositeEntityControl<T, IViewObserver> compositeControl,
			IItemNavigation itemNavigation)
		{
			base.AddEntityPanels(compositeControl, itemNavigation);

			var control = new VectorDatasetControl<T>();
			new VectorDatasetPresenter<T>(control, this);

			compositeControl.AddPanel(control);
		}
	}
}
