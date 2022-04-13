using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public class TableDatasetItem<T> : ObjectDatasetItem<T> where T : TableDataset
	{
		public TableDatasetItem(CoreDomainModelItemModelBuilder modelBuilder, T dataset,
		                        IRepository<Dataset> repository, Model model)
			: base(modelBuilder, dataset, repository, model) { }
	}
}
