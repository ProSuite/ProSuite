using System.Collections.Generic;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Tests.Transformers
{
	public class SimpleUniqueIdProvider<T> : BaseUniqueIdProvider<T>
		where T : IUniqueIdKey
	{
		public SimpleUniqueIdProvider(IEqualityComparer<T> keyComparer)
			: base(keyComparer) { }

		public override IList<InvolvedRow> GetInvolvedRows(int uniqueId)
		{
			if (! TryGetKey(uniqueId, out T key)) return new List<InvolvedRow>();

			return key.GetInvolvedRows();
		}

		public override IList<int> GetOidFieldIndexes()
		{
			return new List<int>();
		}

		public override int GetUniqueId(T key)
		{
			if (key.IsVirtuell)
			{
				int uniqueId = IncrementUniqueIdCount();
				return uniqueId;
			}

			return base.GetUniqueId(key);
		}
	}
}
