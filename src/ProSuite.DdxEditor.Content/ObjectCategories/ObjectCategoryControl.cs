using System.Globalization;
using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public partial class ObjectCategoryControl<E> : UserControl, IEntityPanel<E>,
	                                                ICategoryView
		where E : ObjectCategory
	{
		private ICategoryObserver _observer;

		public ObjectCategoryControl()
		{
			InitializeComponent();
		}

		public string Title => "Object Category Properties";

		public ICategoryObserver Observer
		{
			get { return _observer; }
			set { _observer = value; }
		}

		public void OnBindingTo(E entity)
		{
			if (entity.ObjectDataset is IVectorDataset vectorDataset)
			{
				_textBoxDatasetMinimumSegmentLength.Text =
					vectorDataset.MinimumSegmentLength.ToString(CultureInfo.CurrentCulture);
			}

			_textBoxName.ReadOnly = ! entity.CanChangeName;
		}

		public void SetBinder(ScreenBinder<E> binder)
		{
			binder.Bind(m => m.Name)
			      .To(_textBoxName)
			      .WithLabel(_labelName);

			// read-only or not is determined OnBinding to entity

			binder.Bind(m => m.Description)
			      .To(_textBoxDescription)
			      .WithLabel(_labelDescription);

			binder.Bind(m => m.AllowOrphanDeletion)
			      .To(_comboBoxAllowOrphanDeletion);

			binder.Bind(m => m.SubtypeCode)
			      .To(_textBoxSubtypeCode)
			      .AsReadOnly()
			      .WithLabel(_labelSubtypeCode);

			binder.AddElement(new NumericUpDownNullableElement(
				                  binder.GetAccessor(m => m.MinimumSegmentLengthOverride),
				                  _numericUpDownNullableMinimumSegmentLength));
		}

		public void OnBoundTo(E entity) { }
	}
}
