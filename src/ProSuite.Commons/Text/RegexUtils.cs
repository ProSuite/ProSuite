using System.Text.RegularExpressions;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Text
{
	/// <summary>
	/// Utility methods for regular expressions
	/// </summary>
	public static class RegexUtils
	{
		/// <summary>
		/// Gets a regular expression string for a given wildcard pattern (e.g. "*.exe", "xyz?").
		/// </summary>
		/// <param name="wildCardPattern">The wild card pattern.</param>
		/// <param name="matchCompleteString">if set to <c>true</c> then the complete string is to be matched.</param>
		/// <returns></returns>
		[NotNull]
		public static string GetWildcardExpression([NotNull] string wildCardPattern,
		                                           bool matchCompleteString = false)
		{
			Assert.ArgumentNotNull(wildCardPattern, nameof(wildCardPattern));

			// Escape regexp special character in pattern
			wildCardPattern = wildCardPattern
			                  .Replace(@"\", @"\\")
			                  .Replace(".", @"\.")
			                  .Replace("(", @"\(")
			                  .Replace(")", @"\)")
			                  .Replace("[", @"\[")
			                  .Replace("]", @"\]")
			                  .Replace("+", @"\+")
			                  .Replace("|", @"\|")
			                  .Replace("$", @"\$")
			                  .Replace("^", @"\^")
			                  .Replace("{", @"\{")
			                  .Replace("}", @"\}");

			// Replace valid wildcards with regexp equivalents
			wildCardPattern = wildCardPattern
			                  .Replace('?', '.')
			                  .Replace("*", ".*");

			return matchCompleteString
				       ? $"^{wildCardPattern}$"
				       : wildCardPattern;
		}

		/// <summary>
		/// Gets a regex for a wildcard match string
		/// </summary>
		/// <param name="matchString">The match string.</param>
		/// <param name="matchCase">if set to <c>true</c> [match case].</param>
		/// <param name="matchCompleteString">if set to <c>true</c> then the complete string is to be matched.</param>
		/// <returns></returns>
		[NotNull]
		public static Regex GetWildcardMatchRegex([NotNull] string matchString,
		                                          bool matchCase,
		                                          bool matchCompleteString = false)
		{
			Assert.ArgumentNotNullOrEmpty(matchString, nameof(matchString));

			string expression = GetWildcardExpression(matchString, matchCompleteString);

			return matchCase
				       ? new Regex(expression, RegexOptions.Singleline)
				       : new Regex(expression, RegexOptions.IgnoreCase | RegexOptions.Singleline);
		}
	}
}
