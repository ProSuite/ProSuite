using System;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging.Inspector
{
	/// <summary>
	/// Capture log events into a fixed-size priority queue.
	/// Useful to collect log events during long-running processes
	/// for later user presentation. Usage: Attach(), run process,
	/// Detach(), present LastSnapshot in UI.
	/// </summary>
	public class LogInspector : IDisposable
	{
		private const int DefaultCapacity = 4096;

		private BufferAppender _appender;

		public LogInspector()
		{
			Capacity = DefaultCapacity;
			Threshold = LogInspectorLevel.Debug;
			LoggerPrefix = null;

			LastSnapshot = new LogSnapshot(DefaultCapacity, DateTime.Now);
		}

		public int Capacity { get; set; }
		public LogInspectorLevel Threshold { get; set; }
		public string LoggerPrefix { get; set; }

		public bool IsAttached => _appender != null;

		[NotNull]
		public LogSnapshot LastSnapshot { get; private set; }

		public void Attach()
		{
			// TODO Params to attach to specific repository/logger?

			if (_appender != null)
			{
				throw new InvalidOperationException("Already attached");
			}

			// This should use the proper logger repository and
			// it's a no-op if the appender is already attached:
			Log4NetUtils.AddRootAppender(Interceptor);

			var appender = new BufferAppender(Math.Max(1, Capacity), DateTime.Now);
			var guid = Guid.NewGuid().ToString("N"); // hex digits only
			appender.Name = $"Inspector_{guid}";
			appender.Threshold = ConvertLevel(Threshold);

			if (! string.IsNullOrEmpty(LoggerPrefix))
			{
				appender.AddFilter(new LoggerMatchFilter {LoggerToMatch = LoggerPrefix});
				appender.AddFilter(new DenyAllFilter());
			}

			// An appender's Threshold and Filter chain interact as follows:
			// 1. ignore e if e.Level < a.Threshold
			// 2. walk filter chain until first Accept or Deny
			// 3. PreAppendCheck() and exit if false
			// 4. call Append(e)

			Interceptor.AddAppender(appender);

			// Just for the record:
			// Adding the same instance again is a no-op. Adding another
			// instance with the *same name* will be added to the list!

			_appender = appender;
		}

		/// <summary>
		/// Take a snapshot of the captured logging events
		/// while attached to an event source.
		/// </summary>
		public void TakeSnapshot()
		{
			if (_appender == null)
			{
				throw new InvalidOperationException("Not attached");
			}

			LastSnapshot = _appender.Snapshot();
		}

		/// <summary>
		/// Detach from the logging event source.
		/// Implicitly take a snapshot.
		/// </summary>
		public void Detach()
		{
			if (_appender == null)
			{
				throw new InvalidOperationException("Not attached");
			}

			// RemoveAppender throws an exception if the appender
			// to be removed is not in the list of appenders!
			Interceptor.RemoveAppender(_appender);

			TakeSnapshot();

			_appender = null;
		}

		public void Dispose()
		{
			if (_appender != null)
			{
				Detach();
			}
		}

		#region Static utilities

		private static ForwardingAppender Interceptor => RootInterceptor.Instance;

		private static Level ConvertLevel(LogInspectorLevel level)
		{
			switch (level)
			{
				case LogInspectorLevel.All:
					return Level.All;
				case LogInspectorLevel.Debug:
					return Level.Debug;
				case LogInspectorLevel.Info:
					return Level.Info;
				case LogInspectorLevel.Warn:
					return Level.Warn;
				case LogInspectorLevel.Error:
					return Level.Error;
				case LogInspectorLevel.Off:
					return Level.Off;
				default:
					throw new ArgumentOutOfRangeException(nameof(level), level, null);
			}
		}

		#endregion

		#region Nested type: RootInterceptor

		/// <summary>
		/// A singleton subclass of the <see cref="ForwardingAppender"/>.
		/// To be attached to the root logger. A singleton so we can
		/// easily find it later on.
		/// </summary>
		private sealed class RootInterceptor : ForwardingAppender
		{
			private static volatile RootInterceptor _instance;
			private static readonly object _syncRoot = new object();

			public static RootInterceptor Instance
			{
				get
				{
					if (_instance == null)
					{
						lock (_syncRoot)
						{
							if (_instance == null)
							{
								var guid = Guid.NewGuid().ToString("N"); // hex digits only
								_instance = new RootInterceptor {Name = guid};
							}
						}
					}

					return _instance;
				}
			}
		}

		#endregion
	}
}
