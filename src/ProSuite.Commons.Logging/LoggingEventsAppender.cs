using System;
using System.Collections.Generic;
using log4net.Appender;
using log4net.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging
{
	public class LoggingEventArgs : EventArgs
	{
		private LoggingEventItem _logItem;

		/// <summary>
		/// The original log4net event. Exposed in addition to <see cref="LogItem"/> so that
		/// consumers based on <see cref="log4net.Core.LoggingEvent"/> (such as the WinForms
		/// LogWindowControl) can be fed from the same event without a second appender.
		/// </summary>
		public readonly LoggingEvent LoggingEvent;

		/// <summary>
		/// Monotonically increasing sequence number assigned to this event by
		/// <see cref="LoggingEventsAppender"/>. Lets a consumer that replays the buffered
		/// history (see <see cref="LoggingEventsAppender.SnapshotRecentEvents"/>) tell which
		/// live events it has already shown, so history and live events do not overlap.
		/// </summary>
		public readonly long Sequence;

		public LoggingEventArgs(LoggingEvent logEvent) : this(logEvent, 0) { }

		public LoggingEventArgs(LoggingEvent logEvent, long sequence)
		{
			LoggingEvent = logEvent;
			Sequence = sequence;
		}

		/// <summary>
		/// A projection of <see cref="LoggingEvent"/> used by the WPF log pane. Built lazily on
		/// first access: consumers that feed directly from <see cref="LoggingEvent"/> (such as
		/// the WinForms LogWindowControl) never allocate it. The underlying event is fixed by the
		/// appender before this event is raised, so building the item later is safe.
		/// </summary>
		public LoggingEventItem LogItem =>
			_logItem ?? (_logItem = new LoggingEventItem(LoggingEvent));
	}

	[UsedImplicitly]
	public sealed class LoggingEventsAppender : AppenderSkeleton
	{
		// TODO temporary static event handler - unsubscribe!!!
		public static event EventHandler<LoggingEventArgs> OnNewLogMessage;

		/// <summary>
		/// Maximum number of recent events kept for replay into a log window that is opened
		/// after those events were logged.
		/// </summary>
		private const int _maxBufferedEvents = 5000;

		private static readonly object _bufferLock = new object();
		private static readonly Queue<BufferedEvent> _recentEvents = new Queue<BufferedEvent>();
		private static long _sequence;

		/// <summary>
		/// The lock guarding the recent-events buffer and the sequence counter. A consumer
		/// replaying the history holds this while taking its snapshot so that no event is
		/// produced concurrently, which is what makes the history/live hand-off exact.
		/// </summary>
		public static object SyncRoot => _bufferLock;

		protected override void Append(LoggingEvent loggingEvent)
		{
			// Fix the volatile data (rendered message, exception, thread name, ...) so the
			// buffered event still renders correctly when it is replayed later, possibly on
			// another thread. Deliberately Partial, not All: it excludes the costly LocationInfo
			// stack walk, which the log window does not display.
			loggingEvent.Fix = FixFlags.Partial;

			long sequence;
			lock (_bufferLock)
			{
				sequence = ++_sequence;

				_recentEvents.Enqueue(new BufferedEvent(sequence, loggingEvent));

				while (_recentEvents.Count > _maxBufferedEvents)
				{
					_recentEvents.Dequeue();
				}
			}

			OnNewLogMessage?.Invoke(this, new LoggingEventArgs(loggingEvent, sequence));
		}

		/// <summary>
		/// Returns the events currently held in the replay buffer (oldest first) together with
		/// the highest sequence number assigned so far. Call this inside a
		/// <c>lock (<see cref="SyncRoot"/>)</c> so the snapshot and the sequence high-water
		/// mark are consistent with the live event stream.
		/// </summary>
		/// <param name="highWaterSequence">The highest sequence number assigned to any event so
		/// far; events with a greater sequence number are guaranteed not to be in the snapshot.</param>
		public static IReadOnlyList<LoggingEvent> SnapshotRecentEvents(out long highWaterSequence)
		{
			highWaterSequence = _sequence;

			var result = new List<LoggingEvent>(_recentEvents.Count);
			foreach (BufferedEvent bufferedEvent in _recentEvents)
			{
				result.Add(bufferedEvent.LoggingEvent);
			}

			return result;
		}

		private readonly struct BufferedEvent
		{
			public BufferedEvent(long sequence, LoggingEvent loggingEvent)
			{
				Sequence = sequence;
				LoggingEvent = loggingEvent;
			}

			public long Sequence { get; }
			public LoggingEvent LoggingEvent { get; }
		}
	}
}
