using log4net.Appender;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProSuite.Commons.Logging
{
	public class LoggingEventArgs : EventArgs
	{
		public LoggingEvent logMessage;

		public LoggingEventArgs(LoggingEvent logMessage)
		{
			this.logMessage = logMessage;
		}
	}

	public class LoggingEventsAppender : AppenderSkeleton
	{
		// TODO temporary static event handler !!!
		public static event EventHandler<LoggingEventArgs> OnNewLogMessage;

		private static readonly IMsg _msg = new Msg(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected override void Append(LoggingEvent loggingEvent)
		{
			OnNewLogMessage?.Invoke(this, new LoggingEventArgs(loggingEvent));
		}
	}
}
