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

	public class AppenderDelegate : AppenderSkeleton
	{
		public event EventHandler<LoggingEventArgs> OnNewLogMessage;

		protected override void Append(LoggingEvent loggingEvent)
		{
			OnNewLogMessage?.Invoke(this, new LoggingEventArgs(loggingEvent));
		}
	}
}
