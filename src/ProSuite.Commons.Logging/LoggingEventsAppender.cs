using System;
using log4net.Appender;
using log4net.Core;

namespace ProSuite.Commons.Logging
{
	public class LoggingEventArgs : EventArgs
	{
		public readonly LoggingEventItem LogItem;

		public LoggingEventArgs(LoggingEvent logEvent)
		{
			LogItem = new LoggingEventItem(logEvent);
		}
	}

	public sealed class LoggingEventsAppender : AppenderSkeleton
	{
		// TODO temporary static event handler - unsubscribe!!!
		public static event EventHandler<LoggingEventArgs> OnNewLogMessage;

		protected override void Append(LoggingEvent loggingEvent)
		{
			OnNewLogMessage?.Invoke(this, new LoggingEventArgs(loggingEvent));
		}
	}
}
