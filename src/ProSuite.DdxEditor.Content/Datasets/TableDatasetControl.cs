using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.Datasets
{
	internal partial class TableDatasetControl<T> : UserControl, IEntityPanel<T>
		where T : TableDataset
	{
		private readonly string _title = "Table Dataset Properties";

		public TableDatasetControl()
		{
			InitializeComponent();
		}

		#region IEntityPanel<T> Members

		public string Title => _title;

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder) { }

		public void OnBoundTo(T entity) { }

		#endregion
	}
}
