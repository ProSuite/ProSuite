using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfSandbox
{
	public class EnumBooleanConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value?.Equals(parameter);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value?.Equals(true) == true ? parameter : Binding.DoNothing;
		}

		#endregion
	}
}
