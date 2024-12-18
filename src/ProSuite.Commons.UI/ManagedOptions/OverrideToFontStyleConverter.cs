using System;
using System.Globalization;
using System.Windows.Data;

namespace ProSuite.Commons.UI.ManagedOptions;

public class OverrideToFontStyleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
		{
			return "Normal";
		}

		return (bool) value ? "Italic" : "Normal";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return true;
		throw new NotImplementedException("Cannot convert back from font style to bool");
	}
}
