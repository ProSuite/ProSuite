using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Collections
{
	public class DistinctValues<T>
	{
		private readonly Dictionary<T, DistinctValue<T>> _distinctValues;
		private int _nullCount;
		private readonly bool _isValueType;

		/// <summary>
		/// Initializes a new instance of the <see cref="DistinctValues&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="comparer">The comparer.</param>
		public DistinctValues([CanBeNull] IEqualityComparer<T> comparer = null)
		{
			_distinctValues = new Dictionary<T, DistinctValue<T>>(comparer);
			_isValueType = typeof(T).IsValueType;
		}

		public void AddNull()
		{
			_nullCount++;
		}

		public void Add(T value)
		{
			// Note: ignore r# warning (comparison is only done for non-value types, but r# can't know that)
			// ReSharper disable once CompareNonConstrainedGenericWithNull
			if (! _isValueType && (value == null || value is DBNull))
			{
				_nullCount++;
			}
			else
			{
				DistinctValue<T> distinctValue;
				if (_distinctValues.TryGetValue(value, out distinctValue))
				{
					distinctValue.Count++;
				}
				else
				{
					_distinctValues.Add(value, new DistinctValue<T>(value, 1));
				}
			}
		}

		public bool TryGetMostFrequentValue(out T value, out int count)
		{
			int maxCount = 0;
			T maxCountValue = default(T);

			foreach (var distinctValue in _distinctValues.Values)
			{
				if (distinctValue.Count <= maxCount)
				{
					continue;
				}

				maxCount = distinctValue.Count;
				maxCountValue = distinctValue.Value;
			}

			if (_nullCount > maxCount)
			{
				value = default;
				count = _nullCount;

				return true;
			}

			if (maxCount > 0)
			{
				value = maxCountValue;
				count = maxCount;

				return true;
			}

			value = default;
			count = 0;

			return false;
		}

		public int NullCount
		{
			get { return _nullCount; }
		}

		[NotNull]
		public IEnumerable<DistinctValue<T>> Values
		{
			get { return _distinctValues.Values; }
		}

		public void Union([NotNull] DistinctValues<T> distinctValues)
		{
			_nullCount = _nullCount + distinctValues.NullCount;

			foreach (DistinctValue<T> distinctValue in distinctValues.Values)
			{
				T value = distinctValue.Value;

				DistinctValue<T> existing;
				if (_distinctValues.TryGetValue(value, out existing))
				{
					existing.Count = existing.Count + distinctValue.Count;
				}
				else
				{
					_distinctValues.Add(value, new DistinctValue<T>(value, 1));
				}
			}
		}
	}
}
