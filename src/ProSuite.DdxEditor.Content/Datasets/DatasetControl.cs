using System;
using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.Datasets
{
	public partial class DatasetControl<T> : UserControl, IDatasetView, IEntityPanel<T>
		where T : Dataset
	{
		private const string _title = "Dataset Properties";
		private IDatasetObserver _observer;

		public DatasetControl()
		{
			InitializeComponent();
		}

		public Func<object> FindDatasetCategoryDelegate
		{
			get { return _objectReferenceControlDatasetCategory.FindObjectDelegate; }
			set { _objectReferenceControlDatasetCategory.FindObjectDelegate = value; }
		}

		#region IEntityPanel<T> Members

		public string Title => _title;

		public void OnBindingTo(T entity)
		{
			if (entity.GeometryType != null)
			{
				_textBoxGeometryType.Text = entity.GeometryType.Name;
			}
		}

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.Bind(m => m.Name)
			      .To(_textBoxName)
			      .AsReadOnly()
			      .WithLabel(_labelName);

			binder.Bind(m => m.Description)
			      .To(_textBoxDescription)
			      .WithLabel(_labelDescriprion);

			binder.Bind(m => m.AliasName)
			      .To(_textBoxAliasName)
			      .WithLabel(_labelAliasName);

			binder.Bind(m => m.Abbreviation)
			      .To(_textBoxAbbreviation)
			      .WithLabel(_labelAbbreviation);

			binder.AddElement(new ObjectReferenceScreenElement(
				                  binder.GetAccessor(m => m.DatasetCategory),
				                  _objectReferenceControlDatasetCategory));
		}

		public void OnBoundTo(T entity) { }

		#endregion

		#region IBoundView<Dataset,IDatasetObserver> Members

		public IDatasetObserver Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		public void BindTo(Dataset target) { }

		#endregion
	}
}
