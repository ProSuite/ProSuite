using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DotLiquid
{
	public class NameValuePair
	{
		public NameValuePair(KeyValuePair<string, string> keyValuePair)
			: this(keyValuePair.Key, keyValuePair.Value) { }

		public NameValuePair([NotNull] string name, [CanBeNull] string value)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			Name = name;
			Value = value;
		}

		[NotNull]
		[UsedImplicitly]
		public string Name { get; }

		[CanBeNull]
		[UsedImplicitly]
		public string Value { get; }
	}
}
