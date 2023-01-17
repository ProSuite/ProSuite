using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	public class QueryTableUniqueIdProvider : BaseUniqueIdProvider<IList<long?>>,
	                                          IUniqueIdProvider<IReadOnlyFeature>
	{
		private class ListComparer : IEqualityComparer<IList<long?>>, IComparer<IList<long?>>
		{
			public int Compare(IList<long?> x, IList<long?> y)
			{
				if (x == y)
				{
					return 0;
				}

				if (x == null)
				{
					return -1;
				}

				if (y == null)
				{
					return 1;
				}

				int count = x.Count;
				int d = count.CompareTo(y.Count);
				if (d != 0)
				{
					return d;
				}

				for (var i = 0; i < count; i++)
				{
					d = (x[i] ?? long.MinValue).CompareTo(y[i] ?? long.MinValue);
					if (d != 0)
					{
						return d;
					}
				}

				return 0;
			}

			public bool Equals(IList<long?> x, IList<long?> y)
			{
				return Compare(x, y) == 0;
			}

			public int GetHashCode(IList<long?> values)
			{
				unchecked
				{
					var hash = 19;

					foreach (long? value in values)
					{
						hash = hash * 31 + value.GetHashCode();
					}

					return hash;
				}
			}
		}

		[NotNull] private readonly IDictionary<int, IReadOnlyTable> _baseTablePerOidFieldIndex;

		private int _uniqueIdCount;

		public QueryTableUniqueIdProvider(
			[NotNull] IDictionary<int, IReadOnlyTable> baseTablePerOidFieldIndex)
			: base(new ListComparer())
		{
			Assert.ArgumentNotNull(baseTablePerOidFieldIndex, nameof(baseTablePerOidFieldIndex));

			_baseTablePerOidFieldIndex = baseTablePerOidFieldIndex;
		}

		public override IList<int> GetOidFieldIndexes()
		{
			return new List<int>(_baseTablePerOidFieldIndex.Keys);
		}

		private IList<long?> GetKeys(IReadOnlyRow row)
		{
			var keys = new List<long?>(5); // _keysToId.Count);

			foreach (int fieldIndex in _baseTablePerOidFieldIndex.Keys)
			{
				long? oid = GdbObjectUtils.ReadRowOidValue(row, fieldIndex);

				keys.Add(oid);
			}

			return keys;
		}

		public long GetUniqueId([NotNull] IReadOnlyFeature feature)
		{
			IList<long?> keys = GetKeys(feature);
			long uniqueId = GetUniqueId(keys);
			return uniqueId;
		}

		public int Compare(IList<long?> x, IList<long?> y)
		{
			return new ListComparer().Compare(x, y);
		}

		public override IList<InvolvedRow> GetInvolvedRows(long uniqueId)
		{
			List<InvolvedRow> involvedRows = new InvolvedRows();
			if (! IdToKeys.TryGetValue(uniqueId, out IList<long?> keys))
			{
				return involvedRows;
			}

			var iKey = 0;
			foreach (var baseTable in _baseTablePerOidFieldIndex.Values)
			{
				long? key = keys[iKey];
				iKey++;

				if (key == null)
				{
					continue;
				}

				involvedRows.Add(new InvolvedRow(baseTable.Name, key.Value));
			}

			return involvedRows;
		}
	}
}
