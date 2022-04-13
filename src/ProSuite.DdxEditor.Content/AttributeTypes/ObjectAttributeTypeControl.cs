using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.AttributeTypes
{
	public partial class ObjectAttributeTypeControl<T> : UserControl, IEntityPanel<T>
		where T : ObjectAttributeType
	{
		public ObjectAttributeTypeControl()
		{
			InitializeComponent();
		}

		public string Title => "Object Attribute Type Properties";

		public void OnBindingTo(T entity)
		{
			if (entity.AttributeRole != null)
			{
				_textBoxRole.Text = entity.AttributeRole.ToString();
			}
		}

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.Bind(m => m.ReadOnly)
			      .To(_booleanComboboxReadOnly);

			binder.Bind(m => m.IsObjectDefining)
			      .To(_booleanComboboxIsObjectDefining);
		}

		public void OnBoundTo(T entity) { }
	}
}
