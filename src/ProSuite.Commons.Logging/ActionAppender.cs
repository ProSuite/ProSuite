using System;
using log4net.Appender;
using log4net.Core;

namespace ProSuite.Commons.Logging
{
	public class ActionAppender : AppenderSkeleton
	{
		private readonly Action<LoggingEvent> _logAction;

		public ActionAppender(Action<LoggingEvent> logAction)
		{
			_logAction = logAction;
		}

		protected override void Append(LoggingEvent loggingEvent)
		{
			_logAction(loggingEvent);
		}
	}
}
