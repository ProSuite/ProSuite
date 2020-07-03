using System;
using System.Collections;
using System.Collections.Generic;

namespace ProSuite.Commons.Collections
{
	/// <summary>
	/// A priority queue, implemented using a heap.
	/// <see cref="Add"/> and <see cref="Pop"/> are O(log N)
	/// operations, when N is <see cref="Count"/>, the number
	/// of items in the queue. <see cref="Top"/> is an O(1)
	/// operation. Most other operations, especially those
	/// required by <see cref="ICollection{T}"/> tend to
	/// require O(N) time.
	/// <para/>
	/// There are two modes of operation: fixed-capacity and
	/// unbounded. In fixed-capacity mode, <see cref="Add"/>
	/// throws an exception if the queue is full; in unbounded
	/// mode it enlarges the internal heap array.
	/// <para/>
	/// <see cref="AddWithOverflow"/> is only useful in
	/// fixed-capacity mode: if the queue is full, the top
	/// item will be removed to make room for the new item
	/// (the overall operation requires O(log N) time).
	/// <para/>
	/// To use this priority queue, create a subclass and
	/// override the <see cref="Priority"/> method.
	/// </summary>
	public abstract class PriorityQueue<T> : ICollection<T>
	{
		private int _count;
		private int _version;
		private T[] _heap;
		private readonly int _capacity;
		public const int Unbounded = int.MaxValue;

		/// <summary>
		/// Create a priority queue with the given <paramref name="capacity"/>.
		/// Use a negative <paramref name="capacity"/> for an unbounded queue.
		/// An unbounded queue will grow as needed.
		/// </summary>
		/// <param name="capacity">If negative, this indicates an unbounded
		/// queue; otherwise, this is the queue's fixed capacity.</param>
		protected PriorityQueue(int capacity = -1)
		{
			const int initialLength = 7; // 3 complete levels
			_heap = new T[1 + initialLength]; // _heap[0] unused

			// The heap is in [1..count], _heap[0] is not used.
			// If the heap were to start at index 0, the parent/child
			// arithmetic would be slightly more complicated/expensive.

			if (capacity < 0)
			{
				capacity = Unbounded;
			}

			_count = 0;
			_version = 0;
			_capacity = capacity;
		}

		/// <summary>
		/// Return <c>true</c> if <paramref name="a"/> has higher priority
		/// than <paramref name="b"/>; otherwise, return <c>false</c>.
		/// </summary>
		protected abstract bool Priority(T a, T b);

		/// <summary>
		/// The number of items this priority queue can hold at most.
		/// </summary>
		public int Capacity
		{
			get { return _capacity; }
			// TODO set? easy to enlarge, but how to shrink?
		}

		/// <summary>
		/// The number of items currently in the priority queue.
		/// </summary>
		public int Count
		{
			get { return _count; }
		}

		/// <summary>
		/// Add an item to the PriorityQueue in O(log Count) time.
		/// If the queue is full, throw an InvalidOperationException.
		/// </summary>
		public void Add(T item)
		{
			if (_count >= _capacity)
			{
				throw new InvalidOperationException("Queue is full");
			}

			if (_count + 1 >= _heap.Length)
			{
				_heap = GrowHeap(_heap, _capacity);
			}

			_count += 1;
			_heap[_count] = item;
			_version += 1; // collection modified

			UpHeap(_count);
		}

		/// <summary>
		/// Add an item to the priority queue in O(log Count) time.
		/// If the queue is full, make room by removing the top item,
		/// which my be the one to be inserted.
		/// </summary>
		/// <returns>The item removed, default(T) if none was removed.</returns>
		public T AddWithOverflow(T item)
		{
			if (_count < _capacity)
			{
				Add(item);
				return default(T);
			}

			if (_capacity < 1 || Priority(item, _heap[1]))
			{
				return item;
			}

			var top = _heap[1];

			_heap[1] = item;
			_version += 1; // collection modified
			DownHeap(1); // fix heap

			return top;
		}

		/// <summary>
		/// Get and remove the top item in O(log Count) time.
		/// If the queue is empty, throw an InvalidOperationException.
		/// </summary>
		public T Pop()
		{
			if (_count <= 0)
			{
				throw new InvalidOperationException("Queue is empty");
			}

			var top = _heap[1]; // save the top item
			_heap[1] = _heap[_count]; // move last to first
			_heap[_count] = default(T); // free reference to item
			_count--; // one item less in queue
			_version += 1; // collection modified

			DownHeap(1);

			return top;
		}

