using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Collections;

namespace ProSuite.Commons.Test.Collections
{
	[TestFixture]
	public class PriorityQueueTest
	{
		[Test]
		public void CanCreateSubclass()
		{
			var pq1 = new TestPriorityQueue<char>();
			Assert.AreEqual(TestPriorityQueue<char>.Unbounded, pq1.Capacity);
			Assert.AreEqual(0, pq1.Count);

			var pq2 = new TestPriorityQueue<char>(5);
			Assert.AreEqual(5, pq2.Capacity);
			Assert.AreEqual(0, pq2.Count);
		}

		[Test]
		public void CanFixedCapacity0()
		{
			const int capacity = 0;
			var pq = new TestPriorityQueue<char>(capacity);

			Assert.AreEqual(0, pq.Capacity);
			Assert.AreEqual(0, pq.Count);

			var ex1 = Assert.Catch(() => pq.Top());
			Console.WriteLine("Expected: {0}", ex1.Message);

			var ex2 = Assert.Catch(() => pq.Pop());
			Console.WriteLine("Expected: {0}", ex2.Message);

			// With capacity zero, all additions immediately overflow:
			Assert.AreEqual('z', pq.AddWithOverflow('z'));
			Assert.AreEqual('a', pq.AddWithOverflow('a'));
			var ex3 = Assert.Catch(() => pq.Add('b'));
			Console.WriteLine("Expected: {0}", ex3.Message);
		}

		[Test]
		public void CanFixedCapacity6()
		{
			// Capacity 6: incomplete binary tree
			const int capacity = 6;
			var pq = new TestPriorityQueue<char>(capacity);

			Assert.AreEqual(6, pq.Capacity);
			Assert.AreEqual(0, pq.Count);

			pq.Add('e');
			Assert.AreEqual('e', pq.Top());
			pq.Add('b');
			Assert.AreEqual('b', pq.Top());
			pq.Add('c');
			Assert.AreEqual('b', pq.Top());
			pq.Add('a');
			Assert.AreEqual('a', pq.Top());
			pq.Add('f');
			Assert.AreEqual('a', pq.Top());
			pq.Add('d');
			Assert.AreEqual('a', pq.Top());

			Assert.AreEqual(6, pq.Count);

			Assert.AreEqual('a', pq.AddWithOverflow('x'));
			Assert.AreEqual('a', pq.AddWithOverflow('a'));

			Assert.AreEqual(6, pq.Count);

			Assert.AreEqual('b', pq.Top());
			Assert.AreEqual('b', pq.Pop());
			Assert.AreEqual('c', pq.Pop());
			Assert.AreEqual('d', pq.Pop());
			Assert.AreEqual('e', pq.Pop());
			Assert.AreEqual('f', pq.Pop());
			Assert.AreEqual('x', pq.Pop());

			Assert.AreEqual(0, pq.Count);

			var ex = Assert.Catch(() => pq.Pop());
			Console.WriteLine("Expected: {0}", ex.Message);
		}

		[Test]
		public void CanFixedCapacity7()
		{
			// Capacity 7: complete binary tree
			const int capacity = 7;
			var pq = new TestPriorityQueue<char>(capacity);

			Assert.AreEqual(7, pq.Capacity);
			Assert.AreEqual(0, pq.Count);

			pq.Add('e');
			Assert.AreEqual('e', pq.Top());
			pq.Add('g');
			Assert.AreEqual('e', pq.Top());
			pq.Add('b');
			Assert.AreEqual('b', pq.Top());
			pq.Add('c');
			Assert.AreEqual('b', pq.Top());
			pq.Add('a');
			Assert.AreEqual('a', pq.Top());
			pq.Add('f');
			Assert.AreEqual('a', pq.Top());
			pq.Add('d');
			Assert.AreEqual('a', pq.Top());

			Assert.AreEqual(7, pq.Count);
			Assert.AreEqual('a', pq.Top());

			Assert.AreEqual('a', pq.AddWithOverflow('x')); // 'a' drops out
			Assert.AreEqual('a', pq.AddWithOverflow('a')); // 'a' never gets in...

			Assert.AreEqual(7, pq.Count);

			Assert.AreEqual('b', pq.Top());
			Assert.AreEqual('b', pq.Pop());
			Assert.AreEqual('c', pq.Pop());
			Assert.AreEqual('d', pq.Pop());
			Assert.AreEqual('e', pq.Pop());
			Assert.AreEqual('f', pq.Pop());
			Assert.AreEqual('g', pq.Pop());
			Assert.AreEqual('x', pq.Top());
			Assert.AreEqual('x', pq.Pop());

			Assert.AreEqual(0, pq.Count);

			var ex = Assert.Catch(() => pq.Pop());
			Console.WriteLine("Expected: {0}", ex.Message);
		}

