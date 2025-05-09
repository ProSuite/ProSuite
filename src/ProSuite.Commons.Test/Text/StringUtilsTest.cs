using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.Test.Text
{
	[TestFixture]
	public class StringUtilsTest
	{
		private const string _quote = "\"";
		private const string _slash = @"\";

		[Test]
		public void CanReplaceIgnoringCase()
		{
			Assert.AreEqual("aXcX", StringUtils.Replace("abcb", "b", "X",
			                                            StringComparison.OrdinalIgnoreCase));
			Assert.AreEqual("XXXX", StringUtils.Replace("aAaA", "A", "X",
			                                            StringComparison.OrdinalIgnoreCase));
			// no match:
			Assert.AreEqual("aaa", StringUtils.Replace("aaa", "b", "X",
			                                           StringComparison.OrdinalIgnoreCase));
		}

		[Test]
		public void CanReplaceChars()
		{
			string replaced = StringUtils.ReplaceChars("abCdefGh", '-', new[] { 'C', 'G' });

			Assert.AreEqual("ab-def-h", replaced);
		}

		[Test]
		public void CanReplaceCharsNoMatch()
		{
			const string input = "abcdef";
			string replaced = StringUtils.ReplaceChars(input, '-', new[] { 'X', 'Y', 'Z' });

			Assert.AreEqual(input, replaced);
			Assert.AreSame(input, replaced);
		}

		[Test]
		public void CanReplaceCharsEmpty()
		{
			string replaced = StringUtils.ReplaceChars(string.Empty, '-', new[] { 'X', 'Y', 'Z' });

			Assert.AreEqual(string.Empty, replaced);
			Assert.AreSame(string.Empty, replaced);
		}

		[Test]
		public void CanReplaceCharsNop()
		{
			const string input = "abcdef";
			string replaced = StringUtils.ReplaceChars(input, '-', new char[] { });

			Assert.AreEqual(input, replaced);
			Assert.AreSame(input, replaced);
		}

		[Test]
		public void CanSplitWithoutEscapeCharacter()
		{
			const string input = @"A|B";
			List<string> tokens = StringUtils.Split(input, "|", '\\').ToList();
			WriteTokens(tokens);

			Assert.AreEqual(2, tokens.Count);
			Assert.AreEqual("A", tokens[0]);
			Assert.AreEqual("B", tokens[1]);
		}

		[Test]
		public void CanSplitWithoutEscapeCharacterRemovingEmptyEntries()
		{
			const string input = @"A|";
			List<string> tokens =
				StringUtils.Split(input, "|", '\\', removeEmptyEntries: true).ToList();
			WriteTokens(tokens);

			Assert.AreEqual(1, tokens.Count);
			Assert.AreEqual("A", tokens[0]);
		}

		[Test]
		public void CanSplitWithNonEscapingEscapeCharacter()
		{
			const string input = @"A\B";
			List<string> tokens = StringUtils.Split(input, "|", '\\').ToList();
			WriteTokens(tokens);

			Assert.AreEqual(1, tokens.Count);
			Assert.AreEqual(@"A\B", tokens[0]);
		}

		[Test]
		public void CanSplitWithEscapeCharacter()
		{
			const string input = @"A\|B|C|D\|E|F|G\|H";
			List<string> tokens = StringUtils.Split(input, "|", '\\').ToList();
			WriteTokens(tokens);

			Assert.AreEqual(5, tokens.Count);
			Assert.AreEqual("A|B", tokens[0]);
			Assert.AreEqual("C", tokens[1]);
			Assert.AreEqual("D|E", tokens[2]);
			Assert.AreEqual("F", tokens[3]);
			Assert.AreEqual("G|H", tokens[4]);
		}

		[Test]
		public void CanSplitWithMultipleSeparatorsAndEscapeCharacter()
		{
			const string input = @"A\|B#C|D\#E|F|G\#H";
			List<string> tokens = StringUtils.Split(input, new[] { '|', '#' }, '\\').ToList();
			WriteTokens(tokens);

			Assert.AreEqual(5, tokens.Count);
			Assert.AreEqual("A|B", tokens[0]);
			Assert.AreEqual("C", tokens[1]);
			Assert.AreEqual("D#E", tokens[2]);
			Assert.AreEqual("F", tokens[3]);
			Assert.AreEqual("G#H", tokens[4]);
		}

		[Test]
		public void CanSplitWithEscapeCharacterRemovingEmptyEntries()
		{
			const string input = @"A\|\|";
			List<string> tokens =
				StringUtils.Split(input, "|", '\\', removeEmptyEntries: true).ToList();
			WriteTokens(tokens);

			Assert.AreEqual(1, tokens.Count);
			Assert.AreEqual("A||", tokens[0]); // escape character should be removed
		}

		[Test]
		public void CanRemoveCharactersAtAll()
		{
			Assert.AreEqual(string.Empty,
			                StringUtils.RemoveCharactersAt("ABC", new[] { 0, 1, 2 }));
		}

		[Test]
		public void CanRemoveCharactersAtStart()
		{
			Assert.AreEqual("BC", StringUtils.RemoveCharactersAt("ABC", new[] { 0 }));
		}

		[Test]
		public void CanRemoveCharactersAtEnd()
		{
			Assert.AreEqual("AB", StringUtils.RemoveCharactersAt("ABC", new[] { 2 }));
		}

		[Test]
		public void CanRemoveCharactersAtMiddle()
		{
			Assert.AreEqual("AC", StringUtils.RemoveCharactersAt("ABC", new[] { 1 }));
		}

		[Test]
		public void CanRemoveCharactersAtMiddle2()
		{
			Assert.AreEqual("AD", StringUtils.RemoveCharactersAt("ABCD", new[] { 1, 2 }));
		}

		private static void WriteTokens(IEnumerable<string> tokens)
		{
			int i = 0;
			foreach (string token in tokens)
			{
				i++;
				Console.WriteLine(@"{0}: {1}", i, token);
			}
		}

		[Test]
		public void CanConcatenate()
		{
			var list = new List<int> { 99, 7, 12 };

			string result = StringUtils.Concatenate(list, ",");
			Assert.AreEqual("99,7,12", result);
		}

		[Test]
		public void CanConcatenateSingle()
		{
			var list = new List<int> { 1 };

			string result = StringUtils.Concatenate(list, ",");
			Assert.AreEqual("1", result);
		}

		[Test]
		public void CanConcatenateEmpty()
		{
			var empty = Array.Empty<int>();

			string result = StringUtils.Concatenate(empty, ",");
			Assert.AreEqual(string.Empty, result);
		}

		[Test]
		public void CanConcatenateSorted()
		{
			var list = new List<object> { "b", "a", 1, null };
			string result = StringUtils.ConcatenateSorted(list, ",");
			Assert.AreEqual(",1,a,b", result);
		}

		[Test]
		public void CanConcatenateSortedWithComparer()
		{
			var list = new List<object> { "b", "a", 1, null };
			string result = StringUtils.ConcatenateSorted(
				list, ",",
				(s1, s2) => string.Compare(s1, s2, StringComparison.Ordinal) * -1);
			Assert.AreEqual("b,a,1,", result);
		}

		[Test]
		public void CanConcatenateWithMaxSize()
		{
			var list = new List<int> { 99, 7, 12, 1 };

			IList<string> subLists = StringUtils.Concatenate(list, ",", 2);

			Assert.AreEqual(2, subLists.Count);
			Assert.AreEqual("99,7", subLists[0]);
			Assert.AreEqual("12,1", subLists[1]);
		}

		[Test]
		public void CanJoinWithMaxLength()
		{
			var values = new[] { 1, 2, 3, 4, 5, 6 };
			Assert.AreEqual("1,2,3,4,...", StringUtils.Join(",", values, 8));
			Assert.AreEqual("1,2,3,4,...", StringUtils.Join(",", values, 7));
			Assert.AreEqual("123456", StringUtils.Join(null, values, 999));
			Assert.AreEqual("1 2 3 4 etc.", StringUtils.Join(" ", values, 8, "etc."));
			Assert.AreEqual(string.Empty, StringUtils.Join(",", (int[]) null, 8));
			Assert.AreEqual(string.Empty, StringUtils.Join(null, values, 0));
		}

		[Test]
		public void CanReverse()
		{
			var sb = new StringBuilder();
			sb.Append("Hallelujah");

			var o = sb.Reverse(4, 3);
			Assert.AreEqual("Hallulejah", sb.ToString());
			Assert.AreSame(sb, o);

			sb.Reverse(sb.Length, 0); // border case
			Assert.AreEqual("Hallulejah", sb.ToString());
		}

		[Test]
		public void CanTrim()
		{
			var sb = new StringBuilder(" \n\t foo\r\nbar \t\n\r\n ");
			var o = sb.Trim();

			Assert.AreEqual("foo\r\nbar", sb.ToString());
			Assert.AreSame(sb, o);

			sb = new StringBuilder(Environment.NewLine);
			o = sb.Trim();
			Assert.AreEqual(string.Empty, sb.ToString());
			Assert.AreSame(sb, o);

			sb = new StringBuilder("full");
			sb.Trim();
			Assert.AreEqual("full", sb.ToString());
		}

		[Test]
		public void CanTrimEnd()
		{
			var sb = new StringBuilder("\tfoo \t\n ");

			var o = sb.TrimEnd();

			Assert.AreEqual("\tfoo", sb.ToString());
			Assert.AreSame(sb, o);

			sb = new StringBuilder("full");

			sb.TrimEnd();

			Assert.AreEqual("full", sb.ToString());
		}

		[Test]
		public void CanEscapeDoubleQuotes()
		{
			const string textWithQuotes = "a " + _quote + "B" + _quote + " c";
			const string textWithEscapedQuotes =
				"a " + _slash + _quote + "B" + _slash + _quote + " c";

			string escaped = StringUtils.EscapeDoubleQuotes(textWithQuotes);

			Console.WriteLine(@"text with quotes: {0}", textWithQuotes);
			Console.WriteLine(@"text with escaped quotes: {0}", textWithEscapedQuotes);

			Assert.AreEqual(textWithEscapedQuotes, escaped);
		}

		[Test]
		public void CanFormatPreservingDecimalPlacesWithNoDecimalPlaces()
		{
			AssertExpectedFormatResult(10.0,
			                           new Dictionary<string, string>
			                           {
				                           { "de-DE", "10,0" },
				                           { "en-US", "10.0" },
				                           { "de-CH", "10.0" },
				                           { "fr-FR", "10,0" }
			                           });
		}

		[Test]
		public void CanFormatPreservingDecimalPlaces()
		{
			// NOTE: double has only 15-17 significant digits. In .NET 6 the trailing
			// 0s are not always 0s but whatever is the 'shortest round-trippable' number.
			// See https://devblogs.microsoft.com/dotnet/floating-point-parsing-and-formatting-improvements-in-net-core-3-0/
			// -> In NET 6 the formatted string contains many more digits!
			AssertExpectedFormatResult(1.1234567891200000,
			                           new Dictionary<string, string>
			                           {
				                           { "de-DE", "1,12345678912" },
				                           { "en-US", "1.12345678912" },
				                           { "de-CH", "1.12345678912" },
				                           { "fr-FR", "1,12345678912" }
			                           });
		}

		[Test]
		public void CanFormatNonScientific()
		{
			// NOTE: double has only 15-17 significant digits. In .NET 6 the trailing
			// 0s are not always 0s but whatever is the 'shortest round-trippable' number.
			// See https://devblogs.microsoft.com/dotnet/floating-point-parsing-and-formatting-improvements-in-net-core-3-0/
			AssertExpectedFormatResult(1234567891200000,
			                           new Dictionary<string, string>
			                           {
				                           { "de-DE", "1234567891200000" },
				                           { "en-US", "1234567891200000" },
				                           { "de-CH", "1234567891200000" },
				                           { "fr-FR", "1234567891200000" }
			                           }, true);

			AssertExpectedFormatResult(0,
			                           new Dictionary<string, string>
			                           {
				                           { "de-DE", "0" },
				                           { "en-US", "0" },
				                           { "de-CH", "0" },
				                           { "fr-FR", "0" }
			                           }, true);

			AssertExpectedFormatResult(10.0,
			                           new Dictionary<string, string>
			                           {
				                           { "de-DE", "10" },
				                           { "en-US", "10" },
				                           { "de-CH", "10" },
				                           { "fr-FR", "10" }
			                           }, true);
		}

		[Test]
		public void CanRemoveSpecialCharacters()
		{
			string inputWithSpecialChars =
				"Œ èâöüÄÖÜßâ'and?some`more_special(characters\\that\"might|cause*troubles$";

			string resultWithoutDiacritics =
				StringUtils.ReplaceSpecialCharacters(inputWithSpecialChars);
			string resultWithDiacritics =
				StringUtils.ReplaceSpecialCharacters(inputWithSpecialChars, '_', false);
			string resultWithoutDiacriticsAndNoReplacement =
				StringUtils.ReplaceSpecialCharacters(inputWithSpecialChars, null);

			// NOTE: Ligatures such as Œ are not replaced by the method.
			Assert.AreEqual(
				"Œ_eaouAOUßa_and_some_more_special_characters_that_might_cause_troubles_",
				resultWithoutDiacritics);
			Assert.AreEqual(
				"Œ_èâöüÄÖÜßâ_and_some_more_special_characters_that_might_cause_troubles_",
				resultWithDiacritics);
			Assert.AreEqual(
				"ŒeaouAOUßaandsomemorespecialcharactersthatmightcausetroubles",
				resultWithoutDiacriticsAndNoReplacement);
		}

		private static void AssertExpectedFormatResult(
			double input,
			[NotNull] IEnumerable<KeyValuePair<string, string>> expectedResults)
		{
			AssertExpectedFormatResult(input, expectedResults, false);
		}

		private static void AssertExpectedFormatResult(
			double input,
			[NotNull] IEnumerable<KeyValuePair<string, string>> expectedResults,
			bool useNonScientificFormat)
		{
			foreach (KeyValuePair<string, string> pair in expectedResults)
			{
				string culture = pair.Key;
				string expected = pair.Value;

				string formatResult = useNonScientificFormat
					                      ? StringUtils.FormatNonScientific(
						                      input, CultureInfo.GetCultureInfo(culture))
					                      : StringUtils.FormatPreservingDecimalPlaces(
						                      input, CultureInfo.GetCultureInfo(culture));

				// Note regarding .DOT NET 6:
				// Extra digits were added to improve 'round-trippability', i.e. parsing the formatted string
				// returning to the same number.
				// See https://devblogs.microsoft.com/dotnet/floating-point-parsing-and-formatting-improvements-in-net-core-3-0/

				Assert.AreEqual(expected, formatResult);
			}
		}
	}
}
