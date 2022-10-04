using System.Windows.Forms;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public interface IVectorDatasetView<T> : IWin32Window where T : VectorDataset
	{
		IVectorDatasetObserver<T> Observer { get; set; }
	}
}
