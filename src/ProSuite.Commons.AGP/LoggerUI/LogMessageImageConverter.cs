using System;
using System.Globalization;
using System.Windows.Data;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.LoggerUI
{
	public class LogMessageImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is LogType logType)
			{
				switch (logType)
				{
					case LogType.Debug:
						return "/Dira.ProSuite.EditTools;component/Images/StatusDebug_12x_16x.png";
					case LogType.Warn:
						return "/Dira.ProSuite.EditTools;component/Images/StatusWarning_12x_16x.png";
					case LogType.Error:
						return "/Dira.ProSuite.EditTools;component/Images/StatusCriticalError_12x_16x.png";
				}
			}

			return "/Dira.ProSuite.EditTools;component/Images/StatusInformation_12x_16x.png";
		}

		public object ConvertBack(object value, Type targetType, object parameter,
		                          CultureInfo culture)
		{
			//Put reverse logic here
			throw new NotImplementedException();
		}
	}
}
