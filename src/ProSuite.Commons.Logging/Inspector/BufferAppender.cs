using System;
using System.Collections.Generic;
using System.Linq;
using log4net.Appender;
using log4net.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging.Inspector
{
	public class BufferAppender : AppenderSkeleton
	{
		private readonly CaptureBuffer _buffer;
		private FixFlags _fixFlags;
		private int _errorCount;
		private int _droppedErrors;
		private int _warnCount;
		private int _droppedWarns;
		private int _infoCount;
		private int _droppedInfos;
		private int _debugCount;
		private int _droppedDebugs;
		private readonly object _sync;

		public BufferAppender(int capacity)
		{
			_buffer = new CaptureBuffer(capacity);
			_fixFlags = FixFlags.All;
			_sync = new object();
		}

		public int Capacity
		{
			get { lock(_sync) return _buffer.Capacity; }
		}

		public FixFlags Fix
		{
			get => _fixFlags;
			set => _fixFlags = value;
		}

		[NotNull]
		public LogSnapshot Snapshot(ILoggingContextInfo contextInfo)
		{
			lock (_sync)
			{
				var capturedEvents = _buffer.GetSnapshot()
				                            .Select(e => ConvertEvent(e, contextInfo))
				                            .ToArray();

				return new LogSnapshot(Capacity, capturedEvents)
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

		private static LogInspectorEvent ConvertEvent([NotNull] LoggingEvent loggingEvent,
		                                              ILoggingContextInfo contextInfo = null)
		{
			var level = ConvertLevel(loggingEvent.Level);
			var message = loggingEvent.RenderedMessage ?? Convert.ToString(loggingEvent.MessageObject);
			ILoggingContext context = contextInfo?.GetLoggingContext(loggingEvent);
			return new LogInspectorEvent(level, loggingEvent.TimeStamp,
			                             loggingEvent.LoggerName, message,
			                             loggingEvent.ExceptionObject, context);
		}

		private static LogInspectorLevel ConvertLevel(Level level)
		{
			if (level == null) return LogInspectorLevel.All;
			if (level < Level.Debug) return LogInspectorLevel.All;
			if (level < Level.Info) return LogInspectorLevel.Debug;
			if (level < Level.Warn) return LogInspectorLevel.Info;
			if (level < Level.Error) return LogInspectorLevel.Warn;
			if (level < Level.Fatal) return LogInspectorLevel.Error;
			return LogInspectorLevel.Off;
		}

		#region AppenderSkeleton

		protected override void Append(LoggingEvent loggingEvent)
		{
			if (loggingEvent == null)
			{
				return; // silently ignore the unexpected
			}

			// Fix volatile data in the event prior to storing.
			// There's a fix more/less versus memory tradeoff.
			loggingEvent.Fix = Fix;

			lock (_sync)
			{
				UpdateCounts(loggingEvent.Level);

				_buffer.Append(loggingEvent, out Level droppedLevel);

				UpdateDrops(droppedLevel);
			}
		}

		private void UpdateCounts(Level level)
		{
			if (level == null)
			{
				return;
			}

			if (level < Level.Info)
			{
				_debugCount += 1;
			}
			else if (level < Level.Warn)
			{
				_infoCount += 1;
			}
			else if (level < Level.Error)
			{
				_warnCount += 1;
			}
			else // level >= Error
			{
				_errorCount += 1;
			}
		}

		private void UpdateDrops(Level level)
		{
			if (level == null)
			{
				return;
			}

			if (level < Level.Info)
			{
				_droppedDebugs += 1;
			}
			else if (level < Level.Warn)
			{
				_droppedInfos += 1;
			}
			else if (level < Level.Error)
			{
				_droppedWarns += 1;
			}
			else // level >= Error
			{
				_droppedErrors += 1;
			}
		}

		#endregion

		#region Nested type: CaptureBuffer

		private class CaptureBuffer : PriorityQueue<BufferItem>
		{
			private long _sequenceNumber;

			public CaptureBuffer(int capacity) : base(capacity)
			{
				_sequenceNumber = 0;
			}

			protected override bool Priority(BufferItem a, BufferItem b)
			{
				var aa = a.LoggingEvent;
				var bb = b.LoggingEvent;

				if (aa == null || bb == null)
					throw new ArgumentNullException();

				if (aa.Level == null && bb.Level != null)
					return true;
				if (aa.Level == null || bb.Level == null)
					return false;

				if (aa.Level < bb.Level)
					return true;
				return aa.Level == bb.Level && a.SequenceNumber < b.SequenceNumber;
			}

			public void Append(LoggingEvent loggingEvent, out Level droppedLevel)
			{
				var overflow = AddWithOverflow(new BufferItem(_sequenceNumber++, loggingEvent));

				droppedLevel = overflow.LoggingEvent?.Level;
			}

			public IEnumerable<LoggingEvent> GetSnapshot()
			{
				var array = new BufferItem[Count];
				CopyAll(array, 0);
				return array.OrderBy(item => item.SequenceNumber)
				            .Select(item => item.LoggingEvent);
			}
		}

		/// <summary>
		/// This is simply a <see cref="LoggingEvent"/> together with
		/// a sequence number. We need the sequence number for sorting
		/// events into the order of occurrence, because the resolution
		/// of the <see cref="log4net.Core.LoggingEvent.TimeStamp"/> is
		/// too coarse and our buffering/sorting is not stable.
		/// </summary>
		private readonly struct BufferItem
		{
			public readonly long SequenceNumber;
			public readonly LoggingEvent LoggingEvent;

			public BufferItem(long sequenceNumber, LoggingEvent loggingEvent)
			{
				SequenceNumber = sequenceNumber;
				LoggingEvent = loggingEvent;
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
			/// <param name="capacity">If negative, this indicates an unbounded
			/// queue; otherwise, this is the queue's fixed capacity.</param>
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
				DownHeap(1); // fix heap

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
