using ProSuite.DomainModel.AO.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public interface IVectorDatasetObserver<E> where E : VectorDataset
	{
		Model GetModel();
	}
}
