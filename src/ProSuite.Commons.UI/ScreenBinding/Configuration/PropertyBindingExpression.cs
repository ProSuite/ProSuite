using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Elements;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.Commons.UI.ScreenBinding.Configuration
{
	public class PropertyBindingExpression : IBindingData
	{
		private readonly IPropertyAccessor _accessor;
		private readonly IScreenBinder _binder;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyBindingExpression"/> class.
		/// </summary>
		/// <param name="binder">The binder.</param>
		/// <param name="accessor">The accessor.</param>
		public PropertyBindingExpression([NotNull] IScreenBinder binder,
		                                 [NotNull] IPropertyAccessor accessor)
		{
			Assert.ArgumentNotNull(binder, nameof(binder));
			Assert.ArgumentNotNull(accessor, nameof(accessor));

			_binder = binder;
			_accessor = accessor;
		}

		#region IBindingData Members

		IScreenBinder IBindingData.Binder => _binder;

		IPropertyAccessor IBindingData.Accessor => _accessor;

		#endregion

		[NotNull]
		public PropertyBindingExpression ToVisibilityOf(params Control[] controls)
		{
			foreach (Control control in controls)
			{
				IScreenElement element = _binder.FindElementForControl(control) ??
				                         new ScreenElement<Control>(control);

				element.BindVisibilityTo(_accessor);
				_binder.AddElement(element);
			}

			return this;
		}

		[NotNull]
		public PropertyBindingExpression ToEnabledOf(params Control[] controls)
		{
			foreach (Control control in controls)
			{
				IScreenElement element = _binder.FindElementForControl(control) ??
				                         new ScreenElement<Control>(control);

				element.BindEnabledTo(_accessor);
				_binder.AddElement(element);
			}

			return this;
		}

		[NotNull]
		public TextEditingElementExpression To([NotNull] TextBox textbox)
		{
			var element = new TextboxElement(_accessor, textbox);
			_binder.AddElement(element);

			return new TextEditingElementExpression(element);
		}

		[NotNull]
		public TextEditingElementExpression To([NotNull] Label label)
		{
			var element = new LabelElement(_accessor, label);
			_binder.AddElement(element);

			return new TextEditingElementExpression(element);
		}

		[NotNull]
		public ListElementExpression To([NotNull] ComboBox comboBox)
		{
			var element = new PicklistElement(_accessor, comboBox);
			_binder.AddElement(element);

			return new ListElementExpression(element);
		}

		[NotNull]
		public CheckboxElementExpression To([NotNull] CheckBox box)
		{
			IScreenElement element = new CheckboxElement(_accessor, box);
			_binder.AddElement(element);

			return new CheckboxElementExpression(element);
		}

		[NotNull]
		public NumericUpDownElementExpression To([NotNull] NumericUpDown numericUpDown)
		{
			var element = new NumericUpDownElement(_accessor, numericUpDown);
			_binder.AddElement(element);

			return new NumericUpDownElementExpression(element);
		}

		[NotNull]
		public RadioGroupExpression<ENUM> ToRadioButtons<ENUM>()
		{
			return new RadioGroupExpression<ENUM>(_accessor, _binder);
		}

		[NotNull]
		public BooleanComboboxElementExpression To([NotNull] BooleanCombobox booleanCombobox)
		{
			var element = new BooleanComboboxElement(_accessor, booleanCombobox);
			_binder.AddElement(element);

			return new BooleanComboboxElementExpression(element);
		}
	}
}
