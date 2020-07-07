using System;
using System.Drawing;
using log4net.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Logging
{
	internal class LogEventItem
	{
		private readonly long _logNummer;
		private readonly Image _logLevelImage;
		private readonly string _logMessage;
		private readonly DateTime _logDateTime;
		private readonly Exception _exception;
		private readonly int _indentation;
		private readonly string _loggerName;
		private readonly string _logLevel;
		private readonly LogLevel _level;

		public LogEventItem(long logNummer,
		                    [NotNull] Image logLevelImage,
		                    [NotNull] LoggingEvent loggingEvent)
		{
			string message = loggingEvent.RenderedMessage;

			string trimmedMessage = message.Trim();

			_indentation = message.IndexOf(trimmedMessage, StringComparison.Ordinal);

			_logNummer = logNummer;
			_logLevelImage = logLevelImage;

			_logMessage = trimmedMessage;
			_logDateTime = loggingEvent.TimeStamp;
			_exception = loggingEvent.ExceptionObject;
			_loggerName = loggingEvent.LoggerName;
			_logLevel = loggingEvent.Level.DisplayName;

			_level = GetLogLevel(loggingEvent.Level);
		}

		public LogLevel Level => _level;

		[NotNull]
		public Image LogLevelImage => _logLevelImage;

		public DateTime LogDateTime => _logDateTime;

		public long LogNummer => _logNummer;

		[NotNull]
		public string LogMessage => _logMessage;

		public Exception Exception => _exception;

		public string LoggerName => _loggerName;

		public int Indentation => _indentation;

		public string LogLevel => _logLevel;

		private static LogLevel GetLogLevel(Level level)
		{
			if (level == log4net.Core.Level.Debug)
			{
				return Logging.LogLevel.Debug;
			}

			if (level == log4net.Core.Level.Info)
			{
				return Logging.LogLevel.Info;
			}

			if (level == log4net.Core.Level.Warn)
			{
				return Logging.LogLevel.Warn;
			}

			if (level == log4net.Core.Level.Error)
			{
				return Logging.LogLevel.Error;
			}

			if (level == log4net.Core.Level.Fatal)
			{
				return Logging.LogLevel.Fatal;
			}

			return Logging.LogLevel.Unknown;
		}
	}
}
