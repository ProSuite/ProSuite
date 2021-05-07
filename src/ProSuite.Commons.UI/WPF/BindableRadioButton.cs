using System.Windows;
using System.Windows.Controls;

namespace ProSuite.Commons.UI.WPF
{
	/// <summary>
	/// A WPF radio button with somewhat usable binding behaviour;-(
	/// https://stackoverflow.com/a/15923466/15749950
	/// </summary>
	public class BindableRadioButton : RadioButton
	{
		public object RadioValue
		{
			get => GetValue(RadioValueProperty);
			set => SetValue(RadioValueProperty, value);
		}

		// Using a DependencyProperty as the backing store for RadioValue.
		// This enables animation, styling, binding, etc...

		public static readonly DependencyProperty RadioValueProperty =
			DependencyProperty.Register(
				"RadioValue",
				typeof(object),
				typeof(BindableRadioButton),
				new UIPropertyMetadata(null));

		public object RadioBinding
		{
			get => GetValue(RadioBindingProperty);
			set => SetValue(RadioBindingProperty, value);
		}

		// Using a DependencyProperty as the backing store for RadioBinding.
		// This enables animation, styling, binding, etc...

		public static readonly DependencyProperty RadioBindingProperty =
			DependencyProperty.Register(
				"RadioBinding",
				typeof(object),
				typeof(BindableRadioButton),
				new FrameworkPropertyMetadata(
					null,
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					OnRadioBindingChanged));

		private static void OnRadioBindingChanged(
			DependencyObject d,
			DependencyPropertyChangedEventArgs e)
		{
			BindableRadioButton rb = (BindableRadioButton) d;
			if (rb.RadioValue.Equals(e.NewValue?.ToString()))
			{
				rb.SetCurrentValue(IsCheckedProperty, true);
			}
		}

		protected override void OnChecked(RoutedEventArgs e)
		{
			base.OnChecked(e);
			SetCurrentValue(RadioBindingProperty, RadioValue);
		}
	}
}
