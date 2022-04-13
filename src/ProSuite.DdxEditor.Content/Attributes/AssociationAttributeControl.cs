using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.DdxEditor.Framework.ItemViews;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public partial class AssociationAttributeControl<T> : UserControl, IEntityPanel<T>
		where T : AssociationAttribute
	{
		public AssociationAttributeControl()
		{
			InitializeComponent();
		}

		public string Title => "Association Attribute";

		public void OnBindingTo(T entity) { }

		public void SetBinder(ScreenBinder<T> binder) { }

		public void OnBoundTo(T entity) { }
	}
}