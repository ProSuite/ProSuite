using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using System;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class WorkspaceChangesUtils
	{
		[NotNull]
		public static string Format([CanBeNull] object value,
									[NotNull] IFormatProvider formatProvider)
		{
			if (value is double)
			{
				return StringUtils.FormatPreservingDecimalPlaces((double)value, formatProvider);
			}

			return string.Format(formatProvider, "{0}", value);
		}

		[NotNull]
		public static object ConverToStandardType([NotNull] object fieldValue)
		{
			if (fieldValue is short)
			{
				return Convert.ToInt32(fieldValue);
			}

			if (fieldValue is float)
			{
				return Convert.ToDouble(fieldValue);
			}

			// return as is
			return fieldValue;
		}

		public static bool DiffersInCaseOnly([CanBeNull] string text1,
											 [CanBeNull] string text2)
		{
			return !Equals(text1, text2) &&
				   string.Equals(text1, text2, StringComparison.OrdinalIgnoreCase);
		}

		[NotNull]
		public static IEnumerable<string> Split([CanBeNull] string text)
		{
			return text?.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
				   ?? (IEnumerable<string>)new List<string>();
		}
	}
}
