using System;
using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;

namespace ProSuite.DdxEditor.Content.QA.Categories
{
	public partial class DataQualityCategoryControl : UserControl,
	                                                  IDataQualityCategoryView
	{
		private ScreenBinder<DataQualityCategory> _binder;

		public DataQualityCategoryControl()
		{
			InitializeComponent();
		}

		public void OnBindingTo(DataQualityCategory entity) { }

		public void SetBinder(ScreenBinder<DataQualityCategory> binder)
		{
			_binder = binder;

			_binder.Bind(c => c.Name)
			       .To(_textBoxName)
			       .WithLabel(_labelName);
			_binder.Bind(c => c.Abbreviation)
			       .To(_textBoxAbbreviation)
			       .WithLabel(_labelAbbreviation);
			_binder.Bind(c => c.Uuid)
			       .To(_textBoxUuid)
			       .AsReadOnly()
			       .WithLabel(_labelUuid);
			_binder.Bind(c => c.Description)
			       .To(_textBoxDescription)
			       .WithLabel(_labelDescription);
			_binder.Bind(c => c.ListOrder)
			       .To(_numericUpDownListOrder)
			       .WithLabel(_labelListOrder);

			_binder.Bind(c => c.CanContainQualityConditions)
			       .To(_checkBoxCanContainQualityConditions);
			_binder.Bind(c => c.CanContainQualitySpecifications)
			       .To(_checkBoxCanContainQualitySpecifications);
			_binder.Bind(c => c.CanContainSubCategories)
			       .To(_checkBoxCanContainSubCategories);

			_binder.AddElement(new ObjectReferenceScreenElement(
				                   _binder.GetAccessor(c => c.DefaultModel),
				                   _objectReferenceControlDefaultDataModel));
		}

		public void OnBoundTo(DataQualityCategory entity)
		{
			_textBoxParentCategory.Text = entity.ParentCategory?.GetQualifiedName();
		}

		public IDataQualityCategoryObserver Observer { get; set; }

		public Func<object> FindDefaultModelDelegate
		{
			get { return _objectReferenceControlDefaultDataModel.FindObjectDelegate; }
			set { _objectReferenceControlDefaultDataModel.FindObjectDelegate = value; }
		}
	}
}
