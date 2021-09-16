using log4net.Core;
using System;

namespace ProSuite.Commons.Logging
{
	public enum LogType
	{
		Info,
		Debug,
		Error,
		Verbose,
		Warn,
		Other
	}

	public class LoggingEventItem
	{
		public LoggingEventItem(LoggingEvent logEvent)
		{
			Type = Log4NetUtils.MapLogLevelToLogType(logEvent.Level);
			Time = logEvent.TimeStamp;
			Message = logEvent.RenderedMessage;
			Source = logEvent.LoggerName;
			ExceptionMessage = logEvent.ExceptionObject?.StackTrace ?? string.Empty;
		}

		public LogType Type { get;}
		public DateTime Time { get;}
		public string Message { get;}
		public string Source { get; }
		public string ExceptionMessage { get; }
	}
}
