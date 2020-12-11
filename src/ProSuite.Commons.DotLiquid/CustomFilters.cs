using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DotLiquid
{
	public static class CustomFilters
	{
		[CanBeNull]
		[UsedImplicitly]
		public static string Default([CanBeNull] object input,
		                             [CanBeNull] string defaultValue)
		{
			if (input == null)
			{
				return defaultValue;
			}

			var inputString = input as string;

			if (inputString == null)
			{
				// not a string, not null
				return input.ToString();
			}

			return string.IsNullOrEmpty(inputString)
				       ? defaultValue
				       : inputString;
		}

		[CanBeNull]
		[UsedImplicitly]
		public static string Format(object input, string valueFormat)
		{
			if (input == null)
			{
				return null;
			}

			if (valueFormat == null)
			{
				return input.ToString();
			}

			string formatString = "{0:" + valueFormat + "}";

			return string.Format(formatString, input);
		}
	}
}