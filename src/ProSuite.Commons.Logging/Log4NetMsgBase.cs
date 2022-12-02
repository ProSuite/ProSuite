using System;
using log4net;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging
{
	/// <summary>
	/// Log Msg class using Log4Net.
	/// </summary>
	public class Log4NetMsgBase : MsgBase
	{
		private readonly ILog _log;

		/// <summary>
		/// Initializes a new instance of the <see cref="MsgBase"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		protected Log4NetMsgBase([CanBeNull] Type type)
			: this(Log4NetUtils.GetLogger(type ?? typeof(MsgBase))) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="MsgBase"/> class.
		/// </summary>
		/// <param name="log">The log4net log</param>
		/// <remarks> constructor provided for unit testing (for injecting a mock log object). 
		/// The log is passed as object to avoid log4net dependency in the constructor 
		/// (which would force that dependency on all MsgBase users).
		/// </remarks>
		protected Log4NetMsgBase([NotNull] object log)
		{
			Assert.ArgumentNotNull(log, nameof(log));

			if (! (log is ILog iLog))
			{
				throw new ArgumentException(
					"log4net.ILog implementation expected", nameof(log));
			}

			_log = iLog;
		}

		protected override string RenderObject(object obj)
		{
			return _log.Logger.Repository.RendererMap.FindAndRender(obj);
		}

		public override bool IsDebugEnabled => _log.IsDebugEnabled;

		public override bool IsInfoEnabled => _log.IsInfoEnabled;

		public override bool IsWarnEnabled => _log.IsWarnEnabled;

		public override bool IsErrorEnabled => _log.IsErrorEnabled;

		public override bool IsFatalEnabled => _log.IsFatalEnabled;

		[StringFormatMethod("format")]
		public override void VerboseDebugFormat(string format, params object[] args)
		{
			if (IsVerboseDebugEnabled)
			{
				DebugCore(Format(format, args));
			}
		}

		[StringFormatMethod("format")]
		public override void DebugFormat(string format, params object[] args)
		{
			if (IsDebugEnabled)
			{
				DebugCore(Format(format, args));
			}
		}

		[StringFormatMethod("format")]
		public override void InfoFormat(string format, params object[] args)
		{
			InfoCore(Format(format, args));
		}

		[StringFormatMethod("format")]
		public override void WarnFormat(string format, params object[] args)
		{
			WarnCore(Format(format, args));
		}

		[StringFormatMethod("format")]
		public override void ErrorFormat(string format, params object[] args)
		{
			ErrorCore(Format(format, args));
		}

		[StringFormatMethod("format")]
		public override void FatalFormat(string format, params object[] args)
		{
			FatalCore(Format(format, args));
		}

		protected override void DebugCore(string message)
		{
			_log.Debug(message);
		}

		protected override void InfoCore(string message)
		{
			_log.Info(message);
		}

		protected override void WarnCore(string message)
		{
			_log.Warn(message);
		}

		protected override void ErrorCore(string message)
		{
			_log.Error(message);
		}

		protected override void FatalCore(string message)
		{
			_log.Fatal(message);
		}

		protected override void DebugCore(string message, Exception exception)
		{
			_log.Debug(message, exception);
		}

		protected override void InfoCore(string message, Exception exception)
		{
			_log.Info(message, exception);
		}

		protected override void WarnCore(string message, Exception exception)
		{
			_log.Warn(message, exception);
		}

		protected override void ErrorCore(string message, Exception exception)
		{
			_log.Error(message, exception);
		}

		protected override void FatalCore(string message, Exception exception)
		{
			_log.Fatal(message, exception);
		}
	}
}
