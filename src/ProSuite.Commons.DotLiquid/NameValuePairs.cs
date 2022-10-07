using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DotLiquid
{
	public class NameValuePairs
	{
		public NameValuePairs(
			[NotNull] IEnumerable<KeyValuePair<string, string>> nameValuePairs)
		{
			Assert.ArgumentNotNull(nameValuePairs, nameof(nameValuePairs));

			List<KeyValuePair<string, string>> pairs = nameValuePairs.ToList();

			Value = GetDictionary(pairs);
			Entries = pairs.Select(pair => new NameValuePair(pair))
			               .ToList();
		}

		[NotNull]
		[UsedImplicitly]
		public IDictionary<string, string> Value { get; }

		[NotNull]
		[UsedImplicitly]
		public IEnumerable<string> Values => Value.Values;

		[NotNull]
		[UsedImplicitly]
		public IEnumerable<string> Keys => Value.Keys;

		[NotNull]
		[UsedImplicitly]
		public IList<NameValuePair> Entries { get; }

		[NotNull]
		private static IDictionary<string, string> GetDictionary(
			[NotNull] IEnumerable<KeyValuePair<string, string>> nameValuePairs)
		{
			var result = new Dictionary<string, string>();

			foreach (KeyValuePair<string, string> pair in nameValuePairs)
			{
				Assert.NotNullOrEmpty(pair.Key, "Key must not be null or empty");

				if (result.ContainsKey(pair.Key))
				{
					// ignore duplicates
					// alternative: use key with numeric suffix ([0],[1],[2]...)
					continue;
				}

				result.Add(pair.Key, pair.Value);
			}

			return result;
		}
	}
}
