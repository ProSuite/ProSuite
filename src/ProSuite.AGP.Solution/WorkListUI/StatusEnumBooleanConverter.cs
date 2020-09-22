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
			WorkItemStatus status = (WorkItemStatus) value;
			if (status == WorkItemStatus.Done)
			{
				return true;
			}
			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool val = (bool) value;
			if (val)
			{
				return WorkItemStatus.Done;
			}
			return WorkItemStatus.Todo;
		}
	}
}
