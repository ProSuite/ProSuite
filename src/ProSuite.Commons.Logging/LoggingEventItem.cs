using log4net.Core;
using System;

namespace ProSuite.Commons.Logging
{

	public enum LogType
	{
		Warning,
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
			Message = $"{logEvent.LoggerName}: {logEvent.RenderedMessage}";
		}

		public LogType Type { get; set; }

		public DateTime Time { get; set; }
		public string Message { get; set; }
	}
}
