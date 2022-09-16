using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public partial class ObjectDatasetControl<T> : UserControl, IEntityPanel<T>
		where T : ObjectDataset
	{
		public ObjectDatasetControl()
		{
			InitializeComponent();
		}

		#region IEntityPanel<T> Members

		public string Title => "Object Dataset Properties";

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.Bind(m => m.DisplayFormat)
			      .To(_textBoxDisplayFormat)
			      .WithLabel(_labelDisplayFormat);
		}

		public void OnBoundTo(T entity) { }

		#endregion
	}
}
