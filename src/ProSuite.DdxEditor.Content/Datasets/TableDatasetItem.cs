using ProSuite.Commons.DomainModels;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public class TableDatasetItem<T> : ObjectDatasetItem<T> where T : TableDataset
	{
		public TableDatasetItem(CoreDomainModelItemModelBuilder modelBuilder, T dataset,
		                        IRepository<Dataset> repository, DdxModel model)
			: base(modelBuilder, dataset, repository, model) { }
	}
}
