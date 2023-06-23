using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing.Utils
{
	public static class FilterUtils
	{
		/// <summary>
		/// Split given <param name="text"/> at whitespace, punctuation
		/// (including underscore), and at lowercase-to-uppercase boundaries.
		/// Camel and Pascal cased words expand into any subsequence (not subset).
		/// For example, "FooBar: -Quux!" yields "foo", "foobar" "bar", "quux".
		/// </summary>
		public static IEnumerable<string> MakeTags(string text, string field = null)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				yield break;
			}

			var buffer = new List<int>();
			int index = 0;

			while (index < text.Length)
			{
				while (index < text.Length && ! char.IsLetterOrDigit(text, index))
				{
					index += 1;
				}

				char cc;
				int start = index;
				while (index < text.Length && (char.IsLetterOrDigit(cc=text[index]) || cc == ':'))
				{
					index += 1;
				}

				if (index > start)
				{
					var part = text.Substring(start, index - start).TrimEnd(':');

					foreach (var term in CamelRide(part, buffer))
					{
						yield return string.IsNullOrEmpty(field)
							             ? term.ToLowerInvariant()
							             : string.Concat(field, ":", term.ToLowerInvariant());
					}
				}
			}
		}

		/// <summary>
		/// Parse given <param name="filterText"/> into a list of terms
		/// to be matched against a tagged object. Prefix operators "+"
		/// and "-" are preserved; interior colons as in "type:Foo" as
		/// well; all other punctuation and whitespace serve as separators.
		/// Return null if filterText is empty.
		/// </summary>
		public static IEnumerable<string> ParseFilter([CanBeNull] string filterText)
		{
			if (string.IsNullOrWhiteSpace(filterText))
			{
				yield break;
			}

			int index = 0;
			while (index < filterText.Length)
			{
				while (index < filterText.Length && ! char.IsLetterOrDigit(filterText, index))
				{
					index += 1;
				}

				char cc;
				int start = index;
				while (index < filterText.Length && (char.IsLetterOrDigit(cc=filterText[index]) || cc == ':'))
				{
					index += 1;
				}

				bool hasOp = start > 0 && ((cc = filterText[start - 1]) == '-' || cc == '+');

				if (index > start)
				{
					if (hasOp) start -= 1;
					var term = filterText.Substring(start, index - start);
					yield return term.TrimEnd(':').ToLowerInvariant();
				}
			}
		}

		/// <summary>
		/// Return true iff the given <param name="item"/> and the
		/// given <param name="filter"/> match, that is, if all tokens
		/// in the filter agree with a tag of the item.
		/// </summary>
		/// <remarks>BRUTE FORCE - time O(NM) for N item tags and M filter tokens</remarks>
		public static bool MatchFilter([CanBeNull] ITagged item, [CanBeNull] ICollection<string> filter)
		{
			if (item is null) return false;

			// No/empty filter matches any item:
			if (filter is null || filter.Count < 1) return true;

			var tags = item.Tags ?? Enumerable.Empty<string>();

			// All filter tokens must match an item token (brute force)
			foreach (string token in filter)
			{
				bool neg = token.StartsWith("-");
				var tok = neg || token.StartsWith("+") ? token.Substring(1) : token;

				if (neg)
				{
					if (tags.Contains(tok))
					{
						return false;
					}
				}
				else
				{
					if (!tags.Any(tag => tag.StartsWith(tok)))
					{
						return false;
					}
				}
			}

			return true; // all tokens match
		}

		/// <remarks>
		/// For internal use only (public for testing purposes)
		/// </remarks>
		public static IEnumerable<string> CamelRide(string name, IList<int> buffer = null)
		{
			var hits = buffer ?? new List<int>();

			hits.Clear();
			hits.Add(0); // start of name

			foreach (Match match in _caseSwapRegex.Matches(name))
			{
				hits.Add(match.Index);
			}

			if (name.Length > 0)
			{
				hits.Add(name.Length); // end of name
			}

			for (int i = 0; i < hits.Count - 1; i++)
			{
				for (int j = i + 1; j < hits.Count; j++)
				{
					yield return name.Substring(hits[i], hits[j] - hits[i]);
				}
			}
		}

		// Matches the zero-width transition from lowercase to uppercase letter:
		private static readonly Regex _caseSwapRegex = new Regex(@"(?<=[a-z])(?=[A-Z])");
	}
}
