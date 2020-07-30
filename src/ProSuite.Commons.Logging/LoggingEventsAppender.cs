using log4net.Appender;
using log4net.Core;
using System;

namespace ProSuite.Commons.Logging
{
	public class LoggingEventArgs : EventArgs
	{
		public LoggingEventItem logItem;

		public LoggingEventArgs(LoggingEvent logEvent)
		{
			logItem = new LoggingEventItem(logEvent);
		}
	}

	public class LoggingEventsAppender : AppenderSkeleton
	{
		// TODO temporary static event handler - unsubscribe!!!
		public static event EventHandler<LoggingEventArgs> OnNewLogMessage;

		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected override void Append(LoggingEvent loggingEvent)
		{
			OnNewLogMessage?.Invoke(this, new LoggingEventArgs(loggingEvent));
		}
	}
}
