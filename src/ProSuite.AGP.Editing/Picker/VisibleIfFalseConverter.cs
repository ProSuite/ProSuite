using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProSuite.AGP.Editing.Picker
{
	class VisibleIfFalseConverter:IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool)
			{
				bool boolvalue = (bool) value;
				return boolvalue ? Visibility.Collapsed : Visibility.Visible;
			}
			return Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter,
		                          CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
