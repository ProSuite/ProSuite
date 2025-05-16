using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public interface IVectorDatasetObserver<E> where E : VectorDataset
	{
		DdxModel GetModel();
	}
}
