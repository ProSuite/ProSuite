using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Text
{
	public static class StringUtils
	{
		[NotNull]
		[Pure]
		public static string Replace([NotNull] string source,
		                             [NotNull] string oldValue,
		                             [NotNull] string newValue,
		                             StringComparison comparisonType)
		{
			if (source.Length == 0 || oldValue.Length == 0)
			{
				return source;
			}

			var sb = new StringBuilder();

			var startingPos = 0;
			int nextMatch;
			while ((nextMatch = source.IndexOf(oldValue, startingPos, comparisonType)) > -1)
			{
				sb.Append(source, startingPos, nextMatch - startingPos);
				sb.Append(newValue);
				startingPos = nextMatch + oldValue.Length;
			}

			sb.Append(source, startingPos, source.Length - startingPos);

			return sb.ToString();
		}

		[NotNull]
		public static string ReplaceChars([NotNull] string text,
		                                  char replacementChar,
		                                  [NotNull] params char[] charsToReplace)
		{
			Assert.ArgumentNotNull(text, nameof(text));
			Assert.ArgumentNotNull(charsToReplace, nameof(charsToReplace));

			// there can be a lot of chars (invalid path chars, for example)
			// -> assume that IndexOfAny is faster than individual checks, and actual invalid chars are rare
			if (text.IndexOfAny(charsToReplace) < 0)
			{
				return text;
			}

			// now that we now there is at least one invalid char, search/replace one by one
			// - could use regex also beyond a certain number of chars to replace
			foreach (char charToReplace in charsToReplace)
			{
				if (text.IndexOf(charToReplace) >= 0)
				{
					text = text.Replace(charToReplace, replacementChar);
				}
			}

			return text;
		}

		[NotNull]
		public static string RemoveWhiteSpaceCharacters([NotNull] string text)
		{
			return new string(text.Where(c => ! char.IsWhiteSpace(c))
			                      .ToArray());
		}

		[NotNull]
		public static string ConcatenateSorted(
			[NotNull] IEnumerable list,
			[CanBeNull] string separator,
			[CanBeNull] Comparison<string> comparison = null)
		{
			Assert.ArgumentNotNull(list, nameof(list));

			List<string> tokens = GetTokens(list);

			if (comparison != null)
			{
				tokens.Sort(comparison);
			}
			else
			{
				tokens.Sort();
			}

			return Concatenate(tokens, separator);
		}

		/// <summary>
		/// Creates a string holding the given list elements separated
		/// by the given separator
		/// </summary>
		/// <param name="list">The list with the elements</param>
		/// <param name="separator">The separator string</param>
		/// <returns>A string holding the list elements separated by
		/// the given separator</returns>
		/// <example>
		/// <b>list:</b> [1, 5, 2, 8, 1, 4, 3] (int list)<br/>
		/// <b>separator</b>: ","<br/><para/>
		/// <b>Result:</b> "1,5,2,8,1,4,3"
		/// </example>
		[NotNull]
		public static string Concatenate([NotNull] IEnumerable list,
		                                 [CanBeNull] string separator)
		{
			var sb = new StringBuilder();

			var first = true;
			foreach (object obj in list)
			{
				if (first)
				{
					sb.AppendFormat("{0}", obj);
					first = false;
				}
				else
				{
					sb.AppendFormat("{0}{1}", separator, obj);
				}
			}

			return sb.ToString();
		}

		[NotNull]
		public static string Concatenate<T>([NotNull] IEnumerable<T> list,
		                                    [NotNull] Func<T, string> toString,
		                                    [CanBeNull] string separator)
		{
			IList<string> stringParts = Concatenate(list, toString, separator, -1);

			Assert.True(stringParts.Count < 2,
			            "More than one string parts returned even though no maxElements provided.");

			return stringParts.Count == 1
				       ? stringParts[0]
				       : string.Empty;
		}

		[NotNull]
		public static string Concatenate([NotNull] IEnumerable list,
		                                 [CanBeNull] Func<object, string> toString,
		                                 [CanBeNull] string separator)
		{
			IList<string> stringParts = Concatenate(list, toString, separator, -1);

			Assert.True(stringParts.Count < 2,
			            "More than one string parts returned even though no maxElements provided.");

			return stringParts.Count == 1
				       ? stringParts[0]
				       : string.Empty;
		}

		public static StringBuilder Reverse(this StringBuilder sb, int start, int length)
		{
			if (sb == null) return null;

			if (start < 0)
				throw new ArgumentOutOfRangeException(nameof(start));
			if (start > sb.Length)
				throw new ArgumentOutOfRangeException(nameof(start));
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			if (start + length > sb.Length)
				throw new ArgumentOutOfRangeException(nameof(length));

			for (int lo = start, hi = start + length - 1; lo < hi; lo++, hi--)
			{
				(sb[lo], sb[hi]) = (sb[hi], sb[lo]);
			}

			return sb;
		}

		public static StringBuilder Trim(this StringBuilder sb)
		{
			if (sb is null) return null;

			int i = 0;
			while (i < sb.Length && char.IsWhiteSpace(sb[i])) i += 1;
			if (i > 0) sb.Remove(0, i);

			i = sb.Length;
			while (i > 0 && char.IsWhiteSpace(sb[i - 1])) i -= 1;
			if (i < sb.Length) sb.Remove(i, sb.Length - i);

			return sb;
		}

		public static StringBuilder TrimEnd(this StringBuilder sb)
		{
			if (sb is null) return null;

			int index = sb.Length;

			while (index > 0 && char.IsWhiteSpace(sb[index - 1]))
			{
				index -= 1;
			}

			if (index < sb.Length)
			{
				sb.Remove(index, sb.Length - index);
			}

			return sb;
		}

		/// <summary>
		/// Returns a string with proper-case (according to the case rules of the current culture)
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <returns>The proper-case string.</returns>
		[NotNull]
		public static string ToProperCase([NotNull] string input)
		{
			Assert.ArgumentNotNull(input, nameof(input));

			// ToTitleCase does not change UPPERCASE strings, so convert to lower first

			return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
		}

		/// <summary>
		/// Splits the character-separated value string into a list of single strings
		/// and removes white spaces at the beginning and the end of each single string.
		/// </summary>
		/// <param name="characterSeparatedList">The character separated list.</param>
		/// <param name="separators">The separators.</param>
		/// <returns></returns>
		[NotNull]
		public static List<string> SplitAndTrim([NotNull] string characterSeparatedList,
		                                        [CanBeNull] char[] separators)
		{
			string[] tokens = characterSeparatedList.Split(separators);

			return tokens.Select(token => token.Trim()).ToList();
		}

		[NotNull]
		public static List<string> SplitAndTrim([NotNull] string characterSeparatedList,
		                                        char separator)
		{
			char[] chars = { separator };

			return SplitAndTrim(characterSeparatedList, chars);
		}

		[NotNull]
		public static List<string> SplitAndTrim([NotNull] string stringSeparatedList,
		                                        [NotNull] string separator)
		{
			// different implementation necessary
			var result = new List<string>();

			int idx;
			while ((idx = stringSeparatedList.IndexOf(separator,
			                                          StringComparison.Ordinal)) >= 0)
			{
				if (idx > 0)
				{
					// it's not right at the beginning add to list
					result.Add(stringSeparatedList.Substring(0, idx));

					stringSeparatedList = stringSeparatedList.Remove(0, idx);
				}

				stringSeparatedList = stringSeparatedList.Remove(0, separator.Length);
			}

			// add the last (or only) part
			if (stringSeparatedList.Length > 0)
			{
				result.Add(stringSeparatedList);
			}

			return result;
		}

		/// <summary>
		/// Splits the character-separated value string into a list of long values.
		/// </summary>
		/// <param name="characterSeparatedList">The character separated list.</param>
		/// <param name="separators">The separators.</param>
		/// <returns></returns>
		[NotNull]
		public static List<long> Split([NotNull] string characterSeparatedList,
		                               [CanBeNull] char[] separators)
		{
			List<string> trimmedStrings = SplitAndTrim(characterSeparatedList, separators);
			return trimmedStrings.ConvertAll(StringToLong);
		}

		[NotNull]
		public static List<long> Split([NotNull] string characterSeparatedList,
		                               char separator)
		{
			char[] chars = { separator };
			return Split(characterSeparatedList, chars);
		}

		/// <summary>
		/// Returns a string enumerable that contains the substrings of an input string that are delimited by a specified Unicode character, 
		/// where the delimiters may optionally be escaped using a definable escape character.
		/// </summary>
		/// <param name="s">The string to split</param>
		/// <param name="separators">The separator characters</param>
		/// <param name="escapeCharacter">the escape character</param>
		/// <param name="removeEmptyEntries">if <c>true</c>, empty entries are excluded from the result</param>
		/// <returns>The tokens, without any escape characters that were used to escape separator characters</returns>
		[NotNull]
		public static IEnumerable<string> Split([NotNull] string s,
		                                        [NotNull] char[] separators,
		                                        char escapeCharacter,
		                                        bool removeEmptyEntries = false)
		{
			Assert.ArgumentNotNull(s, nameof(s));
			Assert.ArgumentNotNull(separators, nameof(separators));

			const int separatorLength = 1;
			var startOfSegment = 0;
			var index = 0;

			var escapeIndexes = new List<int>();

			while (index < s.Length)
			{
				index = s.IndexOfAny(separators, index);

				if (index > 0 && s[index - 1] == escapeCharacter)
				{
					escapeIndexes.Add(index - 1);

					index += separatorLength;
					continue;
				}

				if (index == -1)
				{
					break;
				}

				string token = RemoveCharactersAt(
					s.Substring(startOfSegment, index - startOfSegment),
					escapeIndexes,
					-startOfSegment);
				escapeIndexes.Clear();

				if (! removeEmptyEntries || token.Length > 0)
				{
					yield return token;
				}

				index += separatorLength;
				startOfSegment = index;
			}

			string endToken = RemoveCharactersAt(s.Substring(startOfSegment),
			                                     escapeIndexes,
			                                     -startOfSegment);

			if (! removeEmptyEntries || endToken.Length > 0)
			{
				yield return endToken;
			}
		}

		/// <summary>
		/// Returns a string enumerable that contains the substrings of an input string that are delimited by a specified Unicode character, 
		/// where the delimiters may optionally be escaped using a definable escape character.
		/// </summary>
		/// <param name="s">The string to split</param>
		/// <param name="separator">The separator string</param>
		/// <param name="escapeCharacter">the escape character</param>
		/// <param name="removeEmptyEntries">if <c>true</c>, empty entries are excluded from the result</param>
		/// <returns>The tokens, without any escape characters that were used to escape separator characters</returns>
		[NotNull]
		public static IEnumerable<string> Split([NotNull] string s,
		                                        [NotNull] string separator,
		                                        char escapeCharacter,
		                                        bool removeEmptyEntries = false)
		{
			Assert.ArgumentNotNull(s, nameof(s));
			Assert.ArgumentNotNull(separator, nameof(separator));

			var startOfSegment = 0;
			var index = 0;

			var escapeIndexes = new List<int>();

			while (index < s.Length)
			{
				index = s.IndexOf(separator, index, StringComparison.CurrentCulture);

				if (index > 0 && s[index - 1] == escapeCharacter)
				{
					escapeIndexes.Add(index - 1);

					index += separator.Length;
					continue;
				}

				if (index == -1)
				{
					break;
				}

				string token = RemoveCharactersAt(
					s.Substring(startOfSegment, index - startOfSegment),
					escapeIndexes,
					-startOfSegment);

				escapeIndexes.Clear();

				if (! removeEmptyEntries || token.Length > 0)
				{
					yield return token;
				}

				index += separator.Length;
				startOfSegment = index;
			}

			string endToken = RemoveCharactersAt(s.Substring(startOfSegment),
			                                     escapeIndexes,
			                                     -startOfSegment);

			if (! removeEmptyEntries || endToken.Length > 0)
			{
				yield return endToken;
			}
		}

		/// <summary>
		/// Returns a string with characters at specified indexes from an input string.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="indexes"></param>
		/// <param name="indexOffset"></param>
		/// <returns></returns>
		[NotNull]
		public static string RemoveCharactersAt([NotNull] string s,
		                                        [NotNull] ICollection<int> indexes,
		                                        int indexOffset = 0)
		{
			Assert.ArgumentNotNull(s, nameof(s));
			Assert.ArgumentNotNull(indexes, nameof(indexes));

			if (indexes.Count <= 0)
			{
				return s;
			}

			var sb = new StringBuilder(s.Length);
			var start = 0;
			foreach (int index in indexes)
			{
				int localIndex = index + indexOffset;

				sb.Append(s.Substring(start, localIndex - start));

				start = localIndex + 1;
			}

			sb.Append(s.Substring(start, s.Length - start));

			return sb.ToString();
		}

		[NotNull]
		public static IList<string> Concatenate<T>(
			[NotNull] IEnumerable<T> list,
			[NotNull] Func<T, string> toString,
			[CanBeNull] string separator,
			int maxElements)
		{
			Assert.ArgumentNotNull(list, nameof(list));
			Assert.ArgumentNotNull(toString, nameof(toString));

			// TODO: reuse for Concatenate(IEnumerable list ...)

			IList<string> stringParts = new List<string>();

			var builder = new StringBuilder();

			var usedCount = 0;
			foreach (T obj in list)
			{
				string stringRep = toString(obj);

				if (usedCount == 0)
				{
					builder.AppendFormat("{0}", stringRep);
				}
				else
				{
					builder.AppendFormat("{0}{1}", separator, stringRep);
				}

				usedCount++;

				if (usedCount == maxElements)
				{
					stringParts.Add(builder.ToString());
					builder.Length = 0;
					usedCount = 0;
				}
			}

			if (builder.Length > 0)
			{
				stringParts.Add(builder.ToString());
			}

			return stringParts;
		}

		/// <summary>
		/// Creates a list of strings holding the given list elements separated
		/// by the given separator.
		/// </summary>
		/// <param name="list">The list of elements placed into the strings</param>
		/// <param name="separator">The separator for each list element</param>
		/// <param name="maxElements">Maximal count of list elements used
		/// for a concatenated string.</param>
		/// <returns>List of strings of the given list elements</returns>
		/// <example>
		/// <b>list:</b> [1, 5, 2, 8, 1, 4, 3] (int list)<br/>
		/// <b>separator</b>: ","<br/>
		/// <b>maxElements</b>: 3<para/>
		/// <b>Result:</b><br/>
		///     string1: "1,5,2"<br/>
		///     string2: "8,1,4"<br/>
		///     string3: "3"
		/// </example>
		[NotNull]
		public static IList<string> Concatenate([NotNull] IEnumerable list,
		                                        [CanBeNull] string separator,
		                                        int maxElements)
		{
			return Concatenate(list, null, separator, maxElements);
		}

		/// <summary>
		/// Creates a list of strings holding the given list elements separated
		/// by the given separator.
		/// </summary>
		/// <param name="list">The list of elements placed into the strings</param>
		/// <param name="toString">The optional function to format the values in the list. 
		/// If defined this function must accept null values.</param>
		/// <param name="separator">The separator for each list element</param>
		/// <param name="maxElements">Maximal count of list elements used
		/// for a concatenated string.</param>
		/// <returns>
		/// List of strings of the given list elements
		/// </returns>
		/// <example>
		///   <b>list:</b> [1, 5, 2, 8, 1, 4, 3] (int list)<br/>
		///   <b>separator</b>: ","<br/>
		///   <b>maxElements</b>: 3<para/>
		///   <b>Result:</b><br/>
		/// string1: "1,5,2"<br/>
		/// string2: "8,1,4"<br/>
		/// string3: "3"
		///   </example>
		[NotNull]
		public static IList<string> Concatenate(
			[NotNull] IEnumerable list,
			[CanBeNull] Func<object, string> toString,
			[CanBeNull] string separator,
			int maxElements)
		{
			var result = new List<string>();

			var sb = new StringBuilder();

			var usedCount = 0;
			foreach (object obj in list)
			{
				object stringRep = toString == null
					                   ? obj
					                   : toString(obj);
				if (usedCount == 0)
				{
					sb.AppendFormat("{0}", stringRep);
				}
				else
				{
					sb.AppendFormat("{0}{1}", separator, stringRep);
				}

				usedCount++;

				if (usedCount == maxElements)
				{
					result.Add(sb.ToString());
					sb.Length = 0;
					usedCount = 0;
				}
			}

			if (sb.Length > 0)
			{
				result.Add(sb.ToString());
			}

			return result;
		}

		/// <summary>
		/// Escapes any double quotes with a preceding slash. The resulting text
		/// is for example suitable to use as string in generated code.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns></returns>
		[NotNull]
		public static string EscapeDoubleQuotes([NotNull] string text)
		{
			Assert.ArgumentNotNull(text, nameof(text));

			const string slash = "\\";
			const string quote = "\"";

			return text.Replace(quote, slash + quote);
		}

		[ContractAnnotation("s:null => true")]
		public static bool IsNullOrEmptyOrBlank([CanBeNull] string s)
		{
			return string.IsNullOrEmpty(s) || s.Trim().Length == 0;
		}

		[ContractAnnotation("s:null => false")]
		public static bool IsNotEmpty([CanBeNull] string s)
		{
			return ! string.IsNullOrEmpty(s) && s.Trim().Length > 0;
		}

		[NotNull]
		public static string ReplaceOverflow([NotNull] string s,
		                                     int overflowLength,
		                                     [NotNull] string overflowSuffix)
		{
			Assert.ArgumentNotNull(s, nameof(s));
			Assert.ArgumentNotNull(overflowSuffix, nameof(overflowSuffix));

			var sb = new StringBuilder(s);

			int removeFromIndex = s.Length - overflowLength - overflowSuffix.Length;

			sb.Remove(removeFromIndex, sb.Length - removeFromIndex);
			sb.Append(overflowSuffix);

			return sb.ToString();
		}

		/// <summary>
		/// Remove all leading and trailing white-space characters.
		/// </summary>
		/// <param name="s">The string to trim, may be null</param>
		/// <returns>The trimmed string, or null if the original was null</returns>
		[CanBeNull]
		public static string Trim([CanBeNull] string s)
		{
			return string.IsNullOrEmpty(s)
				       ? s
				       : s.Trim();
		}

		/// <summary>
		/// Replaces special characters, i.e. non-letters and non-digits in the given text with
		/// the specified replacement character.
		/// Optionally all diacritics (accents) can be removed, such as è or â.
		/// Ligatures, such as Œ are not removed!
		/// </summary>
		/// <param name="text"></param>
		/// <param name="specialCharReplacement"></param>
		/// <param name="removeDiacritics"></param>
		/// <returns></returns>
		public static string ReplaceSpecialCharacters(
			[NotNull] string text,
			char? specialCharReplacement = '_',
			bool removeDiacritics = true)
		{
			// TODO: The proper solution would be doing the same as this:
			// https://github.com/apache/lucenenet/blob/master/src/Lucene.Net.Analysis.Common/Analysis/Miscellaneous/ASCIIFoldingFilter.cs

			var input = removeDiacritics ? text.Normalize(NormalizationForm.FormD) : text;

			var resultBuilder = new StringBuilder();

			foreach (char c in input)
			{
				UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

				if (removeDiacritics && unicodeCategory == UnicodeCategory.NonSpacingMark)
				{
					// Diacritic:
					continue;
				}

				if (! char.IsLetterOrDigit(c))
				{
					// Replace other special characters (blank, ', ", etc.)
					if (specialCharReplacement.HasValue)
					{
						resultBuilder.Append(specialCharReplacement.Value);
					}
				}
				else
				{
					resultBuilder.Append(c);
				}
			}

			return resultBuilder.ToString();
		}

		/// <summary>
		/// Join the given strings into one string, separated by the
		/// given separator string. 
		/// </summary>
		/// <remarks>
		/// This is similar to string.Join(), but works with any IEnumerable,
		/// whereas string.Join() only works with an array of strings.
		/// </remarks>
		[NotNull]
		public static string Join<T>([CanBeNull] string separator,
		                             [CanBeNull] IEnumerable<T> values)
		{
			if (values == null)
			{
				return string.Empty;
			}

			if (separator == null)
			{
				separator = string.Empty;
			}

			var sb = new StringBuilder();

			using (var enumerator = values.GetEnumerator())
			{
				if (! enumerator.MoveNext())
				{
					return string.Empty;
				}

				if (enumerator.Current != null)
				{
					sb.Append(enumerator.Current);
				}

				while (enumerator.MoveNext())
				{
					sb.Append(separator);

					if (enumerator.Current != null)
					{
						sb.Append(enumerator.Current);
					}
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Join given values into a string. Use given separator between
		/// values. If <paramref name="maxLength"/> is reached, stop
		/// and add the given <paramref name="ellipsis"/> ("..." by default).
		/// The resulting string may be longer than <paramref name="maxLength"/>
		/// by at most the length of the <paramref name="ellipsis"/> string.
		/// </summary>
		public static string Join<T>(string separator, IEnumerable<T> values,
		                             int maxLength, string ellipsis = null)
		{
			if (values is null) return string.Empty;
			if (maxLength <= 0) return string.Empty;

			if (separator is null) separator = string.Empty;
			if (ellipsis is null) ellipsis = "...";

			using (var enumerator = values.GetEnumerator())
			{
				if (! enumerator.MoveNext())
				{
					return string.Empty;
				}

				var sb = new StringBuilder();

				if (enumerator.Current != null)
				{
					sb.Append(enumerator.Current);
				}

				while (enumerator.MoveNext())
				{
					sb.Append(separator);

					int length = sb.Length;

					if (enumerator.Current != null)
					{
						sb.Append(enumerator.Current);
					}

					if (sb.Length > maxLength)
					{
						sb.Length = length; // remove last item
						sb.Append(ellipsis);
						break;
					}
				}

				return sb.ToString();
			}
		}

		/// <summary>
		/// Formats the provided value in non-scientific (no exp notation, even for very large / very small numbers)
		/// </summary>
		/// <param name="value">The value to format</param>
		/// <param name="formatProvider">The format provider</param>
		/// <returns></returns>
		[NotNull]
		public static string FormatNonScientific(double value,
		                                         [NotNull] IFormatProvider formatProvider)
		{
			decimal decimalValue = (decimal) value;
			string result = string.Format(formatProvider, "{0:F99}", decimalValue).TrimEnd('0');

			if (result.Length == 0)
			{
				return result;
			}

			return char.IsDigit(result, result.Length - 1)
				       ? result
				       : result.Substring(0, result.Length - 1);
		}

		[NotNull]
		public static string FormatPreservingDecimalPlaces(
			double value,
			[NotNull] IFormatProvider formatProvider)
		{
			decimal valueDecimal = (decimal) value;
			string result = string.Format(formatProvider, "{0:F99}", valueDecimal)
			                      .TrimEnd('0');

			if (result.Length == 0)
			{
				return result;
			}

			return char.IsDigit(result, result.Length - 1)
				       ? result
				       : $"{result}0";
		}

		public static bool Contains([CanBeNull] string containing,
		                            [NotNull] string searchString,
		                            StringComparison comparisonType)
		{
			if (containing == null)
			{
				return false;
			}

			return containing.IndexOf(searchString, comparisonType) >= 0;
		}

		[NotNull]
		private static List<string> GetTokens([NotNull] IEnumerable list)
		{
			var result = new List<string>();

			foreach (object obj in list)
			{
				result.Add(obj?.ToString() ?? string.Empty);
			}

			return result;
		}

		private static long StringToLong([CanBeNull] string s)
		{
			return Convert.ToInt64(s);
		}
	}
}
