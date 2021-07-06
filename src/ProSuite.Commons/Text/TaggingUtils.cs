using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Text
{
	public static class TaggingUtils
	{
		private const string Separator = ",";   // used when joining
		private const string Separators = ",;"; // used when splitting

		public static string AddTag(string tags, string tag)
		{
			if (string.IsNullOrEmpty(tag)) return tags;
			if (string.IsNullOrEmpty(tags)) return tag;
			if (HasTag(tags, tag)) return tags;
			return string.Concat(tags, Separator, tag);
		}

		public static string AddTags(string tags, params string[] wanted)
		{
			if (wanted == null || wanted.Length < 1)
				return tags;
			if (wanted.Length == 1)
				return AddTag(tags, wanted[0]);
			if (string.IsNullOrEmpty(tags))
				return string.Join(Separator, wanted);
			var existing = new HashSet<string>(SplitTags(tags));
			var extension = string.Join(Separator, wanted.Where(t => ! existing.Contains(t)));
			return extension.Length > 0 ? string.Concat(tags, Separator, extension) : tags;
		}

		public static bool HasTag(string tags, string tag)
		{
			if (tags == null) return false;
			if (string.IsNullOrEmpty(tag)) return false;

			int index = tags.IndexOf(tag, StringComparison.Ordinal);
			if (index < 0) return false;

			// There must be no alphanumeric at either end of the matched string!
			if (index > 0 && char.IsLetterOrDigit(tags, index - 1)) return false;
			int limit = index + tag.Length;
			if (limit < tags.Length && char.IsLetterOrDigit(tags, limit)) return false;

			return true;
		}

		public static bool HasTags(string tags, params string[] wanted)
		{
			if (tags == null) return false;
			if (wanted == null) return false;

			return wanted.All(tag => HasTag(tags, tag));
		}

		[NotNull]
		public static string[] SplitTags(string tags)
		{
			if (string.IsNullOrWhiteSpace(tags)) return new string[0];
			var array = tags.Split(Separators.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			return array.Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();
		}
	}
}
