using System;

namespace ProSuite.QA.Tests.KeySets
{
	internal class GuidKeySet : KeySet<Guid>
	{
		protected override Guid Cast(object key)
		{
			if (key is Guid)
			{
				return (Guid) key;
			}

			if (key is string)
			{
				return new Guid((string) key);
			}

			throw new ArgumentException(@"string or Guid expected", nameof(key));
		}
	}
}
