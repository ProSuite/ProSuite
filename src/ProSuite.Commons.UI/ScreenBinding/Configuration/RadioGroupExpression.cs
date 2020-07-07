using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Elements;

namespace ProSuite.Commons.UI.ScreenBinding.Configuration
{
	public class RadioGroupExpression<ENUM>
	{
		private readonly IScreenBinder _binder;
		private readonly RadioButtonGroup<ENUM> _group = new RadioButtonGroup<ENUM>();
		private readonly IPropertyAccessor _accessor;
		private RadioElement<ENUM> _lastElement;

		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroupExpression&lt;ENUM&gt;"/> class.
		/// </summary>
		/// <param name="accessor">The accessor.</param>
		/// <param name="binder">The binder.</param>
		public RadioGroupExpression([NotNull] IPropertyAccessor accessor,
		                            [NotNull] IScreenBinder binder)
		{
			Assert.ArgumentNotNull(accessor, nameof(accessor));
			Assert.ArgumentNotNull(binder, nameof(binder));

			_accessor = accessor;
			_binder = binder;
		}

		[NotNull]
		public RadioGroupExpression<ENUM> RadioButton([NotNull] RadioButton button)
		{
			var element = new RadioElement<ENUM>(_accessor, button, _group);
			_binder.AddElement(element);

			_lastElement = element;

			return this;
		}

		[NotNull]
		public RadioGroupExpression<ENUM> IsBoundTo(ENUM enumValue)
		{
			_lastElement.BoundValue = enumValue;
			return this;
		}

		[NotNull]
		public RadioGroupExpression<ENUM> AliasAs(string alias)
		{
			_lastElement.Alias = alias;
			return this;
		}

		[NotNull]
		public RadioGroupExpression<ENUM> OnClick(Action handler)
		{
			_lastElement.RegisterOnClickHandler(handler);
			return this;
		}

		[NotNull]
		public RadioGroupExpression<ENUM> RebindAllOnChange()
		{
			_lastElement.RebindAllOnChange();
			return this;
		}
	}
}
