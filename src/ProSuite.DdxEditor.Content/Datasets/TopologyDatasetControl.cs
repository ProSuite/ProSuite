using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public partial class TopologyDatasetControl<T> : UserControl, IEntityPanel<T>
		where T : TopologyDataset
	{
		public TopologyDatasetControl()
		{
			InitializeComponent();

			_fileSystemPathControlDefaultSymbology.FileDefaultExtension = ".lyr";
			_fileSystemPathControlDefaultSymbology.FileFilter = "Layer Files (*.lyr) | *.lyr";
		}

		public string Title => "Topology Dataset Properties";

		public void OnBindingTo(T entity)
		{
			// TODO: where should this happen?
			if (entity.DefaultLayerFile == null)
			{
				entity.DefaultLayerFile =
					new LayerFile(null);
			}
		}

		public void SetBinder(ScreenBinder<T> binder)
		{
			ScreenBinder<LayerFile> layerFileBinder =
				binder.AddChildBinder<LayerFile>(m => m.DefaultLayerFile);

			layerFileBinder.Bind(m => m.FileName)
			               .To(_fileSystemPathControlDefaultSymbology.TextBox)
			               .WithLabel(_labelDefaultSymbology);
		}

		public void OnBoundTo(T entity) { }
	}
}
