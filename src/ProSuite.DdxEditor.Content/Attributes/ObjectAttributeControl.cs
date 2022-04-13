using System;
using System.Windows.Forms;
using ProSuite.Commons.UI.ScreenBinding;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.DdxEditor.Framework.ItemViews;

namespace ProSuite.DdxEditor.Content.Attributes
{
	public partial class ObjectAttributeControl<T> : UserControl, IObjectAttributeView,
	                                                 IEntityPanel<T>
		where T : ObjectAttribute
	{
		// todo: move to more general place
		private const string _noText = "No";
		private const string _yesText = "Yes";

		public ObjectAttributeControl()
		{
			InitializeComponent();
		}

		public Func<object> FindAttributeTypeDelegate
		{
			get { return _objectReferenceControlAttributeType.FindObjectDelegate; }
			set { _objectReferenceControlAttributeType.FindObjectDelegate = value; }
		}

		#region IEntityPanel<T> Members

		public string Title => "Object Attribute Properties";

		public void OnBindingTo(T entity)
		{
			if (entity.Role != null)
			{
				_textBoxRole.Text = entity.Role.ToString();
			}

			if (entity.ObjectAttributeType != null)
			{
				_textBoxObjectAttTypeReadOnly.Text =
					GetBooleanAsText(entity.ObjectAttributeType.ReadOnly);

				_textBoxObjectAttTypeRequireEqualValues.Text =
					GetBooleanAsText(entity.ObjectAttributeType.IsObjectDefining);
			}
		}

		public void SetBinder(ScreenBinder<T> binder)
		{
			binder.AddElement(new NullableBooleanComboboxElement(
				                  binder.GetAccessor(m => m.ReadOnlyOverride),
				                  _nullableBooleanComboboxReadOnly));

			binder.AddElement(new NullableBooleanComboboxElement(
				                  binder.GetAccessor(m => m.IsObjectDefiningOverride),
				                  _nullableBooleanComboboxIsObjectDefiningOverride));

			binder.AddElement(new ObjectReferenceScreenElement(
				                  binder.GetAccessor(m => m.ObjectAttributeType),
				                  _objectReferenceControlAttributeType));
		}

		public void OnBoundTo(T entity) { }

		#endregion

		#region IObjectAttributeView Members

		public Func<object> FindObjectAttributeTypeDelegate
		{
			get { return _objectReferenceControlAttributeType.FindObjectDelegate; }
			set { _objectReferenceControlAttributeType.FindObjectDelegate = value; }
		}

		public void OnBindingTo(ObjectAttributeType entity) { }

		public void SetBinder(ScreenBinder<ObjectAttributeType> binder) { }

		public void OnBoundTo(ObjectAttributeType entity) { }

		#endregion

		private static string GetBooleanAsText(bool value)
		{
			return value
				       ? _yesText
				       : _noText;
		}
	}
}
