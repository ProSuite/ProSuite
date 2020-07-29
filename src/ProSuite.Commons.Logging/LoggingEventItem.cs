using log4net.Core;
using System;

namespace ProSuite.Commons.Logging
{
	public enum LogType
	{
		Warning,
		Info,
		Debug,
		Error
	}

	public class LoggingEventItem
	{

		public LoggingEventItem(LoggingEvent logEvent)
		{
			Type = LogType.Info;//  Info logEvent.Level.DisplayName;
			Time = logEvent.TimeStamp;
			Message = logEvent.RenderedMessage;
			// TODO how to provide hyperlink?
			//LinkMessage = linkMessage;
		}

		public LoggingEventItem(LogType messageType, DateTime messageTime, string messageText, string linkMessage = null)
		{
			Type = messageType;
			Time = messageTime;
			Message = messageText;
			LinkMessage = linkMessage;
		}

		public LogType Type { get; set; }

		public DateTime Time { get; set; }
		public string Message { get; set; }

		public string LinkMessage { get; set; }
	}
}
