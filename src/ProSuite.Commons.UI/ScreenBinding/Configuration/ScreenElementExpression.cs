using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Drivers;
using ProSuite.Commons.UI.ScreenBinding.Stylers;

namespace ProSuite.Commons.UI.ScreenBinding.Configuration
{
	public abstract class ScreenElementExpression<EXPRESSIONTYPE>
	{
		[NotNull] private readonly IScreenElement _element;

		/// <summary>
		/// Initializes a new instance of the <see cref="ScreenElementExpression&lt;EXPRESSIONTYPE&gt;"/> class.
		/// </summary>
		/// <param name="element">The element.</param>
		protected ScreenElementExpression([NotNull] IScreenElement element)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			_element = element;
		}

		[NotNull]
		protected abstract EXPRESSIONTYPE ThisExpression();

		[NotNull]
		public EXPRESSIONTYPE WithAlias(string alias)
		{
			_element.Alias = alias;
			return ThisExpression();
		}

		[NotNull]
		public EXPRESSIONTYPE WithLabel([NotNull] Label label)
		{
			Styler.Default.ApplyStyle(label);

			_element.Label = ControlDriverFactory.GetDriver(label);
			return ThisExpression();
		}

		//public EXPRESSIONTYPE WithLabel(System.Windows.Controls.Label label)
		//{
		//    Styler.Default.ApplyStyle(label);

		//    _element.Label = ControlDriverFactory.GetDriver(label);
		//    return ThisExpression();
		//}

		[NotNull]
		public EXPRESSIONTYPE OnChange([NotNull] Action action)
		{
			((IBoundScreenElement) _element).RegisterChangeHandler(action);
			return ThisExpression();
		}

		[NotNull]
		public EXPRESSIONTYPE OnLostFocus([NotNull] Action action)
		{
			((IBoundScreenElement) _element).RegisterLostFocusHandler(action);
			return ThisExpression();
		}

		[NotNull]
		public EXPRESSIONTYPE RememberLastChoice()
		{
			((IBoundScreenElement) _element).RememberLastChoice();
			return ThisExpression();
		}

		[NotNull]
		public EXPRESSIONTYPE RebindOnChange()
		{
			((IBoundScreenElement) _element).RebindAllOnChange();

			return ThisExpression();
		}

		[NotNull]
		public EXPRESSIONTYPE AsReadOnly()
		{
			_element.ActivationMode = ActivationMode.ReadOnly;
			return ThisExpression();
		}

		[NotNull]
		public EXPRESSIONTYPE WatchWith([NotNull] IWatcher watcher)
		{
			watcher.Watch(_element);
			return ThisExpression();
		}

		[NotNull]
		public EXPRESSIONTYPE WatchWith<T>() where T : IWatcher, new()
		{
			return WatchWith(new T());
		}
	}
}
