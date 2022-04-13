using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;

namespace ProSuite.DdxEditor.Content.DatasetCategories
{
	public partial class DatasetCategoryControl : UserControl, IDatasetCategoryView
	{
		private IDatasetCategoryObserver _observer;

		public DatasetCategoryControl()
		{
			InitializeComponent();
		}

		#region IDatasetCategoryView Members

		public IDatasetCategoryObserver Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		#endregion

		#region IWrappedEntityControl<DatasetCategory> Members

		public void OnBindingTo(DatasetCategory entity) { }

		public void SetBinder(ScreenBinder<DatasetCategory> binder)
		{
			binder.Bind(m => m.Name)
			      .To(_textBoxName)
			      .WithLabel(_labelName);

			binder.Bind(m => m.Description)
			      .To(_textBoxDescription)
			      .WithLabel(_labelDescription);

			binder.Bind(m => m.Abbreviation)
			      .To(_textBoxAbbreviation)
			      .WithLabel(_labelAbbreviation);
		}

		public void OnBoundTo(DatasetCategory entity) { }

		#endregion
	}
}
