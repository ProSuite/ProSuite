using System;
using System.Diagnostics;
using System.Text;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;

namespace ProSuite.Commons.Logging
{
	/// <summary>
	/// Base class that encapsulates logging functionality and provides a base implementation
	/// which is independent from a specific logging framework.
	/// </summary>
	public abstract class MsgBase : IMsg
	{
		protected static string BreakReplacing { get; } = "\n";
		protected static string BreakTag { get; } = "<br>";

		private const string _indentPadding = "  ";
		private const int _maxIndentLevel = 10;
		private static readonly string[] _indentFormats;
		private static int _indentLevel;
		private static bool _isVerboseDebugEnabled;
		private static bool _reportMemoryConsumptionOnError;

		#region Constructors

		/// <summary>
		/// Initializes the <see cref="MsgBase"/> class.
		/// </summary>
		static MsgBase()
		{
			var indentFormats = new string[_maxIndentLevel + 1];

			var builder = new StringBuilder("{0}");

			for (int indentLevel = 0; indentLevel <= _maxIndentLevel; indentLevel++)
			{
				indentFormats[indentLevel] = builder.ToString();
				builder.Insert(0, _indentPadding);
			}

			_indentFormats = indentFormats;
		}

		#endregion

		#region IMsg Members

		public IDisposable IncrementIndentation()
		{
			return IncrementIndentation(null);
		}

		public IDisposable IncrementIndentation(string infoMessage)
		{
			if (! string.IsNullOrEmpty(infoMessage))
			{
				Info(infoMessage);
			}

			if (_indentLevel < _maxIndentLevel)
			{
				_indentLevel++;
			}

			return new DisposableCallback(DecrementIndentation);
		}

		[StringFormatMethod("infoFormat")]
		public IDisposable IncrementIndentation(string infoFormat, params object[] args)
		{
			// ReSharper disable once RedundantStringFormatCall
			return IncrementIndentation(string.Format(infoFormat, args));
		}

		public void DecrementIndentation()
		{
			if (_indentLevel > 0)
			{
				_indentLevel--;
			}
		}

		public void ResetIndentation()
		{
			_indentLevel = 0;
		}

		public int IndentationLevel => _indentLevel;

		public int MaximumIndentationLevel => _maxIndentLevel;

		public abstract bool IsDebugEnabled { get; }

		public abstract bool IsInfoEnabled { get; }

		public abstract bool IsWarnEnabled { get; }

		public abstract bool IsErrorEnabled { get; }

		public abstract bool IsFatalEnabled { get; }

		public bool IsVerboseDebugEnabled
		{
			get => _isVerboseDebugEnabled && IsDebugEnabled;
			set => _isVerboseDebugEnabled = value;
		}

		public bool ReportMemoryConsumptionOnError
		{
			get => _reportMemoryConsumptionOnError;
			set => _reportMemoryConsumptionOnError = value;
		}

		public void VerboseDebug(object message)
		{
			if (IsVerboseDebugEnabled)
			{
				DebugCore(Format(message));
			}
		}

		public void VerboseDebug(Func<string> message)
		{
			if (IsVerboseDebugEnabled)
			{
				DebugCore(message());
			}
		}

		public void Debug(object message)
		{
			if (IsDebugEnabled)
			{
				DebugCore(Format(message));
			}
		}

		public void VerboseDebug(object message, Exception exception)
		{
			if (IsVerboseDebugEnabled)
			{
				DebugCore(Format(message), exception);
			}
		}

		public void VerboseDebug(Func<string> message, Exception exception)
		{
			if (IsVerboseDebugEnabled)
			{
				DebugCore(message(), exception);
			}
		}

		public void Debug(object message, Exception exception)
		{
			if (IsDebugEnabled)
			{
				DebugCore(Format(message), exception);
			}
		}

		public void DebugMemory(object message)
		{
			const bool appendMemoryConsumption = true;
			DebugCore(Format(message, appendMemoryConsumption));
		}

		[StringFormatMethod("format")]
		public void DebugMemory(string format, params object[] args)
		{
			const bool appendMemoryConsumption = true;
			DebugCore(Format(string.Format(format, args), appendMemoryConsumption));
		}

		public void Info(object message)
		{
			InfoCore(Format(message));
		}

		public void Info(object message, Exception exception)
		{
			InfoCore(Format(message), exception);
		}

		public void Warn(object message)
		{
			WarnCore(Format(message));
		}

		public void Warn(object message, Exception exception)
		{
			WarnCore(Format(message), exception);
		}

		public void Error(object message)
		{
			ErrorCore(Format(message, _reportMemoryConsumptionOnError));
		}

		public void Error(object message, Exception exception)
		{
			ErrorCore(Format(message, _reportMemoryConsumptionOnError), exception);
		}

