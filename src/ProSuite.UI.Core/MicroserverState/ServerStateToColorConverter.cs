using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ProSuite.UI.Core.MicroserverState
{
	[ValueConversion(typeof(bool), typeof(SolidColorBrush))]
	public class ServerStateToColorConverter : IValueConverter
	{
		#region Implementation of IValueConverter

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (! (value is ServiceState serviceState))
			{
				throw new ArgumentException($"The value {value} is not of type ServiceState");
			}

			SolidColorBrush color = new SolidColorBrush(GetColor(serviceState));

			return color;
		}

		public object ConvertBack(object value, Type targetType, object parameter,
		                          CultureInfo culture)
		{
			throw new NotImplementedException("Cannot convert back to service state from color");
		}

		#endregion

		private Color GetColor(ServiceState serviceState)
		{
			switch (serviceState)
			{
				case ServiceState.Starting:
					return Colors.Orange;

				case ServiceState.Serving:
					return Colors.ForestGreen;

				case ServiceState.NotServing:
					return Colors.Red;

				default:
					throw new ArgumentOutOfRangeException(nameof(serviceState), serviceState, null);
			}
		}
	}
}
