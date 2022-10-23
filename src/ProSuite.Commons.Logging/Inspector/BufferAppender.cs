using System;
using System.Collections.Generic;
using log4net.Appender;
using log4net.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging.Inspector
{
	public class BufferAppender : AppenderSkeleton
	{
		private readonly CaptureBuffer _buffer;
		private int _errorCount;
		private int _droppedErrors;
		private int _warnCount;
		private int _droppedWarns;
		private int _infoCount;
		private int _droppedInfos;
		private int _debugCount;
		private int _droppedDebugs;
		private readonly object _sync;

		public BufferAppender(int capacity, DateTime? startTime = null)
		{
			_buffer = new CaptureBuffer(capacity);
			_sync = new object();
			StartTime = startTime ?? DateTime.Now;
		}

		public int Capacity
		{
			get
			{
				lock (_sync) return _buffer.Capacity;
			}
		}

		public DateTime StartTime { get; }

		[NotNull]
		public LogSnapshot Snapshot()
		{
			lock (_sync)
			{
				var capturedEvents = _buffer.GetSnapshot();

				return new LogSnapshot(Capacity, StartTime, capturedEvents)
				       {
					       ErrorCount = _errorCount,
					       DroppedErrors = _droppedErrors,
					       WarnCount = _warnCount,
					       DroppedWarns = _droppedWarns,
					       InfoCount = _infoCount,
					       DroppedInfos = _droppedInfos,
					       DebugCount = _debugCount,
					       DroppedDebugs = _droppedDebugs
				       };
			}
		}

		#region AppenderSkeleton

		protected override void Append(LoggingEvent loggingEvent)
		{
			if (loggingEvent == null)
			{
				return; // silently ignore the unexpected
			}

			lock (_sync)
			{
				// Convert to our own type so we do not have to fix
				// and hold any (large) LoggingEvent instances.

				var entry = LogInspectorUtils.ConvertEvent(loggingEvent);

				UpdateCounts(entry.Level);

				var overflow = _buffer.Append(entry);

				if (overflow != null)
				{
					UpdateDrops(overflow.Level);
				}
			}
		}

		#endregion

		#region Private methods

		private void UpdateCounts(LogInspectorLevel level)
		{
			if (level < LogInspectorLevel.Info)
			{
				_debugCount += 1;
			}
			else if (level < LogInspectorLevel.Warn)
			{
				_infoCount += 1;
			}
			else if (level < LogInspectorLevel.Error)
			{
				_warnCount += 1;
			}
			else
			{
				_errorCount += 1;
			}
		}

		private void UpdateDrops(LogInspectorLevel level)
		{
			if (level < LogInspectorLevel.Info)
			{
				_droppedDebugs += 1;
			}
			else if (level < LogInspectorLevel.Warn)
			{
				_droppedInfos += 1;
			}
			else if (level < LogInspectorLevel.Error)
			{
				_droppedWarns += 1;
			}
			else
			{
				_droppedErrors += 1;
			}
		}

		#endregion

		#region Nested type: CaptureBuffer

		private class CaptureBuffer : PriorityQueue<LogInspectorEntry>
		{
			private long _sequenceNumber;

			public CaptureBuffer(int capacity) : base(capacity)
			{
				_sequenceNumber = 0;
			}

			protected override bool Priority(LogInspectorEntry a, LogInspectorEntry b)
			{
				if (a == null || b == null)
					throw new ArgumentNullException();

				if (a.Level < b.Level)
					return true;
				return a.Level == b.Level && a.SequenceNumber < b.SequenceNumber;
			}

			public LogInspectorEntry Append(LogInspectorEntry entry)
			{
				entry.SequenceNumber = _sequenceNumber++;
				return AddWithOverflow(entry);
			}

			public LogInspectorEntry[] GetSnapshot()
			{
				var array = new LogInspectorEntry[Count];

				CopyAll(array, 0);

				Array.Sort(array, new SequenceComparer());

				return array;
			}

			private class SequenceComparer : IComparer<LogInspectorEntry>
			{
				public int Compare(LogInspectorEntry x, LogInspectorEntry y)
				{
					if (x == null || y == null) throw new ArgumentNullException();
					if (x.SequenceNumber > y.SequenceNumber) return 1;
					if (x.SequenceNumber < y.SequenceNumber) return -1;
					return 0;
				}
			}
		}

		#endregion

		#region PriorityQueue

		// This is a much simplified copy of the priority queue in
		// ProSuite.Commons.Collections. Code duplication weights less
		// than the additional dependencies: either from Commons.Logging
		// to Commons (for the priority queue), or from ProSuite.Commons
		// to log4net (for LoggingEvent etc).

		private abstract class PriorityQueue<T>
		{
			private int _count;
			private readonly T[] _heap;
			private readonly int _capacity;

			/// <summary>
			/// Create a priority queue with the given <paramref name="capacity"/>.
			/// Use a negative <paramref name="capacity"/> for an unbounded queue.
			/// An unbounded queue will grow as needed.
			/// </summary>
			/// <param name="capacity">queue capacity, at least 1</param>
			protected PriorityQueue(int capacity)
			{
				if (capacity < 1)
					throw new ArgumentOutOfRangeException(nameof(capacity), "must be at least 1");

				_heap = new T[1 + capacity];

				// The heap is in [1..count], _heap[0] is not used.

				_count = 0;
				_capacity = capacity;
			}

			/// <summary>
			/// Return <c>true</c> if <paramref name="a"/> has higher priority
			/// than <paramref name="b"/>; otherwise, return <c>false</c>.
			/// </summary>
			protected abstract bool Priority(T a, T b);

			public int Capacity => _capacity;

			public int Count => _count;

			/// <summary>
			/// Add an item to the priority queue in O(log Count) time.
			/// If the queue is full, make room by removing the top item,
			/// which my be the one to be inserted.
			/// </summary>
			/// <returns>The item removed, default(T) if none was removed.</returns>
			protected T AddWithOverflow(T item)
			{
				if (_count < _capacity)
				{
					_count += 1;
					_heap[_count] = item;

					UpHeap(_count);
					return default;
				}

				if (_capacity < 1 || Priority(item, _heap[1]))
				{
					return item;
				}

				var top = _heap[1];

				_heap[1] = item;
				DownHeap(1);

				return top;
			}

			protected void CopyAll(T[] array, int arrayIndex)
			{
				Array.Copy(_heap, 1, array, arrayIndex, _count);
			}

			#region Private methods

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

			#endregion
		}

		#endregion
	}
}
