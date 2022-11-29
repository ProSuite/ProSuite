using System.Text;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing.Utils
{
	// Could eventually move to ProSuite.Commons.Text.StringUtils
	public static class StringExtensions
	{
		public static string Canonical([CanBeNull] this string text)
		{
			if (text is null) return null;
			text = text.Trim();
			return text.Length < 1 ? null : text;
		}

		public static string Trim([CanBeNull] this string s)
		{
			return string.IsNullOrEmpty(s) ? s : s.Trim();
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
	}
}
