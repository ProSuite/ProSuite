using System;
using System.Windows.Data;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.LoggerUI
{

	public class LogMessageImageConverter : IValueConverter
	{
		public object Convert(
			object value,
			Type targetType,
			object parameter,
			System.Globalization.CultureInfo culture)
		{
			LogType logType = (LogType)value;
			string image = "/ProSuiteSolution;component/Images/StatusInformation_12x_16x.png";

			switch (logType)
			{
				case LogType.Debug:
					image = "/ProSuiteSolution;component/Images/StatusWarning_12x_16x.png";
					break;
				case LogType.Error:
					image = "/ProSuiteSolution;component/Images/StatusCriticalError_12x_16x.png";
					break;
				default:
					image = "/ProSuiteSolution;component/Images/StatusInformation_12x_16x.png";
					break;
			}

			return image;
		}

		public object ConvertBack(
			object value,
			Type targetType,
			object parameter,
			System.Globalization.CultureInfo culture)
		{
			//Put reverse logic here
			throw new NotImplementedException();
		}
	}
}