		[Test]
		public void CanCopeWithTies()
		{
			var pq = new TestPriorityQueue<char>(5);

			pq.Add('x');
			pq.Add('y');

			Assert.AreEqual(2, pq.Count);

			pq.Add('x'); // again

			Assert.AreEqual(3, pq.Count);

			Assert.AreEqual('x', pq.Pop());
			Assert.AreEqual('x', pq.Pop()); // again!
			Assert.AreEqual('y', pq.Pop());

			Assert.AreEqual(0, pq.Count);
		}

		[Test]
		public void CanUnboundedCapacity()
		{
			const int count = 1000; // will trigger reallocations

			var pq = new TestPriorityQueue<int>();

			var items = Enumerable.Range(1, count).ToList();
			Shuffle(items, new Random(1234));

			foreach (var i in items)
			{
				pq.Add(i);
			}

			Assert.AreEqual(count, pq.Count);

			var popped = new List<int>();
			while (pq.Count > 0)
			{
				popped.Add(pq.Pop());
			}

			Assert.True(popped.SequenceEqual(Enumerable.Range(1, count)));
		}

		[Test]
		public void CannotPopEmpty()
		{
			var pq = new TestPriorityQueue<char>();

			var ex1 = Assert.Catch<InvalidOperationException>(() => pq.Pop());
			Console.WriteLine("Expected: {0}", ex1.Message);

			pq.Add('a');
			pq.Pop();

			var ex2 = Assert.Catch<InvalidOperationException>(() => pq.Pop());
			Console.WriteLine("Expected: {0}", ex2.Message);
		}

		[Test]
		public void CannotTopEmpty()
		{
			var pq = new TestPriorityQueue<char>();

			pq.Add('a');
			pq.Pop();

			var ex = Assert.Catch<InvalidOperationException>(() => pq.Top());
			Console.WriteLine("Expected: {0}", ex.Message);
		}

		[Test]
		public void CannotAddFull()
		{
			const int capacity = 5;
			var pq = new TestPriorityQueue<char>(capacity);

			pq.Add('a');
			pq.Add('b');
			pq.Add('c');
			pq.Add('d');
			pq.Add('e');

			Assert.AreEqual(capacity, pq.Count); // full

			var ex = Assert.Catch<InvalidOperationException>(() => pq.Add('x'));
			Console.WriteLine("Expected: {0}", ex.Message);
		}

		[Test]
		public void CanEnumerateFixedQueue()
		{
			var pq1 = new TestPriorityQueue<char>(5); // fixed capacity

			pq1.AddWithOverflow('f');
			pq1.AddWithOverflow('u');
			pq1.AddWithOverflow('b');
			pq1.AddWithOverflow('a');
			pq1.AddWithOverflow('r');

			var enumerator = pq1.GetEnumerator();

			var list1 = Iterate(enumerator);
			Assert.AreEqual("abfru", new string(list1.OrderBy(c => c).ToArray()));

			pq1.Clear();
			pq1.Add('f');
			pq1.Add('o');
			pq1.Add('o');

			enumerator.Reset();

			var list2 = Iterate(enumerator);
			Assert.AreEqual("foo", new string(list2.OrderBy(c => c).ToArray()));

			enumerator.Dispose();
		}

		[Test]
		public void CanEnumerateGrowingQueue()
		{
			var pq = new TestPriorityQueue<char>(); // unbounded

			pq.Add('f');
			pq.Add('u');
			pq.Add('b');
			pq.Add('a');
			pq.Add('r');

			var enumerator = pq.GetEnumerator();

			var list1 = Iterate(enumerator);
			Assert.AreEqual("abfru", new string(list1.OrderBy(c => c).ToArray()));

			list1.Clear();
			pq.Pop(); // modify collection
			enumerator.Reset();

			var list2 = Iterate(enumerator);
			Assert.AreEqual("bfru", new string(list2.OrderBy(c => c).ToArray()));

			enumerator.Dispose();
		}

		[Test]
		public void CanEnumerateFixedEmptyQueue()
		{
			// ReSharper disable once CollectionNeverUpdated.Local
			var pq = new TestPriorityQueue<char>(0); // fixed capacity
			var enumerator = pq.GetEnumerator();

			IterateEmpty(enumerator);

			pq.AddWithOverflow('x');
			Assert.AreEqual(0, pq.Count); // still empty
			enumerator.Reset();

			IterateEmpty(enumerator);

			enumerator.Dispose();
		}

