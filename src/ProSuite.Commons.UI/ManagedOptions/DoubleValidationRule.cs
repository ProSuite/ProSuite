using System.Globalization;
using System.Windows.Controls;

namespace ProSuite.Commons.UI.ManagedOptions
{
	public class DoubleValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			string input = value as string;
			if (string.IsNullOrEmpty(input))
			{
				return new ValidationResult(false, "Value cannot be empty.");
			}

			// Get the decimal separator for the current culture
			string decimalSeparator = cultureInfo.NumberFormat.NumberDecimalSeparator;

			// Allow partial inputs like "9." or "-" where "." matches the culture's decimal separator
			if (input.EndsWith(decimalSeparator) || input == "-")
			{
				return ValidationResult.ValidResult;
			}

			// Check if it's a valid double using the current culture
			if (double.TryParse(input, NumberStyles.Any, cultureInfo, out _))
			{
				return ValidationResult.ValidResult;
			}

			return new ValidationResult(false, "Please enter a valid number.");
		}
	}
}
