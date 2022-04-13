using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.AttributeTypes
{
	public partial class AttributeTypeControl<T> : UserControl, IEntityPanel<T>
		where T : AttributeType
	{
		public AttributeTypeControl()
		{
			InitializeComponent();
		}

		public string Title => "Attribute Type Properties";

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.Bind(m => m.Name)
			      .To(_textBoxName)
			      .AsReadOnly()
			      .WithLabel(_labelName);

			binder.Bind(m => m.Description)
			      .To(_textBoxDescription)
			      .WithLabel(_labelDescription);
		}

		public void OnBoundTo(T entity) { }
	}
}
