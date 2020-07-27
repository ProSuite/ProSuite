using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clients.AGP.ProSuiteSolution.LoggerUI
{
	public enum LogType
	{
		Warning,
		Info,
		Debug,
		Error
	}

	public class ProSuiteQALogEventArgs : EventArgs
	{
		public LogMessage logMessage;

		public ProSuiteQALogEventArgs(LogMessage logMessage)
		{
			this.logMessage = logMessage;
		}

	}

	public class LogMessage
	{
		public LogMessage(LogType messageType, DateTime messageTime, string messageText, string linkMessage = null)
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
