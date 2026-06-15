using System.Windows.Forms;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.UI.Core.DataModel.ResourceLookup;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public partial class AttributeControl<T> : UserControl, IEntityPanel<T>
		where T : Attribute
	{
		public AttributeControl()
		{
			InitializeComponent();
		}

		public string Title => "Attribute Properties";

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.Bind(e => e.Name)
			      .To(_textBoxName)
			      .AsReadOnly()
			      .WithLabel(_labelName);

			binder.Bind(e => e.Description)
			      .To(_textBoxDescription)
			      .WithLabel(_labelDescription);
		}

		public void OnBoundTo(T entity)
		{
			if (entity.Deleted)
			{
				_textBoxFieldType.Text = "<deleted>";
			}
			else
			{
				try
				{
					string typeName = Attribute.GetTypeName(entity.FieldType);

					_textBoxFieldType.Text =
						entity.FieldType == FieldType.Text
							? string.Format("{0} (Length: {1})", typeName, entity.FieldLength)
							: typeName;
				}
				catch (ModelElementAccessException)
				{
					_textBoxFieldType.Text = "<unknown field type>";
				}
			}

			_pictureBoxFieldType.Image = FieldTypeImageLookup.GetImage(entity);
		}
	}
}
