using System;
using System.Diagnostics;
using System.Text;
using log4net;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;

namespace ProSuite.Commons.Logging
{
	/// <summary>
	/// Base class for assembly-specific Msg classes, which point to 
	/// the correct resource files for the assembly.
	/// </summary>
	public abstract class MsgBase : IMsg
	{
		private const string _breakReplacing = "\n";
		private const string _breakTag = "<br>";

		private const string _indentPadding = "  ";
		private const int _maxIndentLevel = 10;
		private static readonly string[] _indentFormats;
		private static int _indentLevel;
		private static bool _isVerboseDebugEnabled;
		private static bool _reportMemoryConsumptionOnError;
		private readonly ILog _log;

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

		///// <summary>
		///// Initializes a new instance of the <see cref="MsgBase"/> class.
		///// </summary>
		///// <param name="type">The type.</param>
		///// <param name="rm">The resource manager(s)</param>
		///// <remarks>The specified resource managers will be searched for a 
		///// supplied resource name in the given order.</remarks>
		//protected MsgBase(Type type, params ResourceManager[] rm) :
		//    this(LogManager.GetLogger(type), rm) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="MsgBase"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		protected MsgBase([CanBeNull] Type type)
			: this(Log4NetUtils.GetLogger(type ?? typeof(MsgBase))) { }

		///// <summary>
		///// Initializes a new instance of the <see cref="MsgBase"/> class.
		///// </summary>
		///// <param name="log">The log4net log</param>
		///// <param name="rm">The resource manager(s)</param>
		///// <remarks>The specified resource managers will be searched for a 
		///// supplied resource name in the given order.<para/>
		///// NOTE: constructor provided for unit testing (for injecting a mock log object). 
		///// The log is passed as object to avoid log4net dependency in the constructor 
		///// (which would force that dependency on all MsgBase users).
		///// </remarks>
		//protected MsgBase(object log, params ResourceManager[] rm)
		//{
		//    if (! (log is ILog))
		//    {
		//        throw new ArgumentException(
		//            "log4net.ILog implementation expected", "log");
		//    }

		//    _log = (ILog) log;
		//    // _resourceManagers = rm;
		//}

