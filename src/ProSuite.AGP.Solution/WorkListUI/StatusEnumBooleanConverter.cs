using System;
using System.Globalization;
using System.Windows.Data;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.Solution.WorkListUI
{
	public class StatusEnumBooleanConverter: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is WorkItemStatus status && status == WorkItemStatus.Done;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is bool flag && flag ? WorkItemStatus.Done : WorkItemStatus.Todo;
		}
	}
}
