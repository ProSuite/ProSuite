using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.Test.Collections
{
	[TestFixture]
	public class CollectionUtilsTest
	{
		[Test]
		public void CanPartitionEven()
		{
			AssertCorrectPartitioning(new[] {"a", "b", "c", "d"}, 2);
		}

		[Test]
		public void CanPartitionOdd()
		{
			AssertCorrectPartitioning(new[] {"a", "b", "c"}, 2);
		}

		[Test]
		public void CanPartitionOdd2()
		{
			AssertCorrectPartitioning(new[] {"a", "b", "c", "d", "e", "f", "g"}, 5);
		}

		[Test]
		public void CanPartitionCountLargerCollectionCount()
		{
			AssertCorrectPartitioning(new[] {"a", "b", "c", "d"}, 100);
		}

		[Test]
		public void CanPartitionToSinglePartition()
		{
			AssertCorrectPartitioning(new[] {"a", "b", "c", "d"}, 1);
		}

		[Test]
		public void CanPartitionEmptyCollection()
		{
			AssertCorrectPartitioning(new List<string>(), 1);
		}

		[Test]
		public void CanPartitionUniqueItem()
		{
			AssertCorrectPartitioning(new[] {"a"}, 1);
		}

		[Test]
		public void CanConcat()
		{
			IList<int> listOne = CreateList(0, 1, 2);
			IList<int> listTwo = CreateList(3, 4, 5);
			IList<int>[] listArray = {listOne, listTwo};

			List<int> result = CollectionUtils.Concat(listArray);
			Assert.AreEqual(6, result.Count);
			Assert.AreEqual(2, result[2]);
			Assert.AreEqual(3, result[3]);
		}

		[Test]
		public void CanSplitEven()
		{
			IList<int> longList = CreateList(1, 2, 3, 4, 5, 6, 7, 8, 9);

			IList<IList<int>> splitLists =
				new List<IList<int>>(CollectionUtils.Split(longList, 3));

			Assert.AreEqual(3, splitLists.Count);

			Assert.AreEqual(3, splitLists[2].Count);
			Assert.AreEqual(9, splitLists[2][2]);

			Assert.AreEqual(3, splitLists[1].Count);
			Assert.AreEqual(4, splitLists[1][0]);
		}

		[Test]
		public void CanSplitOdd()
		{
			IList<int> longList = CreateList(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

			IList<IList<int>> splitLists =
				new List<IList<int>>(CollectionUtils.Split(longList, 3));

			Assert.AreEqual(4, splitLists.Count);

			Assert.AreEqual(1, splitLists[3].Count);
			Assert.AreEqual(10, splitLists[3][0]);

			Assert.AreEqual(3, splitLists[1].Count);
			Assert.AreEqual(4, splitLists[1][0]);
		}

		[Test]
		public void CanSplitBelowMax()
		{
			IList<int> longList = CreateList(1, 2, 3, 4, 5, 6, 7, 8, 9);

			IList<IList<int>> splitLists =
				new List<IList<int>>(CollectionUtils.Split(longList, 10));

			Assert.AreEqual(1, splitLists.Count);

			Assert.AreEqual(9, splitLists[0][8]);
		}

		[Test]
		public void CanSplitEqualMax()
		{
			IList<int> longList = CreateList(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

			IList<IList<int>> splitLists =
				new List<IList<int>>(CollectionUtils.Split(longList, 10));

			Assert.AreEqual(1, splitLists.Count);

			Assert.AreEqual(10, splitLists[0][9]);
		}

		[Test]
		public void CanMoveBack()
		{
			IList<int> list = CreateList(1, 2, 3, 4);

			CollectionUtils.MoveTo(list, 3, 1);

			Assert.AreEqual(4, list.Count);

			Assert.AreEqual(1, list[0]);
			Assert.AreEqual(3, list[1]);
			Assert.AreEqual(2, list[2]);
			Assert.AreEqual(4, list[3]);
		}

		[Test]
		public void CanMoveForward()
		{
			IList<int> list = CreateList(1, 2, 3, 4);

			CollectionUtils.MoveTo(list, 2, 2);

			Assert.AreEqual(4, list.Count);

			Assert.AreEqual(1, list[0]);
			Assert.AreEqual(3, list[1]);
			Assert.AreEqual(2, list[2]);
			Assert.AreEqual(4, list[3]);
		}

		[Test]
		public void CanMoveInSingleList()
		{
			IList<int> list = CreateList(1);

			CollectionUtils.MoveTo(list, 1, 0);

			Assert.AreEqual(1, list.Count);

			Assert.AreEqual(1, list[0]);
		}

		[Test]
		public void CanMoveToEnd()
		{
			IList<int> list = CreateList(1, 2, 3, 4);

			CollectionUtils.MoveTo(list, 1, 3);

			Assert.AreEqual(4, list.Count);

			Assert.AreEqual(2, list[0]);
			Assert.AreEqual(3, list[1]);
			Assert.AreEqual(4, list[2]);
			Assert.AreEqual(1, list[3]);
		}

		[Test]
		public void CanMoveToSelf()
		{
			IList<int> list = CreateList(1, 2, 3, 4);

			CollectionUtils.MoveTo(list, 3, 2);

			Assert.AreEqual(4, list.Count);

			Assert.AreEqual(1, list[0]);
			Assert.AreEqual(2, list[1]);
			Assert.AreEqual(3, list[2]);
			Assert.AreEqual(4, list[3]);
		}

		[Test]
		public void CanMoveToStart()
		{
			IList<int> list = CreateList(1, 2, 3, 4);

			CollectionUtils.MoveTo(list, 4, 0);

			Assert.AreEqual(4, list.Count);

			Assert.AreEqual(4, list[0]);
			Assert.AreEqual(1, list[1]);
			Assert.AreEqual(2, list[2]);
			Assert.AreEqual(3, list[3]);
		}

		[Test]
		public void CannotRotateEmptyArray()
		{
			int[] array = { };
			Assert.Throws<ArgumentOutOfRangeException>(
				() => CollectionUtils.Rotate(array, 3));
		}

		[Test]
		public void CanRotate()
		{
			int[] array = {1, 2, 3, 4, 5};
			// Rotate left 3 steps:
			int[] array1 = {4, 5, 1, 2, 3};
			// Rotate right 1 step:
			int[] array2 = {3, 4, 5, 1, 2};
			// Rotate left 4 steps:
			int[] array3 = {2, 3, 4, 5, 1};
			// Rotate right 2 steps:
			int[] array4 = {5, 1, 2, 3, 4};
			// Rotate subarray [1..3] left 1 step:
			int[] array5 = {5, 2, 3, 1, 4};
			// Rotate subarray [3..4] right 4 steps:
			int[] array6 = {5, 2, 3, 1, 4};

			CollectionUtils.Rotate(array, 3);
			Assert.IsTrue(SameElementsSameOrder(array, array1));

			CollectionUtils.Rotate(array, -1);
			Assert.IsTrue(SameElementsSameOrder(array, array2));

			CollectionUtils.Rotate(array, 9);
			Assert.IsTrue(SameElementsSameOrder(array, array3));

			CollectionUtils.Rotate(array, -12);
			Assert.IsTrue(SameElementsSameOrder(array, array4));

			// Rotate array[1..3], leave array[0] and array[4]:
			CollectionUtils.Rotate(array, 1, 1, 3);
			Assert.IsTrue(SameElementsSameOrder(array, array5));

			CollectionUtils.Rotate(array, 4, 3, 2);
			Assert.IsTrue(SameElementsSameOrder(array, array6));
		}

		[Test]
		public void CanSubArray()
		{
			int[] array = {1, 2, 3, 4, 5};

			var result = new List<int>();
			result.AddRange(CollectionUtils.SubArray(array, -1, 9));
			Assert.AreEqual(5, result.Count);
			Assert.AreEqual(1, result[0]);

			result.Clear();
			result.AddRange(CollectionUtils.SubArray(array, 3, 1));
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(4, result[0]);

			result.Clear();
			result.AddRange(CollectionUtils.SubArray(array, 3, 9));
			Assert.AreEqual(2, result.Count);
			Assert.AreEqual(5, result[1]);
		}

		[Test]
		public void CanTestHaveSameElements()
		{
			// Note: repetitions in the lists are intended!
			IList<int> listOne = CreateList(1, 2, 3, 4, 4);
			IList<int> listTwo = CreateList(1, 3, 4, 1, 2);
			IList<int> listThree = CreateList(2, 2, 3, 3);

			Assert.IsTrue(CollectionUtils.HaveSameElements(listOne, listTwo));

			Assert.IsTrue(CollectionUtils.HaveSameElements(listTwo, listOne));

			Assert.IsFalse(CollectionUtils.HaveSameElements(listTwo, listThree));
		}

		[Test]
		public void IsSubsetOfWorks()
		{
			IList<int> listOne = CreateList(1, 2, 3, 4);
			IList<int> listTwo = CreateList(1, 2, 3, 4, 5);
			IList<int> listThree = CreateList(1, 2, 2, 1, 3);

			Assert.IsTrue(CollectionUtils.IsSubsetOf(listOne, listOne));
			Assert.IsTrue(CollectionUtils.IsSubsetOf(listOne, listTwo));
			Assert.IsFalse(CollectionUtils.IsSubsetOf(listTwo, listOne));

			Assert.IsTrue(CollectionUtils.IsSubsetOf(listThree, listOne));
		}

		[Test]
		public void CanGetFrequenciesInt()
		{
			var values = new[] {1, 2, 3, 2, 3, 7, 3};
			IDictionary<int, int> frequencies = CollectionUtils.GetFrequencies(values);

			Assert.AreEqual(1, frequencies[1]);
			Assert.AreEqual(2, frequencies[2]);
			Assert.AreEqual(3, frequencies[3]);
			Assert.AreEqual(1, frequencies[7]);
		}

		[Test]
		public void CanGetFrequenciesString()
		{
			var values = new[] {"a", "b", "c", "b", null, "x", null};
			int nullCount;
			IDictionary<string, int> frequencies = CollectionUtils.GetFrequencies(values,
				out nullCount);

			Assert.AreEqual(2, nullCount);
			Assert.AreEqual(1, frequencies["a"]);
			Assert.AreEqual(2, frequencies["b"]);
			Assert.AreEqual(1, frequencies["c"]);
			Assert.AreEqual(1, frequencies["x"]);
		}

		[Test]
		public void CanGetFrequenciesObject()
		{
			var values = new object[] {"a", 1, "c", 1.11, null, "c", null};
			int nullCount;
			IDictionary<object, int> frequencies = CollectionUtils.GetFrequencies(values,
				out nullCount);

			Assert.AreEqual(2, nullCount);
			Assert.AreEqual(1, frequencies["a"]);
			Assert.AreEqual(1, frequencies[1]);
			Assert.AreEqual(2, frequencies["c"]);
			Assert.AreEqual(1, frequencies[1.11]);
		}

		[Test]
		public void CanGetMostFrequentValues()
		{
			var values = new object[] {"a", 1, "c", 1.11, null, "c", null};
			var result = new List<object>(CollectionUtils.GetMostFrequentValues(values));

			Assert.AreEqual(2, result.Count);
			Assert.IsTrue(result.Contains("c"));
			Assert.IsTrue(result.Contains(null));
		}

		[Test]
		public void CanGetDuplicates()
		{
			var values = new object[] {"a", 1, "c", 1.11, null, "c", null};
			int nullCount;
			var duplicates =
				new List<object>(CollectionUtils.GetDuplicates(values, out nullCount));

			Assert.AreEqual(1, duplicates.Count);
			Assert.IsTrue(duplicates.Contains("c"));
			Assert.AreEqual(2, nullCount);
		}

		[Test]
		public void CanGetDuplicatesForEmptyList()
		{
			var values = new object[] { };
			int nullCount;
			var duplicates =
				new List<object>(CollectionUtils.GetDuplicates(values, out nullCount));

			Assert.AreEqual(0, duplicates.Count);
			Assert.AreEqual(0, nullCount);
		}

		[Test]
		public void CanMaxElement()
		{
			// MaxElement() extends IEnumerable<T>
			// and is defined in class CollectionUtils.

			string[] seq = {"foo", "bar", "linq", "quux"};
			var empty = Array.Empty<string>();

			Assert.AreEqual("linq", seq.MaxElement(s => s.Length));

			// Notice that the first max wins (this ambiguity is probably
			// the reason why out-of-the-box LINQ has no such operator).

			try
			{
				empty.MaxElement(s => s.Length);
				Assert.Fail("Expected exception on empty sequence");
			}
			catch (Exception ex)
			{
				Console.WriteLine(@"Expected: {0}", ex.Message);
			}
		}

		[Test]
		public void CanMaxElementOrDefault()
		{
			// MaxElementOrDefault() extends IEnumerable<T>
			// and is defined in class CollectionUtils.

			var empty = Array.Empty<string>();
			string[] seq = {"foo", "bar", "linq", "quux"};

			Assert.IsNull(empty.MaxElementOrDefault(s => s.Length));

			// For non-empty sequences, MaxElementOrDefault
			// must exhibit the same behaviour as MaxElement.

			Assert.AreEqual("linq", seq.MaxElementOrDefault(s => s.Length));
		}

		[Test]
		public void CanZipTwoCollections()
		{
			int[] array = {1, 2, 3, 4, 5};

			int[] array1 = {10, 20, 30, 40, 50};

			List<int> result = CollectionUtils.Zip(array, array1);

			for (int index = 0; index < array.Length; index++)
			{
				Assert.AreEqual(array[index], result[index + index]);
			}

			for (int index = 0; index < array1.Length; index++)
			{
				Assert.AreEqual(array1[index], result[index + index + 1]);
			}

			int[] array2 = {10, 20, 30, 40, 50, 99};

			result.Clear();

			result = CollectionUtils.Zip(array, array2);

			Assert.AreEqual(array.Length + array2.Length, result.Count);
		}

		[Test]
		public void CanZipThreeCollections()
		{
			int[] array = {1, 2, 3, 4, 5};

			int[] array1 = {10, 20, 30, 40, 50};

			int[] array2 = {100, 200, 300, 400, 500};

			List<int> result = CollectionUtils.Zip(array, array1, array2);

			for (int index = 0; index < array.Length; index++)
			{
				Assert.AreEqual(array[index], result[index + index + index]);
			}

			for (int index = 0; index < array1.Length; index++)
			{
				Assert.AreEqual(array1[index], result[index + index + index + 1]);
			}

			for (int index = 0; index < array2.Length; index++)
			{
				Assert.AreEqual(array2[index], result[index + index + index + 2]);
			}

			int[] array4 = {10, 20, 30, 40, 50, 99};

			result.Clear();

			result = CollectionUtils.Zip(array, array4, array2);

			Assert.AreEqual(array.Length + array4.Length + array2.Length, result.Count);
		}

		private static void AssertCorrectPartitioning(
			[NotNull] ICollection<string> collection, int partitionCount)
		{
			List<List<string>> partitions =
				CollectionUtils.Partition(collection, partitionCount)
				               .ToList();

			var concatenated = new List<string>();
			int index = 0;
			foreach (List<string> partition in partitions)
			{
				Console.WriteLine("Partition {0}: {1}", index,
				                  StringUtils.Concatenate(partition, ","));
				concatenated.AddRange(partition);
				index++;
			}

			Assert.AreEqual(collection.Count <= partitionCount
				                ? collection.Count
				                : partitionCount, partitions.Count);
			Assert.AreEqual(collection.Count, concatenated.Count);
			Assert.True(collection.SequenceEqual(concatenated));
		}

		[NotNull]
		private static IList<T> CreateList<T>(params T[] values)
		{
			return new List<T>(values);
		}

		private static bool SameElementsSameOrder<T>([NotNull] T[] array1,
		                                             [NotNull] T[] array2)
		{
			int count = array1.Length;

			if (count != array2.Length)
			{
				return false;
			}

			for (int i = 0; i < count; i++)
			{
				if (! Equals(array1[i], array2[i]))
				{
					return false;
				}
			}

			return true;
		}
	}
}
