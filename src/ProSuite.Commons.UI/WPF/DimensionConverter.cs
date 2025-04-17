using System;
using System.Globalization;
using System.Windows.Data;

namespace ProSuite.Commons.UI.WPF;

/// <summary>
/// Convert between a textual dimension like "1.5 mm" or "-3pt"
/// and the <see cref="Dimension"/> type. Formatting is controlled
/// by the <see cref="FormatSpecifier"/> property and the parameter
/// passed to <see cref="Convert"/> (the latter takes precedence).
/// For the supported syntax see <see cref="Dimension.Parse"/>.
/// </summary>
/// <example>
/// Usage example in XAML:
/// <code>
/// &lt;TextBox PreviewKeyDown="PreviewKeyDownHandler"&gt;
///   &lt;TextBox.Style&gt;
///     &lt;Style TargetType="{x:Type TextBox}"&gt;
///       &lt;Style.Triggers&gt;
///         &lt;Trigger Property="Validation.HasError" Value="True"&gt;
///           &lt;Setter Property="ToolTip" Value="{Binding RelativeSource={RelativeSource Self}, Path=(Validation.Errors)[0].ErrorContent}"/&gt;
///         &lt;/Trigger&gt;
///       &lt;/Style.Triggers&gt;
///     &lt;/Style&gt;
///   &lt;/TextBox.Style&gt;
///   &lt;TextBox.Text&gt;
///     &lt;Binding Path="MarginAlongDimension" UpdateSourceTrigger="Default"&gt;
///       &lt;Binding.Converter&gt;
///         &lt;local:DimensionConverter FormatSpecifier="F3" /&gt; &lt;!--or use ConverterParameter--&gt;
///       &lt;/Binding.Converter&gt;
///       &lt;Binding.ValidationRules&gt;
///         &lt;local:DimensionValidation ValidUnitsText="mm,pt,mu" UnitRequired="False" /&gt;
///       &lt;/Binding.ValidationRules&gt;
///     &lt;/Binding&gt;
///   &lt;/TextBox.Text&gt;
///   &lt;TextBox.InputBindings&gt;
///     &lt;KeyBinding Key="Up" Command="{Binding IncrementMarginAlongCommand}" /&gt;
///     &lt;KeyBinding Key="Down" Command="{Binding DecrementMarginAlongCommand}" /&gt;
///   &lt;/TextBox.InputBindings&gt;
/// &lt;/TextBox&gt;
/// </code>
/// Here the up and down arrow keys call methods on the view model to
/// increment or decrement the value; the PreviewKeyDown handler should
/// call <c>UpdateSource()</c> on the binding expression for up and down
/// keys to ensure increment/decrement work on the latest value, like this:
/// <code>
/// private void PreviewKeyDownHandler(object sender, KeyEventArgs args)
/// {
///     if (sender is TextBox textBox &amp;&amp; args.Key is Key.Enter or Key.Up or Key.Down)
///     {
///         textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
///     }
///     args.Handled = false; // pass the event on!
/// }
/// </code>
/// </example>
[ValueConversion(typeof(Dimension), typeof(string))]
public class DimensionConverter : IValueConverter
{
	/// <summary>
	/// Controls formatting of the value part of the dimension.
	/// Use any of .NET's standard numeric format strings, as described in
	/// <see href="https://learn.microsoft.com/dotnet/standard/base-types/standard-numeric-format-strings"/>
	/// </summary>
	public string FormatSpecifier { get; set; }

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		// TODO consider DependencyProperty.UnsetValue if value is null
		if (targetType != typeof(string))
			throw new InvalidOperationException($"Cannot convert to {targetType.Name}");
		if (value is null) return string.Empty;
		if (value is string s) return s;
		if (value is Dimension dim)
		{
			var format = GetFormat(parameter);
			var num = dim.Value.ToString(format, culture);
			return dim.Unit is null || ! double.IsFinite(dim.Value)
				       ? num
				       : string.Concat(num, ' ', dim.Unit);
		}
		throw new InvalidOperationException($"Cannot convert {value.GetType().Name} value");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (targetType != typeof(Dimension))
			throw new InvalidOperationException($"Cannot convert to {targetType.Name}");
		return Dimension.Parse(value as string, culture);
	}

	private string GetFormat(object parameter)
	{
		if (parameter is string s) return s;
		if (parameter is int i && 0 <= i && i < 15)
			return $"F{i}";
		if (! string.IsNullOrEmpty(FormatSpecifier))
			return FormatSpecifier;
		return "R";
	}
}
