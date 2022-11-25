using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	public class QueryTableUniqueIdProvider : BaseUniqueIdProvider<IList<int?>>,
	                                          IUniqueIdProvider<IReadOnlyFeature>
	{
		private class ListComparer : IEqualityComparer<IList<int?>>, IComparer<IList<int?>>
		{
			public int Compare(IList<int?> x, IList<int?> y)
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
					d = (x[i] ?? int.MinValue).CompareTo(y[i] ?? int.MinValue);
					if (d != 0)
					{
						return d;
					}
				}

				return 0;
			}

			public bool Equals(IList<int?> x, IList<int?> y)
			{
				return Compare(x, y) == 0;
			}

			public int GetHashCode(IList<int?> values)
			{
				unchecked
				{
					var hash = 19;

					foreach (int? value in values)
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
			:base(new ListComparer())
		{
			Assert.ArgumentNotNull(baseTablePerOidFieldIndex, nameof(baseTablePerOidFieldIndex));

			_baseTablePerOidFieldIndex = baseTablePerOidFieldIndex;
		}

		public override IList<int> GetOidFieldIndexes()
		{
			return new List<int>(_baseTablePerOidFieldIndex.Keys);
		}

		public IList<int?> GetKeys(IReadOnlyFeature feature)
		{
			var keys = new List<int?>(5); // _keysToId.Count);

			foreach (int fieldIndex in _baseTablePerOidFieldIndex.Keys)
			{
				object oidValue = feature.get_Value(fieldIndex);

				keys.Add(oidValue as int?);
			}

			return keys;
		}

		public int GetUniqueId([NotNull] IReadOnlyFeature feature)
		{
			IList<int?> keys = GetKeys(feature);
			int uniqueId = GetUniqueId(keys);
			return uniqueId;
		}

		public int Compare(IList<int?> x, IList<int?> y)
		{
			return new ListComparer().Compare(x, y);
		}

		public override IList<InvolvedRow> GetInvolvedRows(int uniqueId)
		{
			List<InvolvedRow> involvedRows = new InvolvedRows();
			if (!IdToKeys.TryGetValue(uniqueId, out IList<int?> keys))
			{
				return involvedRows;
			}

			var iKey = 0;
			foreach (var baseTable in _baseTablePerOidFieldIndex.Values)
			{
				int? key = keys[iKey];
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
