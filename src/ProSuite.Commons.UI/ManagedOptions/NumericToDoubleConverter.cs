using System;
using System.Globalization;
using System.Windows.Data;

namespace ProSuite.Commons.UI.ManagedOptions
{
	/// <summary>
	/// Converts a numeric setting value (e.g. <see cref="int"/> or <see cref="double"/>) to a
	/// <see cref="double"/> for the slider's <c>Value</c> dependency property, and back to the
	/// binding source's type. WPF passes the binding source property type as
	/// <paramref name="targetType"/> on <see cref="ConvertBack"/>, so a single converter serves
	/// both int- and double-valued centralizable settings.
	/// </summary>
	public class NumericToDoubleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
			{
				return 0d;
			}

			return System.Convert.ToDouble(value, culture);
		}

		public object ConvertBack(object value, Type targetType, object parameter,
		                          CultureInfo culture)
		{
			if (value == null)
			{
				return null;
			}

			Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

			return System.Convert.ChangeType(value, underlyingType, culture);
		}
	}
}
