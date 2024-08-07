using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	public abstract class BaseUniqueIdProvider<T> : IUniqueIdProvider, IInvolvedRowsProvider
	{
		[NotNull] private readonly IDictionary<T, long> _keysToId;
		[NotNull] private readonly IDictionary<long, T> _idToKeys;

		private long _uniqueIdCount;

		protected BaseUniqueIdProvider([NotNull] IEqualityComparer<T> keyComparer)
		{
			Assert.ArgumentNotNull(keyComparer, nameof(keyComparer));

			//_keysToId = new Dictionary<IList<int?>, int>(new ListComparer());
			//_idToKeys = new Dictionary<int, IList<int?>>();
			_keysToId = LargeDictionaryFactory.CreateDictionary<T, long>(
				equalityComparer: keyComparer);
			_idToKeys = LargeDictionaryFactory.CreateDictionary<long, T>();
		}

		protected IDictionary<long, T> IdToKeys => _idToKeys;

		protected long IncrementUniqueIdCount()
		{
			_uniqueIdCount++;
			return _uniqueIdCount;
		}

		public virtual long GetUniqueId([NotNull] T keys)
		{
			long uniqueId;
			if (! _keysToId.TryGetValue(keys, out uniqueId))
			{
				uniqueId = ++_uniqueIdCount;

				_keysToId.Add(keys, uniqueId);
				_idToKeys.Add(uniqueId, keys);
			}

			return uniqueId;
		}

		public abstract IList<int> GetOidFieldIndexes();

		public abstract IList<InvolvedRow> GetInvolvedRows(long uniqueId);

		public bool TryGetKey(long uniqueId, out T key)
		{
			return _idToKeys.TryGetValue(uniqueId, out key);
		}

		public bool Remove(long uniqueId)
		{
			T key;

			if (! _idToKeys.TryGetValue(uniqueId, out key))
			{
				return false;
			}

			_idToKeys.Remove(uniqueId);
			_keysToId.Remove(key);

			return true;
		}
	}
}