		[Test]
		public void CanEnumerateGrowingEmptyQueue()
		{
			// ReSharper disable once CollectionNeverUpdated.Local
			var pq = new TestPriorityQueue<char>(); // unbounded
			var enumerator = pq.GetEnumerator();

			IterateEmpty(enumerator);

			pq.Add('x');
			pq.Pop(); // empty again
			enumerator.Reset();

			IterateEmpty(enumerator);

			enumerator.Dispose();
		}

		[Test(Description = "Collection must not change while being enumerated")]
		public void CannotEnumerateChangingQueue()
		{
			const string items = "hello";
			var pq = new TestPriorityQueue<char>(items.Length + 1);

			foreach (var c in items)
			{
				pq.Add(c);
			}

			var iter = pq.GetEnumerator();

			Assert.True(iter.MoveNext());
			pq.Add('x'); // modify (will fill up capacity)
			var ex1 = Assert.Catch<InvalidOperationException>(() => iter.MoveNext());
			Console.WriteLine("Expected after Add: {0}", ex1.Message);

			iter.Reset();

			Assert.True(iter.MoveNext());
			pq.AddWithOverflow('y'); // modify (will overflow)
			var ex2 = Assert.Catch<InvalidOperationException>(() => iter.MoveNext());
			Console.WriteLine("Expected after AddWithOverflow: {0}", ex2.Message);

			iter.Reset();

			Assert.True(iter.MoveNext());
			pq.Pop(); // modify
			var ex3 = Assert.Catch<InvalidOperationException>(() => iter.MoveNext());
			Console.WriteLine("Expected after Pop: {0}", ex3.Message);

			iter.Reset();

			Assert.True(iter.MoveNext());
			pq.Clear(); // modify
			var ex4 = Assert.Catch<InvalidOperationException>(() => iter.MoveNext());
			Console.WriteLine("Expected after Clear: {0}", ex4.Message);

			iter.Dispose();
		}

		[Test]
		public void CanCopyTo()
		{
			var pq = new TestPriorityQueue<char>();
			foreach (char c in "hello")
			{
				pq.Add(c);
			}

			var array = new char[pq.Count];
			pq.CopyTo(array, 0);

			// The items in the array are in "heap array order",
			// but that's an undocumented implementation detail;
			// officially the ordering is undefined, so sort:

			Array.Sort(array);
			Assert.True(array.SequenceEqual("ehllo".ToCharArray()));
		}

		[Test]
		public void CanContains()
		{
			var pq = new TestPriorityQueue<char>();
			foreach (char c in "hello")
			{
				pq.Add(c);
			}

			Assert.True(pq.Contains('h'));
			Assert.True(pq.Contains('e'));
			Assert.True(pq.Contains('l'));
			Assert.True(pq.Contains('o'));
			Assert.False(pq.Contains('x'));
		}

		[Test]
		public void CanRemove()
		{
			var pq = new TestPriorityQueue<char>();
			foreach (char c in "abcdefghijklmnopqrstuvwxyz")
			{
				pq.Add(c);
			}

			Assert.IsFalse(pq.Remove('$')); // no such item
			Assert.AreEqual(26, pq.Count);

			// Last item: easy to remove
			Assert.IsTrue(pq.Remove('z'));
			Assert.AreEqual(25, pq.Count);
			Assert.AreEqual('a', pq.Top());
			Assert.IsFalse(pq.Contains('z'));

			// Remove a bottom row item:
			Assert.IsTrue(pq.Remove('w'));
			Assert.AreEqual(24, pq.Count);
			Assert.AreEqual('a', pq.Top());
			Assert.IsFalse(pq.Contains('w'));

			// Remove an inner item:
			Assert.IsTrue(pq.Remove('e'));
			Assert.AreEqual(23, pq.Count);
			Assert.AreEqual('a', pq.Top());
			Assert.IsFalse(pq.Contains('e'));

			// Remove the root item:
			Assert.IsTrue(pq.Remove('a'));
			Assert.AreEqual(22, pq.Count);
			Assert.AreEqual('b', pq.Top());
			Assert.IsFalse(pq.Contains('a'));

			// Remove returns false if not found:
			Assert.IsFalse(pq.Remove('z'));
			Assert.IsFalse(pq.Remove('w'));
			Assert.IsFalse(pq.Remove('e'));
			Assert.IsFalse(pq.Remove('a'));

			// Pop remaining items and verify order:
			Assert.AreEqual('b', pq.Pop());
			Assert.AreEqual('c', pq.Pop());
			Assert.AreEqual('d', pq.Pop());
			Assert.AreEqual('f', pq.Pop());
			Assert.AreEqual('g', pq.Pop());
			Assert.AreEqual('h', pq.Pop());
			Assert.AreEqual('i', pq.Pop());
			Assert.AreEqual('j', pq.Pop());
			Assert.AreEqual('k', pq.Pop());
			Assert.AreEqual('l', pq.Pop());
			Assert.AreEqual('m', pq.Pop());
			Assert.AreEqual('n', pq.Pop());
			Assert.AreEqual('o', pq.Pop());
			Assert.AreEqual('p', pq.Pop());
			Assert.AreEqual('q', pq.Pop());
			Assert.AreEqual('r', pq.Pop());
			Assert.AreEqual('s', pq.Pop());
			Assert.AreEqual('t', pq.Pop());
			Assert.AreEqual('u', pq.Pop());
			Assert.AreEqual('v', pq.Pop());
			Assert.AreEqual('x', pq.Pop());
			Assert.AreEqual('y', pq.Pop());

			Assert.AreEqual(0, pq.Count);
		}

