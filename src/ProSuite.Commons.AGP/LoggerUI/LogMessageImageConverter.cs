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
					return "/ProSuite.Commons.AGP;component/LoggerUI/Images/StatusDebug_16x.png";
				case LogType.Warn:
					return "/ProSuite.Commons.AGP;component/LoggerUI/Images/StatusWarning_16x.png";
				case LogType.Error:
					return "/ProSuite.Commons.AGP;component/LoggerUI/Images/StatusError_16x.png";
			}
		}

		return "/ProSuite.Commons.AGP;component/LoggerUI/Images/StatusInformation_16x.png";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		//Put reverse logic here
		throw new NotImplementedException();
	}
}
