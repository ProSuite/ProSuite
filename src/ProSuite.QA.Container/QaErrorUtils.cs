using System;
using System.Collections.Generic;
using System.Globalization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public static class QaErrorUtils
	{
		/// <summary>
		/// Assigns the values reported by a test to the two numeric and the one text
		/// value slot of an issue.
		/// </summary>
		public static void GetValues([CanBeNull] IEnumerable<object> values,
		                             out double? doubleValue1,
		                             out double? doubleValue2,
		                             out string textValue)
		{
			doubleValue1 = null;
			doubleValue2 = null;
			textValue = null;

			if (values == null)
			{
				return;
			}

			foreach (object value in values)
			{
				if (value == null)
				{
					continue;
				}

				if (doubleValue1 == null)
				{
					doubleValue1 = TryGetDoubleValue(value);
					if (doubleValue1 != null)
					{
						continue;
					}
				}

				if (doubleValue2 == null)
				{
					doubleValue2 = TryGetDoubleValue(value);
					if (doubleValue2 != null)
					{
						continue;
					}
				}

				if (textValue == null)
				{
					var stringValue = value as string;
					if (stringValue != null)
					{
						textValue = stringValue;
						continue;
					}

					textValue = Convert.ToString(value, CultureInfo.InvariantCulture);
				}

				// if we get here then the value could not be assigned to an output
			}
		}

		private static double? TryGetDoubleValue([NotNull] object value)
		{
			if (value is double)
			{
				return (double) value;
			}

			return value is float || value is int || value is short || value is decimal
				       ? (double?) Convert.ToDouble(value)
				       : null;
		}
	}
}
