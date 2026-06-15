using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Collections
{
	public static class CollectionUtils
	{
		public static void AddIfNotNull<T>([NotNull] this IList<T> list,
		                                   [CanBeNull] T value) where T : class
		{
			if (value == null)
			{
				return;
			}

			list.Add(value);
		}

		/// <summary>
		/// Get a new list that is the concatenation of the given lists.
		/// </summary>
		/// <typeparam name="T">Type of list elements.</typeparam>
		/// <param name="lists">The lists to concatenate.</param>
		[NotNull]
		public static List<T> Concat<T>([NotNull] IEnumerable<IList<T>> lists)
		{
			var resultList = new List<T>();

			foreach (IList<T> list in lists)
			{
				resultList.AddRange(list);
			}

			return resultList;
		}

		/// <summary>
		/// Splits the specified items into an enumeration of lists of a specified maximum list size.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items">The items.</param>
		/// <param name="maximumSize">The maximum size of the split lists.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IList<T>> Split<T>([NotNull] IEnumerable<T> items,
		                                             int maximumSize)
		{
			Assert.ArgumentNotNull(items, nameof(items));

			var currentList = new List<T>();

			var currentCount = 0;
			foreach (T item in items)
			{
				if (currentCount >= maximumSize)
				{
					yield return currentList;

					currentList = new List<T>();
					currentCount = 0;
				}

				currentCount++;
				currentList.Add(item);
			}

			if (currentCount > 0)
			{
				yield return currentList;
			}
		}

		[NotNull]
		public static List<T> GetList<T>(params T[] items)
		{
			var list = new List<T>(items.Length);

			list.AddRange(items);

			return list;
		}

		public static int GetCount<T>([NotNull] IEnumerable<T> enumerable)
		{
			Assert.ArgumentNotNull(enumerable, nameof(enumerable));

			var count = 0;

			// This construction avoids the "Local variable not used"
			// warning that shows up when using foreach.
			using (IEnumerator<T> enumerator = enumerable.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					++count;
				}
			}

			return count;
		}

		[NotNull]
		public static T[] ToArray<T>([NotNull] IEnumerable<T> items)
		{
			Assert.ArgumentNotNull(items, nameof(items));

			var list = new List<T>(items);

			var oidArray = new T[list.Count];

			list.CopyTo(oidArray);

			return oidArray;
		}

		[NotNull]
		public static IEnumerable<T> SubArray<T>([NotNull] T[] array, int startIndex,
		                                         int count)
		{
			Assert.ArgumentNotNull(array, nameof(array));

			int limitIndex = Math.Min(startIndex + count, array.Length);
			if (startIndex < 0)
			{
				startIndex = 0;
			}

			for (int index = startIndex; index < limitIndex; index++)
			{
				yield return array[index];
			}
		}

		/// <summary>
		/// Rotate the given array the given number of steps to the left.
		/// If the number of steps is negative, rotate to the right.
		/// </summary>
		/// <typeparam name="T">Array element type.</typeparam>
		/// <param name="array">The array.</param>
		/// <param name="steps">How many steps to rotate.</param>
		/// <remarks>The array is rotated in place. No extra space is needed.
		/// Rotating to the left means towards lower array indices.
		/// Rotating to the right means towards higher array indices.
		/// </remarks>
		public static void Rotate<T>([NotNull] T[] array, int steps)
		{
			Rotate(array, steps, 0, array.Length);
		}

		/// <summary>
		/// Rotate count elements starting at index in the given array
		/// the given number of steps to the left.
		/// If the number of steps is negative, rotate to the right.
		/// </summary>
		/// <typeparam name="T">Array element type.</typeparam>
		/// <param name="array">The array.</param>
		/// <param name="steps">How many steps to rotate.</param>
		/// <param name="index">Index into the array, zero-based.</param>
		/// <param name="count">Number of elements (starting at index) to rotate.</param>
		/// <remarks>The array is rotated in place. No extra space is needed.
		/// Rotating to the left means towards lower array indices.
		/// Rotating to the right means towards higher array indices.
		/// </remarks>
		public static void Rotate<T>([NotNull] T[] array, int steps, int index, int count)
		{
			Assert.ArgumentNotNull(array, nameof(array));

			if (index < 0 || index >= array.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			if (count < 0 || index + count > array.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			steps = steps % count;

			if (steps == 0)
			{
				return; // nothing to do
			}

			if (steps < 0)
			{
				steps = count + steps;
			}

			Array.Reverse(array, index, steps);
			Array.Reverse(array, index + steps, count - steps);
			Array.Reverse(array, index, count);
		}

		public static void Swap<T>(IList<T> list, int i1, int i2)
		{
			if (i1 == i2)
				return;

			T temp = list[i1];
			list[i1] = list[i2];
			list[i2] = temp;
		}

		/// <summary>
		/// See <see cref="Sort{T}(IList{T}, int, int, Func{T,T, int})"/>
		/// </summary>
		public static void Sort<T>(IList<T> list, Func<T, T, int> compare)
		{
			Sort(list, 0, list.Count, compare);
		}

		/// <summary>
		/// Sort <paramref name="count"/> items in <paramref name="list"/>,
		/// starting from <paramref name="startIndex"/>, using the given
		/// <paramref name="compare"/> function.
		/// </summary>
		public static void Sort<T>(IList<T> list, int startIndex, int count,
		                           Func<T, T, int> compare)
		{
			Assert.ArgumentNotNull(list, nameof(list));
			Assert.ArgumentCondition(0 <= startIndex && startIndex <= list.Count,
			                         "startIndex out of range");
			Assert.ArgumentCondition(0 <= count && startIndex + count <= list.Count,
			                         "count out of range");
			Assert.ArgumentNotNull(compare, nameof(compare));

			QuickSort(startIndex, startIndex + count - 1, list, compare);
		}

		private static void QuickSort<T>(int left, int right, IList<T> list,
		                                 Func<T, T, int> compare)
		{
			if (left < right)
			{
				// Partition list[left..right] using list[right] as pivot:
				T pivot = list[right];
				int i = left, j = right - 1;
				for (;;)
				{
					while (compare(list[i], pivot) <= 0 && i < right)
						++i;
					while (compare(list[j], pivot) >= 0 && j > i)
						--j;
					if (i >= j)
						break;
					Swap(list, i, j);
				}

				Swap(list, i, right); // move the pivot into place

				QuickSort(left, i - 1, list, compare);
				QuickSort(i + 1, right, list, compare);
			}
		}

		public static void MoveTo<T>([NotNull] IList<T> list, [NotNull] T item, int index)
		{
			Assert.ArgumentNotNull(list, nameof(list));

			int oldIndex = list.IndexOf(item);

			if (oldIndex == index)
			{
				return;
			}

			if (oldIndex >= 0)
			{
				list.RemoveAt(oldIndex);
				//if (oldIndex < index && index > 0)
				//{
				//    index--;
				//}
			}

			list.Insert(index, item);
		}

		public static void MoveTo<T>([NotNull] IList<T> list, IEnumerable<T> items,
		                             int newIndex)
		{
			Assert.ArgumentNotNull(list, nameof(list));
			Assert.ArgumentNotNull(items, nameof(items));

			List<ListItem<T>> listItems = GetListItems(list, items);

			bool moveUp = newIndex < GetFirstIndex(listItems);
			bool reverse = moveUp;

			SortListItems(listItems, reverse);

			foreach (ListItem<T> listItem in listItems)
			{
				MoveTo(list, listItem.Item, newIndex);
			}
		}

		public static void MoveBy<T>([NotNull] IList<T> list,
		                             [NotNull] IEnumerable<T> items, int offset)
		{
			Assert.ArgumentNotNull(list, nameof(list));
			Assert.ArgumentNotNull(items, nameof(items));

			List<ListItem<T>> listItems = GetListItems(list, items);

			bool reverse = offset > 0;

			SortListItems(listItems, reverse);

			foreach (ListItem<T> listItem in listItems)
			{
				MoveTo(list, listItem.Item, listItem.ListIndex + offset);
			}
		}

		/// <summary>
		/// Cycles through the full list <paramref name="maximumFullCycles"/> times.
		/// At the end of the list it will continue at the beginning.
		/// </summary>
		public static IEnumerable<T> Cycle<T>([NotNull] IList<T> list,
		                                      int maximumFullCycles)
		{
			int length = list.Count;
			int cycleCount = -1;
			int i = 0;
			while (cycleCount < maximumFullCycles)
			{
				i %= length;

				if (i == 0)
				{
					cycleCount++;

					if (cycleCount == maximumFullCycles)
					{
						yield break;
					}
				}

				yield return list[i];

				i++;
			}

			//for (int i = 0; i < list.Count; i++)
			//{
			//	if (i == 0)
			//	{
			//		cycleCount++;

			//		if (cycleCount == maximumFullCycles)
			//		{
			//			yield break;
			//		}
			//	}

			//	yield return list[i];
			//}
		}

		/// <summary>
		/// Check if two collections have the same elements.
		/// </summary>
		/// <typeparam name="T">The type of elements in the collections.</typeparam>
		/// <param name="enumerable1">The first collection.</param>
		/// <param name="enumerable2">The second collection.</param>
		/// <param name="comparer">Optional: The comparer used to compare elements.</param>
		/// <returns>true if the two collections have the same elements;
		/// otherwise, false</returns>
		/// <remarks>
		/// The implementation uses the mathematical definition of set equality:
		/// two sets are equal if one set contains all elements of the other and
		/// vice versa. This means that duplicate elements are ignored: for instance,
		/// [1,2,3] contains the same elements as [1,2,2,3].</remarks>
		public static bool HaveSameElements<T>([NotNull] IEnumerable<T> enumerable1,
		                                       [NotNull] IEnumerable<T> enumerable2,
											   [CanBeNull] IEqualityComparer<T> comparer = null) 
		{
			Assert.ArgumentNotNull(enumerable1, nameof(enumerable1));
			Assert.ArgumentNotNull(enumerable2, nameof(enumerable2));


			IDictionary<T, int> index = comparer != null
				                            ? new Dictionary<T, int>(comparer)
				                            : new Dictionary<T, int>();

			ICollection<T> collection1 = GetCollection(enumerable1);
			ICollection<T> collection2 = GetCollection(enumerable2);

			foreach (T element in collection1)
			{
				if (! index.ContainsKey(element))
				{
					index.Add(element, 0);
				}
			}

			foreach (T element in collection2)
			{
				if (! index.ContainsKey(element))
				{
					return false;
				}
			}

			index.Clear();

			foreach (T element in collection2)
			{
				if (! index.ContainsKey(element))
				{
					index.Add(element, 0);
				}
			}

			foreach (T element in collection1)
			{
				if (! index.ContainsKey(element))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Check if one collection is a subset of another collection.
		/// </summary>
		/// <typeparam name="T">The type of elements in the collections.</typeparam>
		/// <param name="subset">The collection to check if it is a subset.</param>
		/// <param name="superset">The collection to check against.</param>
		/// <param name="comparer">Optional: The comparer used to compare elements.</param>
		/// <returns>true if <paramref name="subset"/> is a subset of <paramref name="superset"/>;
		/// otherwise, false</returns>
		/// <remarks>
		/// The implementation uses the mathematical definition of subset:
		/// a set A is a subset of set B if all elements of A are contained in B.
		/// This means that duplicate elements are ignored: for instance,
		/// [1,2] is a subset of [1,2,2,3], and [1,1,2] is also a subset of [1,2,3].
		/// Note that by mathematical definition, every set is a subset of itself.</remarks>
		public static bool IsSubsetOf<T>([NotNull] IEnumerable<T> subset,
		                                 [NotNull] IEnumerable<T> superset,
		                                 [CanBeNull] IEqualityComparer<T> comparer = null)
		{
			Assert.ArgumentNotNull(subset, nameof(subset));
			Assert.ArgumentNotNull(superset, nameof(superset));

			IDictionary<T, int> index = comparer != null
				                            ? new Dictionary<T, int>(comparer)
				                            : new Dictionary<T, int>();

			ICollection<T> supersetCollection = GetCollection(superset);
			ICollection<T> subsetCollection = GetCollection(subset);

			// Build index of all elements in the superset
			foreach (T element in supersetCollection)
			{
				if (!index.ContainsKey(element))
				{
					index.Add(element, 0);
				}
			}

			// Check if all elements in subset exist in superset
			foreach (T element in subsetCollection)
			{
				if (!index.ContainsKey(element))
				{
					return false;
				}
			}

			return true;
		}

		public static int CompareAscending([CanBeNull] object xValue,
		                                   [CanBeNull] object yValue)
		{
			// If values implement IComparer
			if (xValue is IComparable comparable)
			{
				return comparable.CompareTo(yValue);
			}

			if (Equals(xValue, yValue))
			{
				// values don't implement IComparer but are equivalent
				return 0;
			}

			if (xValue == null)
			{
				return -1;
			}

			if (yValue == null)
			{
				return 1;
			}

			// Values don't implement IComparer and are not equivalent, so compare as string values
			return string.Compare(xValue.ToString(),
			                      yValue.ToString(),
			                      StringComparison.CurrentCulture);
		}

		public static int CompareDescending(object xValue, object yValue)
		{
			// Return result adjusted for ascending or descending sort order ie
			// multiplied by 1 for ascending or -1 for descending
			return CompareAscending(xValue, yValue) * -1;
		}

		/// <summary>
		/// Gets the most frequent values in an enumerable
		/// </summary>
		/// <param name="values">The values.</param>
		/// <returns></returns>
		public static IEnumerable<T> GetMostFrequentValues<T>(
			[NotNull] IEnumerable<T> values)
		{
			int nullCount;
			IDictionary<T, int> frequencies = GetFrequencies(values, out nullCount);

			var highestCount = 0;
			foreach (KeyValuePair<T, int> pair in frequencies)
			{
				if (pair.Value > highestCount)
				{
					highestCount = pair.Value;
				}
			}

			highestCount = Math.Max(highestCount, nullCount);

			foreach (KeyValuePair<T, int> pair in frequencies)
			{
				if (pair.Value == highestCount)
				{
					yield return pair.Key;
				}
			}

			if (nullCount > 0 && nullCount == highestCount)
			{
				yield return default;
			}
		}

		[NotNull]
		public static IDictionary<T, int> GetFrequencies<T>([NotNull] IEnumerable<T> values)
		{
			return GetFrequencies(values, out int _);
		}

		[NotNull]
		public static IDictionary<T, int> GetFrequencies<T>([NotNull] IEnumerable<T> values,
		                                                    out int nullCount)
		{
			nullCount = 0;
			var result = new Dictionary<T, int>();

			foreach (T value in values)
			{
				if (Equals(value, null))
				{
					nullCount++;
					continue;
				}

				if (result.ContainsKey(value))
				{
					result[value]++;
				}
				else
				{
					result.Add(value, 1);
				}
			}

			return result;
		}

		[NotNull]
		public static IList<T> GetDuplicates<T>([NotNull] IEnumerable<T> values,
		                                        out int nullCount)
		{
			return GetDuplicates(values, out nullCount, null);
		}

		[NotNull]
		public static IList<T> GetDuplicates<T>([NotNull] IEnumerable<T> values,
		                                        out int nullCount,
		                                        [CanBeNull] IEqualityComparer<T> comparer)
		{
			Assert.ArgumentNotNull(values, nameof(values));

			var set = new HashSet<T>(comparer);

			var result = new List<T>();
			nullCount = 0;

			bool isStruct = typeof(T).IsValueType;

			foreach (T value in values)
			{
				// ReSharper disable CompareNonConstrainedGenericWithNull
				if (! isStruct && value == null)
				{
					// ReSharper restore CompareNonConstrainedGenericWithNull
					nullCount++;
				}
				else
				{
					if (! set.Add(value))
					{
						// already in set
						result.Add(value);
					}
				}
			}

			return result;
		}

		public static TSource MaxElement<TSource, TKey>([NotNull] this IEnumerable<TSource> source,
		                                                [NotNull] Func<TSource, TKey> selector)
			where TKey : IComparable<TKey>
		{
			Assert.ArgumentNotNull(source, nameof(source));
			Assert.ArgumentNotNull(selector, nameof(selector));

			TSource maxElement = default(TSource);
			TKey maxOrdinal = default(TKey);
			var gotAny = false;

			foreach (TSource element in source)
			{
				if (gotAny)
				{
					TKey ordinal = selector(element);
					if (ordinal.CompareTo(maxOrdinal) > 0)
					{
						maxElement = element;
						maxOrdinal = ordinal;
					}
				}
				else
				{
					maxElement = element;
					maxOrdinal = selector(element);
					gotAny = true;
				}
			}

			if (gotAny)
			{
				return maxElement;
			}

			throw new InvalidOperationException("Sequence contains no elements");
		}

		public static TSource MaxElementOrDefault<TSource, TKey>(
			[NotNull] this IEnumerable<TSource> source,
			[NotNull] Func<TSource, TKey> selector)
			where TKey : IComparable<TKey>
		{
			Assert.ArgumentNotNull(source, nameof(source));
			Assert.ArgumentNotNull(selector, nameof(selector));

			TSource maxElement = default(TSource);
			TKey maxOrdinal = default(TKey);
			var gotAny = false;

			foreach (TSource element in source)
			{
				if (gotAny)
				{
					TKey ordinal = selector(element);
					if (ordinal.CompareTo(maxOrdinal) > 0)
					{
						maxElement = element;
						maxOrdinal = ordinal;
					}
				}
				else
				{
					maxElement = element;
					maxOrdinal = selector(element);
					gotAny = true;
				}
			}

			return gotAny ? maxElement : default;
		}

		public static TSource MinElement<TSource, TKey>([NotNull] this IEnumerable<TSource> source,
		                                                [NotNull] Func<TSource, TKey> selector)
			where TKey : IComparable<TKey>
		{
			Assert.ArgumentNotNull(source, nameof(source));
			Assert.ArgumentNotNull(selector, nameof(selector));

			bool gotAny = TryGetMinElement(source, selector, out TSource minElement);

			if (gotAny)
			{
				return minElement;
			}

			throw new InvalidOperationException("Sequence contains no elements");
		}

		public static TSource MinElementOrDefault<TSource, TKey>(
			[NotNull] this IEnumerable<TSource> source,
			[NotNull] Func<TSource, TKey> selector)
			where TKey : IComparable<TKey>
		{
			Assert.ArgumentNotNull(source, nameof(source));
			Assert.ArgumentNotNull(selector, nameof(selector));

			bool gotAny = TryGetMinElement(source, selector, out TSource minElement);

			return gotAny ? minElement : default;
		}

		[NotNull]
		public static IEnumerable<List<T>> Partition<T>(
			[NotNull] IEnumerable<T> items,
			int partitionCount)
		{
			Assert.ArgumentNotNull(items, nameof(items));
			Assert.ArgumentCondition(partitionCount > 0, "Invalid partition count: {0}",
			                         partitionCount);

			List<T> list = items.ToList();

			// calculate bounds as double to distribute rounding error
			double lowerDouble = 0;
			var lowerIndex = 0;
			double partitionSize = list.Count / (double) partitionCount;

			for (var partitionIndex = 0; partitionIndex < partitionCount; partitionIndex++)
			{
				double upperDouble = lowerDouble + partitionSize;

				int upperIndex = (int) Math.Round(upperDouble) - 1;
				int count = upperIndex - lowerIndex + 1;
				if (count > 0)
				{
					yield return list.GetRange(lowerIndex, count);
				}

				lowerDouble = upperDouble;
				lowerIndex = upperIndex + 1;
			}
		}

		[NotNull]
		public static IEnumerable<KeyValuePair<T, T>> GetAllTuples<T>(IEnumerable<T> objs)
			where T : class
		{
			var objList = new List<T>(objs);

			for (var i = 0; i < objList.Count - 1; i++)
			{
				for (int j = i + 1; j < objList.Count; j++)
				{
					yield return new KeyValuePair<T, T>(objList[i], objList[j]);
				}
			}
		}

		/// <summary>
		/// A more efficient way to get the distinct elements compared to group by.
		/// Author: Jon Skeet (https://stackoverflow.com/questions/489258/linqs-distinct-on-a-particular-property)
		/// NOTE: It is included in .NET 6
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <typeparam name="TKey"></typeparam>
		/// <param name="source"></param>
		/// <param name="keySelector"></param>
		/// <returns></returns>
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
			IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			HashSet<TKey> seenKeys = new HashSet<TKey>();

			foreach (TSource element in source)
			{
				if (seenKeys.Add(keySelector(element)))
				{
					yield return element;
				}
			}
		}

		public static void AddToValueList<TKey, TValue>(
			[NotNull] IDictionary<TKey, List<TValue>> dictionary,
			[NotNull] TKey key,
			[NotNull] TValue value)
		{
			List<TValue> list;
			if (! dictionary.TryGetValue(key, out list))
			{
				list = new List<TValue>();
				dictionary.Add(key, list);
			}

			list.Add(value);
		}

		public static void AddToValueList<TKey, TValue>(
			[NotNull] IDictionary<TKey, IList<TValue>> dictionary,
			[NotNull] TKey key,
			[NotNull] TValue value)
		{
			IList<TValue> list;
			if (! dictionary.TryGetValue(key, out list))
			{
				list = new List<TValue>();
				dictionary.Add(key, list);
			}

			list.Add(value);
		}

		#region Non-public

		private static int GetFirstIndex<T>(ICollection<ListItem<T>> listItems)
		{
			Assert.ArgumentNotNull(listItems, nameof(listItems));

			if (listItems.Count == 0)
			{
				return -1;
			}

			int minimumIndex = int.MaxValue;

			foreach (ListItem<T> listItem in listItems)
			{
				if (listItem.ListIndex < minimumIndex)
				{
					minimumIndex = listItem.ListIndex;
				}
			}

			return minimumIndex;
		}

		private static void SortListItems<T>(List<ListItem<T>> listItems, bool reverse)
		{
			Assert.ArgumentNotNull(listItems, nameof(listItems));

			listItems.Sort(delegate(
				               ListItem<T> listItem1, ListItem<T> listItem2)
			               {
				               int reverseFactor = reverse
					                                   ? -1
					                                   : 1;

				               return
					               listItem1.ListIndex.CompareTo(listItem2.ListIndex) *
					               reverseFactor;
			               });
		}

		private static List<ListItem<T>> GetListItems<T>(IList<T> list,
		                                                 IEnumerable<T> items)
		{
			Assert.ArgumentNotNull(list, nameof(list));
			Assert.ArgumentNotNull(items, nameof(items));

			var listItems = new List<ListItem<T>>();

			foreach (T item in items)
			{
				int currentIndex = list.IndexOf(item);

				if (currentIndex < 0)
				{
					throw new ArgumentOutOfRangeException(
						nameof(items),
						string.Format(
							"Item {0} is not part of quality specification", item));
				}

				listItems.Add(new ListItem<T>(item, currentIndex));
			}

			return listItems;
		}

		private static bool TryGetMinElement<TSource, TKey>(IEnumerable<TSource> source,
		                                                    Func<TSource, TKey> selector,
		                                                    out TSource minElement)
			where TKey : IComparable<TKey>
		{
			minElement = default;
			TKey minOrdinal = default(TKey);
			var gotAny = false;

			foreach (TSource element in source)
			{
				if (gotAny)
				{
					TKey ordinal = selector(element);
					if (ordinal.CompareTo(minOrdinal) < 0)
					{
						minElement = element;
						minOrdinal = ordinal;
					}
				}
				else
				{
					minElement = element;
					minOrdinal = selector(element);
					gotAny = true;
				}
			}

			return gotAny;
		}

		#endregion

		#region Nested types

		/// <summary>
		/// Helper class for moving list elements (stores the original list index per item)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		private class ListItem<T>
		{
			public ListItem(T item, int listIndex)
			{
				Assert.ArgumentNotNull(item, nameof(item));
				Assert.True(listIndex >= 0, "Invalid list index: {0}", listIndex);

				Item = item;
				ListIndex = listIndex;
			}

			public T Item { get; }

			public int ListIndex { get; }

			public override string ToString()
			{
				return string.Format("{0} (index:{1})", Item, ListIndex);
			}
		}

		#endregion

		[NotNull]
		public static ICollection<T> GetCollection<T>([NotNull] IEnumerable<T> enumerable)
		{
			return enumerable as ICollection<T> ?? enumerable.ToList();
		}

		[NotNull]
		public static List<T> Zip<T>([NotNull] IEnumerable<T> first,
		                             [NotNull] IEnumerable<T> second)
		{
			Assert.ArgumentNotNull(first, nameof(first));
			Assert.ArgumentNotNull(second, nameof(second));

			var result = new List<T>();

			using (IEnumerator<T> e1 = first.GetEnumerator())
			{
				using (IEnumerator<T> e2 = second.GetEnumerator())
				{
					while (e1.MoveNext() && e2.MoveNext())
					{
						result.AddRange(new[] { e1.Current, e2.Current });
					}

					while (e1.MoveNext())
					{
						result.Add(e1.Current);
					}

					while (e2.MoveNext())
					{
						result.Add(e2.Current);
					}
				}
			}

			return result;
		}

		[NotNull]
		public static List<T> Zip<T>([NotNull] IEnumerable<T> first,
		                             [NotNull] IEnumerable<T> second,
		                             [NotNull] IEnumerable<T> third)
		{
			Assert.ArgumentNotNull(first, nameof(first));
			Assert.ArgumentNotNull(second, nameof(second));
			Assert.ArgumentNotNull(third, nameof(third));

			var result = new List<T>();

			using (IEnumerator<T> e1 = first.GetEnumerator())
			{
				using (IEnumerator<T> e2 = second.GetEnumerator())
				{
					using (IEnumerator<T> e3 = third.GetEnumerator())
					{
						while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
						{
							result.AddRange(new[] { e1.Current, e2.Current, e3.Current });
						}

						while (e1.MoveNext())
						{
							result.Add(e1.Current);
						}

						while (e2.MoveNext())
						{
							result.Add(e2.Current);
						}

						while (e3.MoveNext())
						{
							result.Add(e3.Current);
						}
					}
				}
			}

			return result;
		}
	}
}
