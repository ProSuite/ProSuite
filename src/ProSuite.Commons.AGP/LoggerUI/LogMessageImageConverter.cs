using System;
using System.Globalization;
using System.Windows.Data;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.LoggerUI;

public class LogMessageImageConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is LogType logType)
		{
			switch (logType)
			{
				case LogType.Debug:
					return "/ProSuite.Commons.AGP;component/Images/StatusDebug_12x_16x.png";
				case LogType.Warn:
					return "/ProSuite.Commons.AGP;component/Images/StatusWarning_12x_16x.png";
				case LogType.Error:
					return "/ProSuite.Commons.AGP;component/Images/StatusCriticalError_12x_16x.png";
			}
		}

		return "/ProSuite.Commons.AGP;component/Images/StatusInformation_12x_16x.png";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		//Put reverse logic here
		throw new NotImplementedException();
	}
}
