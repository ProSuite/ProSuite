using System;
using System.Globalization;
using System.Windows.Data;

namespace ProSuite.Commons.UI.WPF
{
	/// <summary>
	/// Convert between a textual scale like "1:2500" and a double
	/// representing the denominator of such a scale (e.g. 2500).
	/// Syntax: "" (empty) or "1:N" or "1/N" where N is a valid
	/// double according to current regional settings.
	/// </summary>
	/// <remarks>
	/// Exceptions in <see cref="Convert"/> and <see cref="ConvertBack"/>
	/// may crash the application! Use validation to ensure that only
	/// valid strings are passed through <see cref="ConvertBack"/> using
	/// XAML as shown in a comment within the code.
	/// </remarks>
	/// <seealso href="https://stackoverflow.com/questions/6123880"/>
	/// <seealso href="https://learn.microsoft.com/dotnet/desktop/wpf/data/#data-validation"/>
	[ValueConversion(typeof(double), typeof(string))]
	public class ScaleConverter : IValueConverter
	{
		// <TextBox>
		//   <TextBox.Text>
		//     <Binding Path="ScaleDenomDoubleProperty"
		//              UpdateSourceTrigger="PropertyChanged"> <!--or Default-->
		//       <Binding.ValidationRules>
		//         <namespace:ScaleValidation/>
		//       </Binding.ValidationRules>
		//       <Binding.Converter>
		//         <namespace:ScaleConverter/>
		//       </Binding.Converter>
		//     </Binding>
		//   </TextBox.Text>
		// </TextBox>

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null)
			{
				return string.Empty;
			}

			double denom = System.Convert.ToDouble(value);
			if (denom <= 0 || ! IsFinite(denom))
			{
				return string.Empty;
			}

			return denom < 1
				       ? string.Format(culture, $"{1/denom}:1")
				       : string.Format(culture, $"1:{denom}");
		}

		private static bool IsFinite(double value)
		{
			// IsFinite does not compile in .net framework
			return !double.IsNaN(value) && !double.IsInfinity(value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Parse(value as string, culture);
		}

		public static double Parse(string text, CultureInfo culture)
		{
			const double none = 0.0;

			text = text.Trim();
			if (string.IsNullOrEmpty(text)) return none;

			int index = text.IndexOfAny(_fractionChars);

			if (index < 0)
			{
				return double.Parse(text.Trim(), culture);
			}

			string numerText = text.Substring(0, index).Trim();
			double numer = string.IsNullOrEmpty(numerText)
				               ? 1.0
				               : double.Parse(numerText, culture);

			string denomText = text.Substring(index + 1).Trim();
			double denom = string.IsNullOrEmpty(denomText)
				               ? 1.0
				               : double.Parse(denomText, culture);

			denom /= numer;

			return double.IsNaN(denom) ? none : denom;
		}

		private static readonly char[] _fractionChars = ":/".ToCharArray();
	}
}
