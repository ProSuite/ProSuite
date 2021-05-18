using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ProSuite.Commons.UI.WPF
{
	/// <summary>
	/// Adapted from https://stackoverflow.com/questions/8533546/use-of-boolean-to-color-converter-in-xaml
	/// </summary>
	[ValueConversion(typeof(bool), typeof(SolidColorBrush))]
	public class BoolToColorBrushConverter : IValueConverter
	{
		#region Implementation of IValueConverter

		/// <summary>
		/// Converts a boolean value to a SolidColorBrush.
		/// </summary>
		/// <param name="value">Bolean value controlling which color should be returned.</param>
		/// <param name="targetType"></param>
		/// <param name="parameter">A string in the format [ColorNameIfTrue;ColorNameIfFalse;OpacityNumber]
		/// may be provided for customization, default is [LimeGreen;DarkGray;1.0].</param>
		/// <param name="culture"></param>
		/// <returns>A SolidColorBrush in the supplied or default colors depending on the state of value.</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			SolidColorBrush color;
			// Setting default values
			var colorIfTrue = Colors.LimeGreen;
			var colorIfFalse = Colors.DarkGray;
			double opacity = 1;

			// Parsing converter parameter
			if (parameter != null)
			{
				// Parameter format: [ColorNameIfTrue;ColorNameIfFalse;OpacityNumber]
				var parameterString = parameter.ToString();

				if (! string.IsNullOrEmpty(parameterString))
				{
					var parameters = parameterString.Split(';');

					if (parameters.Length > 0 && ! string.IsNullOrEmpty(parameters[0]))
					{
						colorIfTrue = ColorFromName(parameters[0]);
					}

					if (parameters.Length > 1 && ! string.IsNullOrEmpty(parameters[1]))
					{
						colorIfFalse = ColorFromName(parameters[1]);
					}

					if (parameters.Length > 2 && ! string.IsNullOrEmpty(parameters[2]))
					{
						string transparencyString = parameters[2];
						double parseResult;
						if (double.TryParse(transparencyString, NumberStyles.AllowDecimalPoint,
						                    CultureInfo.InvariantCulture.NumberFormat,
						                    out parseResult))
						{
							opacity = parseResult;
						}
					}
				}
			}

			color = value != null && (bool) value
				        ? new SolidColorBrush(colorIfTrue)
				        : new SolidColorBrush(colorIfFalse);

			color.Opacity = opacity;

			return color;
		}

		public object ConvertBack(object value, Type targetType, object parameter,
		                          CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion

		private static Color ColorFromName(string colorName)
		{
			System.Drawing.Color systemColor = System.Drawing.Color.FromName(colorName);
			return Color.FromArgb(systemColor.A, systemColor.R, systemColor.G, systemColor.B);
		}
	}
}
