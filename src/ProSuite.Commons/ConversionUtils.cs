using System;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	public static class ConversionUtils
	{
		private static readonly Regex _isGuid =
			new Regex(
				@"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$",
				RegexOptions.Compiled);

		/// <summary>
		/// Determines whether the specified candidate is a valid GUID.
		/// </summary>
		/// <param name="candidate">The candidate.</param>
		/// <returns>
		/// 	<c>true</c> if the specified candidate is GUID; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsValidGuid([CanBeNull] string candidate)
		{
			return ! string.IsNullOrEmpty(candidate) && _isGuid.IsMatch(candidate);
		}

		public static void ParseTo([NotNull] Type type, [CanBeNull] string value,
		                           [CanBeNull] IFormatProvider culture,
		                           out object castedValue)
		{
			ParseTo(type, value, culture, false, out castedValue);
		}

		public static void ParseTo([NotNull] Type type,
		                           [CanBeNull] string value,
		                           [CanBeNull] IFormatProvider culture,
		                           bool nullAlwaysParses,
		                           out object castedValue)
		{
			if (! TryParseTo(type, value, culture, nullAlwaysParses, out castedValue))
			{
				throw new ArgumentException(
					string.Format("Cannot convert value [ {0} ] to type {1}", value, type));
			}
		}

		public static bool TryParseTo([NotNull] Type type,
		                              [CanBeNull] string stringValue,
		                              [CanBeNull] IFormatProvider culture,
		                              out object castValue)
		{
			return TryParseTo(type, stringValue, culture, false, out castValue);
		}

		public static bool TryParseTo([NotNull] Type type,
		                              [CanBeNull] string stringValue,
		                              [CanBeNull] IFormatProvider culture,
		                              bool nullAlwaysParses,
		                              out object castValue)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			bool isValid;

			if (stringValue == null && nullAlwaysParses)
			{
				isValid = true;
				castValue = null;
			}
			else if (type == typeof(bool))
			{
				bool boolResult;
				isValid = bool.TryParse(stringValue, out boolResult);
				castValue = boolResult;
			}
			else if (type == typeof(string))
			{
				isValid = true;
				castValue = stringValue;
			}
			else if (type == typeof(DateTime))
			{
				DateTime dateTimeResult;
				isValid = DateTime.TryParse(stringValue, culture,
				                            DateTimeStyles.None,
				                            out dateTimeResult);
				castValue = dateTimeResult;
			}
			else if (type == typeof(int))
			{
				int intResult;
				isValid = int.TryParse(stringValue, NumberStyles.Any, culture,
				                       out intResult);
				castValue = intResult;
			}
			else if (type == typeof(double))
			{
				double doubleResult;
				isValid = double.TryParse(stringValue, NumberStyles.Any,
				                          culture, out doubleResult);
				castValue = doubleResult;
			}
			else if (type.IsEnum)
			{
				if (stringValue == null)
				{
					castValue = null;
					isValid = false;
				}
				else
				{
					try
					{
						// Remove namespace and type prefix if present for enum parsing from python
						int lastDot = stringValue.LastIndexOf('.');
						if (lastDot >= 0 && lastDot < stringValue.Length - 1)
						{
							stringValue = stringValue.Substring(lastDot + 1);
						}
						castValue = Enum.Parse(type, stringValue, true);
						isValid = true;
					}
					catch (ArgumentException)
					{
						castValue = null;
						isValid = false;
					}
				}
			}
			else
			{
				castValue = null;
				isValid = false;

				ConstructorInfo info =
					type.GetConstructor(
						new[] {typeof(string), typeof(IFormatProvider)});
				if (info != null)
				{
					try
					{
						castValue = Activator.CreateInstance(type, stringValue, culture);
						isValid = true;
					}
					catch (TargetInvocationException)
					{
						castValue = null;
						isValid = false;
					}
				}
			}

			return isValid;
		}
	}
}
