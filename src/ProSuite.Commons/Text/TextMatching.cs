using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ProSuite.Commons.Text
{
	public static class TextMatching
	{
		/// <summary>
		/// Glob pattern matching: a star matches zero or more characters,
		/// a question mark any single character, all other characters literally.
		/// </summary>
		/// <remarks>Donated by UJR, see <see href="https://github.com/ujr/wildmatch"/></remarks>
		/// <returns>true iff pattern matches text</returns>
		public static bool WildMatch(string pattern, string text, bool ignoreCase = false)
		{
			int i = 0; // pattern index
			int ii = -1; // pattern rewind index
			int j = 0; // text index
			int jj = -1; // text rewind index
			const int eos = -1; // end of string

			for (;;)
			{
				int pc = i < pattern.Length ? pattern[i++] : eos;
				if (pc == '*')
				{
					ii = i;
					jj = j;
					continue;
				}

				int sc = j < text.Length ? text[j++] : eos;
				if (sc == eos)
					return pc == eos;

				int folded = ignoreCase ? SwapCase((char) sc) : sc;
				if (pc != '?' && pc != sc && pc != folded)
				{
					if (ii < 0) // no previous star, cannot rewind
						return false;
					i = ii; // rewind in pattern
					j = ++jj; // retry with shorter text suffix
				}
			}
		}

		private static char SwapCase(char c)
		{
			char l = char.ToLower(c);
			return l == c ? char.ToUpper(c) : l;
		}

		/// <summary>
		/// Convert a wildcard pattern (* and ?) to a regex
		/// </summary>
		/// <remarks>Could be a heavier but more general alternative
		/// to the <see cref="WildMatch"/> method</remarks>
		public static Regex WildcardToRegex(string pattern, bool ignoreCase = false)
		{
			pattern = Regex.Escape(pattern);
			pattern = pattern.Replace("\\*", ".*");
			pattern = pattern.Replace("\\?", ".");
			var options = RegexOptions.Singleline; // make '.' match '\n'
			if (ignoreCase) options |= RegexOptions.IgnoreCase;
			return new Regex($"^{pattern}$", options);
		}

		/// <summary>
		/// Pattern matching similar to git ignore patterns.
		/// If <paramref name="pattern"/> ends with a separator, then
		/// <paramref name="pathName"/> must also end with a separator.
		/// An empty pattern does not match anything. Empty file
		/// or directory names are not handled.
		/// </summary>
		/// <param name="pattern">the git-ignore-like pattern</param>
		/// <param name="pathName">a path name</param>
		/// <param name="separator">the separator used in pattern and path</param>
		/// <param name="ignoreCase">true to ignore case, false otherwise</param>
		/// <returns>true iff the pattern matches the given path name</returns>
		public static bool PathMatch(string pattern, string pathName,
		                             char separator = '/', bool ignoreCase = false)
		{
			if (string.IsNullOrEmpty(pattern)) return false;
			if (string.IsNullOrEmpty(pathName)) return false;
			var seps = new[] { separator };
			// Note: string.EndsWith(char) and string.Split(char) are not in netstandard2.0
			var patterns = pattern.Split(seps, StringSplitOptions.RemoveEmptyEntries);
			var names = pathName.Split(seps, StringSplitOptions.RemoveEmptyEntries);
			if (! PathMatch(patterns, names, ignoreCase)) return false;
			bool wantDirectory = pattern[pattern.Length - 1] == separator;
			bool isDirectory = pathName[pathName.Length - 1] == separator;
			return ! wantDirectory || isDirectory;
		}

		/// <summary>
		/// Like <see cref="WildMatch"/> but operating on path segments,
		/// using "**" as the wildcard and each segment being compared with
		/// <see cref="WildMatch"/>. Mimics the behavior of gitignore patterns.
		/// </summary>
		/// <remarks>Mimics the behaviour of gitignore patterns. Caller's
		/// duty to split the actual paths and patterns on their separator
		/// characters such as slash or backslash.</remarks>
		/// <returns>true iff patterns match names</returns>
		public static bool PathMatch(IList<string> patterns, IList<string> names, bool ignoreCase = false)
		{
			if (patterns is null)
				throw new ArgumentNullException(nameof(patterns));
			if (names is null)
				throw new ArgumentNullException(nameof(names));

			int i = 0; // pattern index
			int ii = -1; // pattern rewind index
			int j = 0; // name index
			int jj = -1; // name rewind index

			for (;;)
			{
				string pattern = i < patterns.Count ? patterns[i++] : null;
				if (pattern == "**")
				{
					ii = i;
					jj = j;
					continue;
				}

				string name = j < names.Count ? names[j++] : null;
				if (name is null)
					return pattern is null;

				bool match = pattern != null &&
				             WildMatch(pattern, name, ignoreCase);
				if (! match)
				{
					if (ii < 0) // no previous star, cannot rewind
						return false;
					i = ii; // rewind in pattern
					j = ++jj; // retry with shorter text suffix
				}
			}
		}

		/// <summary>
		/// Simple pattern matching (à la DJB multilog patterns):
		/// A star in pattern matches any string up to the first
		/// occurrence of the next pattern char in the text. A
		/// trailing star in pattern matches any string. All other
		/// characters match literally. NOTICE that this is much
		/// simpler than the usual file name patterns!
		/// </summary>
		/// <returns>true iff pattern matches text</returns>
		/// <remarks>Donated by LocationFinder</remarks>
		public static bool SimpleMatch(string pattern, string text, bool ignoreCase = false)
		{
			if (pattern is null)
				throw new ArgumentNullException(nameof(pattern));
			if (text is null)
				return false;

			int i = 0; // pattern index
			int k = 0; // text index
			while (i < pattern.Length && k < text.Length)
			{
				char pc = pattern[i++];
				if (pc == '*') // match up to next pattern char in text
				{
					if (i < pattern.Length)
					{
						pc = pattern[i];
						for (; k < text.Length - 1; k++)
						{
							var tc = text[k];
							var folded = ignoreCase ? SwapCase(tc) : tc;
							if (pc == tc || pc == folded) break;
						}
					}
					else return true; // trailing * matches any text
				}
				else // literal match
				{
					var tc = text[k];
					var folded = ignoreCase ? SwapCase(tc) : tc;
					if (pc != tc && pc != folded) return false;
					k++;
				}
			}

			if (k < text.Length) return false; // more text than pattern
			return i >= pattern.Length || i + 1 >= pattern.Length && pattern[i] == '*';
		}
	}
}
