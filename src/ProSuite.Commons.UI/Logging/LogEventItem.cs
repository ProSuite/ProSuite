using System;
using System.Drawing;
using log4net.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Logging
{
	internal class LogEventItem
	{
		// Fully-qualified below to avoid clashing with this class's own LogLevel string
		// property: an unqualified "LogLevel" here would bind to that member, not the type.
		private readonly long _logNumber;
		private readonly Image _logLevelImage;
		private readonly string _logMessage;
		private readonly DateTime _logDateTime;
		private readonly Exception _exception;
		private readonly int _indentation;
		private readonly string _loggerName;
		private readonly string _logLevel;
		private readonly ProSuite.Commons.Logging.LogLevel _level;

		public LogEventItem(long logNumber,
		                    [NotNull] Image logLevelImage,
		                    [NotNull] LoggingEvent loggingEvent)
		{
			string message = loggingEvent.RenderedMessage;

			string trimmedMessage = message.Trim();

			_indentation = message.IndexOf(trimmedMessage, StringComparison.Ordinal);

			_logNumber = logNumber;
			_logLevelImage = logLevelImage;

			_logMessage = trimmedMessage;
			_logDateTime = loggingEvent.TimeStamp;
			_exception = loggingEvent.ExceptionObject;
			_loggerName = loggingEvent.LoggerName;
			_logLevel = loggingEvent.Level.DisplayName;

			_level = GetLogLevel(loggingEvent.Level);
		}

		public ProSuite.Commons.Logging.LogLevel Level => _level;

		[NotNull]
		public Image LogLevelImage => _logLevelImage;

		public DateTime LogDateTime => _logDateTime;

		public long LogNumber => _logNumber;

		[NotNull]
		public string LogMessage => _logMessage;

		public Exception Exception => _exception;

		public string LoggerName => _loggerName;

		public int Indentation => _indentation;

		public string LogLevel => _logLevel;

		private static ProSuite.Commons.Logging.LogLevel GetLogLevel(Level level)
		{
			if (level == log4net.Core.Level.Debug)
			{
				return ProSuite.Commons.Logging.LogLevel.Debug;
			}

			if (level == log4net.Core.Level.Info)
			{
				return ProSuite.Commons.Logging.LogLevel.Info;
			}

			if (level == log4net.Core.Level.Warn)
			{
				return ProSuite.Commons.Logging.LogLevel.Warn;
			}

			if (level == log4net.Core.Level.Error)
			{
				return ProSuite.Commons.Logging.LogLevel.Error;
			}

			if (level == log4net.Core.Level.Fatal)
			{
				return ProSuite.Commons.Logging.LogLevel.Fatal;
			}

			return ProSuite.Commons.Logging.LogLevel.Unknown;
		}
	}
}
