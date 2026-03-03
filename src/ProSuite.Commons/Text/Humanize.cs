namespace ProSuite.Commons.Text
{
	/// <summary>
	/// Utilities to format data in a "human" way (assuming English).
	/// Initial version lifted from FileGDB.LinqPadDriver nuget package.
	/// </summary>
	public static class Humanize
	{
		/// <summary>
		/// Create strings like "1 item" and "5 items"
		/// </summary>
		public static string FormatCount(int count, string singular, string plural = null)
		{
			if (string.IsNullOrEmpty(singular))
			{
				return count.ToString();
			}

			if (count == 1)
			{
				return $"1 {singular}";
			}

			if (plural is null)
			{
				plural = Pluralize(singular);
			}

			return $"{count} {plural}";
		}

		/// <summary>
		/// Assuming the given <paramref name="word"/> is an
		/// English singular noun, return the corresponding plural form.
		/// </summary>
		public static string Pluralize(string word)
		{
			if (string.IsNullOrEmpty(word))
			{
				return word;
			}

			int n = word.Length;
			char e1 = n > 0 ? word[n - 1] : '$'; // last letter
			char e2 = n > 1 ? word[n - 2] : '$'; // second to last

			// Cases like "fifty" => "fifties" but not "joy" (vowel)
			if (e1 == 'y' && e2 != 'a' && e2 != 'e' && e2 != 'i' && e2 != 'o' && e2 != 'u')
			{
				return string.Concat(word.Substring(0, n - 1), "ies");
			}
			// With modern .NET versions, prefer:
			//const string vowels = "aeiou";
			//if (e1 == 'y' && !vowels.Contains(e2))
			//	return string.Concat(word.AsSpan(0, n - 1), "ies");

			// Cases like "boss" and "buzz" and "bash"
			if (e1 == 's' || e1 == 'x' || e1 == 'z' || (e1 == 'h' && (e2 == 'c' || e2 == 's')))
			{
				return word + "es";
			}

			// All other cases:
			return word + "s";
		}
	}
}
