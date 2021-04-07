using System;

namespace ProSuite.Commons.Logging.Inspector
{
	public class LogInspectorEntry
	{
		public LogInspectorEntry(LogInspectorLevel level, DateTime timeStamp, string loggerName,
		                         string message, Exception exception = null,
		                         ILoggingContext context = null)
		{
			Level = level;
			TimeStamp = timeStamp;
			LoggerName = loggerName;
			Message = message;
			Context = context;
			Exception = exception;
		}

		public LogInspectorLevel Level { get; }
		public DateTime TimeStamp { get; }
		public string LoggerName { get; }
		public string Message { get; }
		public Exception Exception { get; }
		public ILoggingContext Context { get; }
		public long SequenceNumber { get; set; }
	}
}
