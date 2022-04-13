using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public class TopologyDatasetItem : DatasetItem<TopologyDataset>
	{
		public TopologyDatasetItem(CoreDomainModelItemModelBuilder modelBuilder,
		                           TopologyDataset dataset,
		                           IRepository<Dataset> repository)
			: base(modelBuilder, dataset, repository) { }

		protected override void AddEntityPanels(
			ICompositeEntityControl<TopologyDataset, IViewObserver> compositeControl)
		{
			// Dataset
			base.AddEntityPanels(compositeControl);

			// TopologyDataset
			IEntityPanel<TopologyDataset> control =
				new TopologyDatasetControl<TopologyDataset>();

			compositeControl.AddPanel(control);
		}
	}
}