		public void Fatal(object message)
		{
			FatalCore(Format(message, _reportMemoryConsumptionOnError));
		}

		public void Fatal(object message, Exception exception)
		{
			FatalCore(Format(message, _reportMemoryConsumptionOnError), exception);
		}

		public abstract void VerboseDebugFormat(string format, params object[] args);

		public abstract void DebugFormat(string format, params object[] args);

		public abstract void InfoFormat(string format, params object[] args);

		public abstract void WarnFormat(string format, params object[] args);

		public abstract void ErrorFormat(string format, params object[] args);

		public abstract void FatalFormat(string format, params object[] args);

		public Stopwatch DebugStartTiming()
		{
			return DebugStartTiming(null);
		}

		[StringFormatMethod("format")]
		public Stopwatch DebugStartTiming(string format,
		                                  params object[] args)
		{
			if (! IsDebugEnabled)
			{
				return null;
			}

			if (! string.IsNullOrEmpty(format))
			{
				DebugCore(Format(format, args));
			}

			var watch = new Stopwatch();
			watch.Start();
			return watch;
		}

		[StringFormatMethod("format")]
		public void DebugStopTiming(Stopwatch stopwatch, string format,
		                            params object[] args)
		{
			if (stopwatch == null || ! IsDebugEnabled)
			{
				return;
			}

			stopwatch.Stop();

			string message = TryFormat((string) PrepareMessage(format), args);
			string suffix = string.Format(" [{0:N0} ms]", stopwatch.ElapsedMilliseconds);

			DebugCore(Format(message + suffix));
		}

		[NotNull]
		[StringFormatMethod("format")]
		private static string TryFormat([CanBeNull] string format, params object[] args)
		{
			if (format == null)
			{
				return string.Empty;
			}

			try
			{
				return string.Format(format, args);
			}
			catch (Exception e)
			{
				return format + string.Format(" (formatting error: {0})", e.Message);
			}
		}

		#endregion

		public static string ReplaceBreakTags(string message)
		{
			return message.Contains(BreakTag)
				       ? message.Replace(BreakTag, BreakReplacing)
				       : message;
		}

		#region Non-public members

		private static string IndentationFormat => _indentFormats[_indentLevel];

		protected abstract void DebugCore(string message);

		protected abstract void InfoCore(string message);

		protected abstract void WarnCore(string message);

		protected abstract void ErrorCore(string message);

		protected abstract void FatalCore(string message);

		protected abstract void DebugCore(string message, Exception exception);

		protected abstract void InfoCore(string message, Exception exception);

		protected abstract void WarnCore(string message, Exception exception);

		protected abstract void ErrorCore(string message, Exception exception);

		protected abstract void FatalCore(string message, Exception exception);

		protected abstract string RenderObject(object obj);

		protected string GetIndented(object message)
		{
			return string.Format(IndentationFormat, message);
		}

		protected string Format(object message, bool appendMemoryConsumption)
		{
			return GetIndented(PrepareMessage(message, appendMemoryConsumption));
		}

		protected string Format(object message)
		{
			return GetIndented(PrepareMessage(message));
		}

		protected string Format(string format, object[] args)
		{
			return GetIndented(TryFormat((string) PrepareMessage(format), args));
		}

		private static string GetMemoryConsumptionText()
		{
			ProcessUtils.GetMemorySize(out long virtualBytes,
			                           out long privateBytes,
			                           out long workingSet);

			const int mb = 1024 * 1024;
			return string.Format(
				"VB:{0:N0} Mb, PB:{1:N0} Mb, WS:{2:N0} Mb",
				virtualBytes / mb, privateBytes / mb, workingSet / mb);
		}

		private object PrepareMessage(object message,
		                              bool appendMemoryConsumption = false)
		{
			if (! (message is string messageString))
			{
				return GetAppendedMessage(RenderObject(message),
				                          appendMemoryConsumption);
			}

			if (messageString.Contains(BreakTag))
			{
				messageString = messageString.Replace(BreakTag, BreakReplacing);
			}

			return GetAppendedMessage(messageString, appendMemoryConsumption);
		}

		private static object GetAppendedMessage(string message,
		                                         bool appendMemoryConsumption)
		{
			return appendMemoryConsumption
				       ? string.Format("{0} [{1}]", message, GetMemoryConsumptionText())
				       : message;
		}

		#endregion

		#region Nested types

		#region Nested type: Callback

		private delegate void Callback();

		#endregion

		#region Nested type: DisposableCallback

		private class DisposableCallback : IDisposable
		{
			private readonly Callback _callback;

			public DisposableCallback(Callback callback)
			{
				_callback = callback;
			}

			#region IDisposable Members

			public void Dispose()
			{
				_callback();
			}

			#endregion
		}

		#endregion

		#endregion
	}
}
