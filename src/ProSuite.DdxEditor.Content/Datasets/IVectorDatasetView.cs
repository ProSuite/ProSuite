using System.Windows.Forms;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public interface IVectorDatasetView<T> : IWin32Window where T : VectorDataset
	{
		IVectorDatasetObserver<T> Observer { get; set; }
	}
}