		[Test]
		[Category(TestCategory.Performance)]
		public void PerformanceTest()
		{
			const int capacity = 100 * 1000;
			const int count = 100 * capacity;
			var random = new Random(12345);

			var startTime1 = DateTime.Now;
			var pq1 = new TestPriorityQueue<int>(capacity);

			for (int i = 0; i < count; i++)
			{
				int value = random.Next();
				pq1.AddWithOverflow(value);
			}

			Assert.AreEqual(Math.Min(capacity, count), pq1.Count);

			while (pq1.Count > 0)
			{
				pq1.Pop();
			}

			var elapsed1 = DateTime.Now - startTime1;

			Console.WriteLine("Capacity={0:N0} Count={1:N0} AddWithOverflow/Pop Elapsed={2}",
			                  capacity, count, elapsed1);

			var startTime2 = DateTime.Now;
			var pq2 = new TestPriorityQueue<int>(); // unbounded

			for (int i = 0; i < count; i++)
			{
				int value = random.Next();
				pq2.Add(value);
			}

			Assert.AreEqual(count, pq2.Count);

			while (pq2.Count > 0)
			{
				pq2.Pop();
			}

			var elapsed2 = DateTime.Now - startTime2;

			Console.WriteLine("Capacity=unbounded Count={0:N0} Add/Pop Elapsed={1}",
			                  count, elapsed2);

			Assert.Less(elapsed1, TimeSpan.FromSeconds(1), "Too slow");
			Assert.Less(elapsed2, TimeSpan.FromSeconds(12), "Too slow");
		}

		#region Test Utilities

		private class TestPriorityQueue<T> : PriorityQueue<T>
		{
			private readonly IComparer<T> _comparer;

			public TestPriorityQueue(int capacity = -1) : base(capacity)
			{
				_comparer = Comparer<T>.Default;
			}

			protected override bool Priority(T a, T b)
			{
				return _comparer.Compare(a, b) < 0;
			}
		}

		private static IList<T> Iterate<T>(IEnumerator<T> enumerator)
		{
			// Note: to test pq's enumerator, do NOT call pq.ToList(),
			// as this extension method bypasses the enumerator (it
			// seems to use ICollector.CopyTo(), if available).

			var list = new List<T>();

			Assert.AreEqual(default(T), enumerator.Current);

			while (enumerator.MoveNext())
			{
				list.Add(enumerator.Current);
			}

			// Want Current at default(T) after MoveNext returned false;
			// and MoveNext must not change its mind once it returned false:
			Assert.AreEqual(default(T), enumerator.Current);
			Assert.IsFalse(enumerator.MoveNext());

			return list;
		}

		private static void IterateEmpty<T>(IEnumerator<T> enumerator)
		{
			// Want Current at default(T) before first MoveNext
			// and after each MoveNext that returns false; once
			// MoveNext returned false, subsequent calls must also
			// return false (unless there's a Reset in-between):
			Assert.AreEqual(default(T), enumerator.Current);
			Assert.False(enumerator.MoveNext());
			Assert.AreEqual(default(T), enumerator.Current);
			Assert.False(enumerator.MoveNext());
			Assert.AreEqual(default(T), enumerator.Current);
		}

		private static void Shuffle<T>(IList<T> list, Random random)
		{
			// This is Fisher-Yates shuffle. It's also in ListUtils,
			// but here I prefer no dependency on ListUtils.
			int n = list.Count;
			while (n > 1)
			{
				int k = random.Next(n); // 0 <= k < n
				n -= 1;
				var temp = list[k];
				list[k] = list[n];
				list[n] = temp;
			}
		}

		#endregion
	}
}