		/// <summary>
		/// Get the top item in constant time.
		/// If the queue is empty, throw an InvalidOperationException.
		/// </summary>
		public T Top()
		{
			if (_count <= 0)
			{
				throw new InvalidOperationException("Queue is empty");
			}

			return _heap[1];
		}

		/// <summary>
		/// Call this when the top item's priority has changed.
		/// It fixes the heap structure in O(log Count) time.
		/// To fix the heap if the priority of any other item
		/// has changed, remove the item and add it again.
		/// </summary>
		/// <remarks>
		/// It's more efficient to do
		/// <code>pq.Top().ChangePriority(); pq.TopChanged();</code>
		/// than it is to do
		/// <code>top = pq.Pop(); top.ChangePriority(); pq.Add(top);</code>
		/// </remarks>
		/// <returns>The new top item.</returns>
		public T TopChanged()
		{
			DownHeap(1);

			return _heap[1];
		}

		public override string ToString()
		{
			return Capacity == Unbounded
				       ? string.Format("Count = {0}, Capacity = unbounded", Count)
				       : string.Format("Count = {0}, Capacity = {1}", Count, Capacity);
		}

		#region Non-public methods

		private void UpHeap(int k)
		{
			var item = _heap[k]; // save bottom item

			int parent = k / 2;

			while (parent > 0 && Priority(item, _heap[parent]))
			{
				_heap[k] = _heap[parent];

				k = parent;
				parent = k / 2;
			}

			_heap[k] = item; // restore saved item
		}

		private void DownHeap(int k)
		{
			// Inside the while loop, there are two calls to the comparer.
			// There is an alternative implementation with only one call
			// inside the loop and an UpHeap operation after the loop.

			var item = _heap[k]; // save top item

			while (k <= _count / 2)
			{
				// Pick smaller child:
				int child = k + k; // left child
				if (child < _count && Priority(_heap[child + 1], _heap[child]))
				{
					child++; // right child
				}

				if (Priority(item, _heap[child])) break;

				_heap[k] = _heap[child]; // shift child up

				k = child;
			}

			_heap[k] = item; // restore saved item
		}

		private static T[] GrowHeap(T[] heap, int max)
		{
			long oldSize = heap.Length - 1; // omit empty slot
			int newSize = (int) Math.Min(2 * oldSize + 1, max);
			var newHeap = new T[1 + newSize]; // 1st slot unused
			Array.Copy(heap, 1, newHeap, 1, oldSize);
			return newHeap;
		}

		#endregion

		#region ICollection implementation

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Contains(T item) // O(N)
		{
			for (int i = 1; i <= _count; i++)
			{
				if (Equals(_heap[i], item))
				{
					return true;
				}
			}

			return false;
		}

		public void Clear()
		{
			if (_count > 0)
			{
				// Overwrite with default(T) to free reference to items!
				Array.Clear(_heap, 0, Math.Min(1 + _count, _heap.Length));

				_count = 0; // no more items
				_version += 1; // collection modified
			}
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			Array.Copy(_heap, 1, array, arrayIndex, _count);
		}

		public bool Remove(T item)
		{
			int index;
			for (index = 1; index <= _count; ++index)
			{
				if (Equals(_heap[index], item)) break;
			}

			if (index > _count)
			{
				return false; // no such item
			}

			// Replace removed item with last item and fix the heap:

			_heap[index] = _heap[_count];
			_heap[_count] = default(T); // release reference

			_count -= 1; // one less entry
			_version += 1; // collection modified

			if (index <= _count)
			{
				DownHeap(index); // fix heap
			}
			// else: removed last item, nothing to fix

			return true; // removed
		}

		#endregion

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new PriorityQueueEnumerator(this);
		}

		#endregion

		#region PriorityQueueEnumerator

		private class PriorityQueueEnumerator : IEnumerator<T>
		{
			private readonly PriorityQueue<T> _pq;
			private int _version;
			private int _index;
			private T _current;

			public PriorityQueueEnumerator(PriorityQueue<T> pq)
			{
				_pq = pq;

				Reset();
			}

			#region IEnumerator implementation

			public void Reset()
			{
				_version = _pq._version;
				_index = 1; // rewind
				_current = default(T);
			}

			public bool MoveNext()
			{
				if (_version != _pq._version)
				{
					throw new InvalidOperationException("Collection changed while enumerating");
				}

				if (_index <= _pq._count)
				{
					_current = _pq._heap[_index];
					_index += 1;
					return true;
				}

				_current = default(T);
				return false;
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}

			public T Current
			{
				get { return _current; }
			}

			public void Dispose()
			{
				// Position at end, so MoveNext would return false:

				_index = _pq._count + 1;
				_current = default(T);
			}

			#endregion
		}

		#endregion
	}
}