using ProSuite.Commons.DomainModels;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public class VectorDatasetItem<T> : ObjectDatasetItem<T> where T : VectorDataset
	{
		private readonly Model _datasetModel;

		public VectorDatasetItem(CoreDomainModelItemModelBuilder modelBuilder, T dataset,
		                         IRepository<Dataset> repository,
		                         Model model)
			: base(modelBuilder, dataset, repository, model)
		{
			_datasetModel = model;
		}

		public Model DatasetModel => _datasetModel;

		protected override void AddEntityPanels(
			ICompositeEntityControl<T, IViewObserver> compositeControl)
		{
			base.AddEntityPanels(compositeControl);

			var control = new VectorDatasetControl<T>();
			new VectorDatasetPresenter<T>(control, this);

			compositeControl.AddPanel(control);
		}
	}
}
