using System;
using System.Collections.Generic;
using System.Linq;
using log4net.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging.Inspector
{
	public static class LogInspectorUtils
	{
		public static LogInspectorEntry ConvertEvent([NotNull] LoggingEvent loggingEvent)
		{
			var level = ConvertLevel(loggingEvent.Level);
			var message = loggingEvent.RenderedMessage ?? Convert.ToString(loggingEvent.MessageObject);

			var context = LoggingContext.GetLoggingContext(loggingEvent);

			return new LogInspectorEntry(level, loggingEvent.TimeStamp,
			                             loggingEvent.LoggerName, message,
			                             loggingEvent.ExceptionObject, context);
		}

		public static LogInspectorLevel ConvertLevel(Level level)
		{
			if (level == null) return LogInspectorLevel.All;
			if (level < Level.Debug) return LogInspectorLevel.All;
			if (level < Level.Info) return LogInspectorLevel.Debug;
			if (level < Level.Warn) return LogInspectorLevel.Info;
			if (level < Level.Error) return LogInspectorLevel.Warn;
			if (level < Level.Fatal) return LogInspectorLevel.Error;
			return LogInspectorLevel.Off;
		}

		public static IEnumerable<string> FormatFieldHeaders(ILoggingContextFormat format = null)
		{
			yield return "Date";
			yield return "Time";
			yield return "Level";
			yield return "Logger";
			yield return "Message";
			yield return "Exception";

			if (format?.ContextHeaders != null)
			{
				foreach (var contextHeader in format.ContextHeaders)
				{
					yield return contextHeader;
				}
			}
		}

		public static IEnumerable<string> FormatFieldValues(LogInspectorEntry item, ILoggingContextFormat format = null)
		{
			if (item == null) yield break;

			var timeStamp = item.TimeStamp;
			yield return timeStamp.ToString("yyyy-MM-dd");
			yield return timeStamp.ToString("HH:mm:ss");

			yield return item.Level.ToString();
			yield return item.LoggerName;
			yield return item.Message;
			yield return item.Exception?.ToString() ?? string.Empty;

			foreach (var value in FormatContextFields(item.Context, format))
			{
				yield return value;
			}
		}

		private static IEnumerable<string> FormatContextFields(
			[CanBeNull] object context,
			[CanBeNull] ILoggingContextFormat format)
		{
			if (context is ILoggingContext ctx)
			{
				return format != null
					       ? format.FormatContextFields(ctx)
					       : ctx.Select(obj => obj.ToString());
			}

			return Enumerable.Empty<string>();
		}
	}
}
