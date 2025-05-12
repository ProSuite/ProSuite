using System.Globalization;
using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public partial class VectorDatasetControl<T> : UserControl, IVectorDatasetView<T>,
	                                               IEntityPanel<T>
		where T : VectorDataset
	{
		private IVectorDatasetObserver<T> _observer;

		public VectorDatasetControl()
		{
			InitializeComponent();

			_fileSystemPathControlDefaultSymbology.FileDefaultExtension = ".lyr";
			_fileSystemPathControlDefaultSymbology.FileFilter =
				"Layer Files (*.lyr) | *.lyr";
		}

		#region IEntityPanel<T> Members

		public string Title => "Vector Dataset Properties";

		#region IVectorDatasetView<T> Members

		public IVectorDatasetObserver<T> Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		#endregion

		public void OnBindingTo(T entity)
		{
			if (entity.DefaultLayerFile == null)
			{
				entity.DefaultLayerFile = new LayerFile(null);
			}

			DdxModel model = _observer.GetModel();

			_textBoxModelMinimumSegmentLength.Text =
				model.DefaultMinimumSegmentLength.ToString(CultureInfo.CurrentCulture);
		}

		public void SetBinder(ScreenBinder<T> binder)
		{
			ScreenBinder<LayerFile> layerFileBinder =
				binder.AddChildBinder<LayerFile>(m => m.DefaultLayerFile);

			layerFileBinder.Bind(m => m.FileName)
			               .To(_fileSystemPathControlDefaultSymbology.TextBox)
			               .WithLabel(_labelDefaultSymbology);

			binder.AddElement(new NumericUpDownNullableElement(
				                  binder.GetAccessor(m => m.MinimumSegmentLengthOverride),
				                  _numericUpDownNullableMinimumSegmentLength));
		}

		public void OnBoundTo(T entity) { }

		#endregion
	}
}