		/// <summary>
		/// Initializes a new instance of the <see cref="MsgBase"/> class.
		/// </summary>
		/// <param name="log">The log4net log</param>
		/// <remarks> constructor provided for unit testing (for injecting a mock log object). 
		/// The log is passed as object to avoid log4net dependency in the constructor 
		/// (which would force that dependency on all MsgBase users).
		/// </remarks>
		protected MsgBase([NotNull] object log)
		{
			Assert.ArgumentNotNull(log, nameof(log));

			if (! (log is ILog))
			{
				throw new ArgumentException(
					"log4net.ILog implementation expected", nameof(log));
			}

			_log = (ILog) log;
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

		[StringFormatMethod("format")]
		public void VerboseDebugFormat(string format, params object[] args)
		{
			if (IsVerboseDebugEnabled)
			{
				DebugCore(Format(format, args));
			}
		}

		[StringFormatMethod("format")]
		public void DebugFormat(string format, params object[] args)
		{
			if (_log.IsDebugEnabled)
			{
				DebugCore(Format(format, args));
			}
		}

		[StringFormatMethod("format")]
		public void InfoFormat(string format, params object[] args)
		{
			InfoCore(Format(format, args));
		}

		[StringFormatMethod("format")]
		public void WarnFormat(string format, params object[] args)
		{
			WarnCore(Format(format, args));
		}

		[StringFormatMethod("format")]
		public void ErrorFormat(string format, params object[] args)
		{
			ErrorCore(Format(format, args));
		}

		[StringFormatMethod("format")]
		public void FatalFormat(string format, params object[] args)
		{
			FatalCore(Format(format, args));
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
			if (_log.IsDebugEnabled)
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
			if (_log.IsDebugEnabled)
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

		public Stopwatch DebugStartTiming()
		{
			return DebugStartTiming(null);
		}

		[StringFormatMethod("format")]
		public Stopwatch DebugStartTiming(string format,
		                                  params object[] args)
		{
			if (! _log.IsDebugEnabled)
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
			if (stopwatch == null || ! _log.IsDebugEnabled)
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

		public bool IsDebugEnabled => _log.IsDebugEnabled;

		public bool IsVerboseDebugEnabled
		{
			get { return _isVerboseDebugEnabled && IsDebugEnabled; }
			set { _isVerboseDebugEnabled = value; }
		}

		public bool ReportMemoryConsumptionOnError
		{
			get { return _reportMemoryConsumptionOnError; }
			set { _reportMemoryConsumptionOnError = value; }
		}

		public bool IsInfoEnabled => _log.IsInfoEnabled;

		public bool IsWarnEnabled => _log.IsWarnEnabled;

		public bool IsErrorEnabled => _log.IsErrorEnabled;

		public bool IsFatalEnabled => _log.IsFatalEnabled;

		#endregion

		public static string ReplaceBreakTags(string message)
		{
			return message.Contains(_breakTag)
				       ? message.Replace(_breakTag, _breakReplacing)
				       : message;
		}

		#region Non-public members

		private static string IndentationFormat => _indentFormats[_indentLevel];

		protected virtual void DebugCore(string message)
		{
			_log.Debug(message);
		}

		protected virtual void InfoCore(string message)
		{
			_log.Info(message);
		}

		protected virtual void WarnCore(string message)
		{
			_log.Warn(message);
		}

		protected virtual void ErrorCore(string message)
		{
			_log.Error(message);
		}

		protected virtual void FatalCore(string message)
		{
			_log.Fatal(message);
		}

		protected virtual void DebugCore(string message, Exception exception)
		{
			_log.Debug(message, exception);
		}

		protected virtual void InfoCore(string message, Exception exception)
		{
			_log.Info(message, exception);
		}

		protected virtual void WarnCore(string message, Exception exception)
		{
			_log.Warn(message, exception);
		}

		protected virtual void ErrorCore(string message, Exception exception)
		{
			_log.Error(message, exception);
		}

		protected virtual void FatalCore(string message, Exception exception)
		{
			_log.Fatal(message, exception);
		}

		protected string GetIndented(object message)
		{
			return string.Format(IndentationFormat, message);
		}

		private string Format(object message, bool appendMemoryConsumption)
		{
			return GetIndented(PrepareMessage(message, appendMemoryConsumption));
		}

		private string Format(object message)
		{
			return GetIndented(PrepareMessage(message));
		}

		private string Format(string format, object[] args)
		{
			return GetIndented(TryFormat((string) PrepareMessage(format), args));
		}

		private static string GetMemoryConsumptionText()
		{
			long virtualBytes;
			long privateBytes;
			long workingSet;
			ProcessUtils.GetMemorySize(out virtualBytes, out privateBytes, out workingSet);

			const int mb = 1024 * 1024;
			return string.Format(
				"VB:{0:N0} PB:{1:N0} WS:{2:N0}",
				virtualBytes / mb,
				privateBytes / mb,
				workingSet / mb);
		}

		private object PrepareMessage(object message)
		{
			return PrepareMessage(message, false);
		}

		private object PrepareMessage(object message, bool appendMemoryConsumption)
		{
			var messageString = message as string;

			if (messageString == null)
			{
				return GetAppendedMessage(RenderObject(message),
				                          appendMemoryConsumption);
			}

			if (messageString.Contains(_breakTag))
			{
				messageString = messageString.Replace(_breakTag, _breakReplacing);
			}

			return GetAppendedMessage(messageString, appendMemoryConsumption);
		}

		private string RenderObject(object obj)
		{
			return _log.Logger.Repository.RendererMap.FindAndRender(obj);
		}

		private static object GetAppendedMessage(string message,
		                                         bool appendMemoryConsumption)
		{
			return appendMemoryConsumption
				       ? string.Format("{0} [{1}]", message,
				                       GetMemoryConsumptionText())
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
