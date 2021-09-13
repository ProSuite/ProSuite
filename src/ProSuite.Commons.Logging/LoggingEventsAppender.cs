using log4net.Appender;
using log4net.Core;
using System;

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

	public class LoggingEventsAppender : AppenderSkeleton
	{
		// TODO temporary static event handler - unsubscribe!!!
		public static event EventHandler<LoggingEventArgs> OnNewLogMessage;

		protected override void Append(LoggingEvent loggingEvent)
		{
			OnNewLogMessage?.Invoke(this, new LoggingEventArgs(loggingEvent));
		}
	}
}
