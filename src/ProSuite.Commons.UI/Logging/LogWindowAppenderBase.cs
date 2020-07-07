using log4net.Appender;
using log4net.Core;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Logging
{
	public abstract class LogWindowAppenderBase : AppenderSkeleton
	{
		private ILogWindow _logWindow;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LogWindowAppenderBase"/> class.
		/// </summary>
		/// <remarks>Empty default constructor</remarks>
		protected LogWindowAppenderBase([NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			Name = name;
		}

		#endregion

		private ILogWindow LogWindow => _logWindow ?? (_logWindow = GetLogWindow());

		protected override void Append(LoggingEvent loggingEvent)
		{
			ILogWindow window = LogWindow;

			window?.AddLogEvent(loggingEvent);
		}

		[CanBeNull]
		protected abstract ILogWindow GetLogWindow();
	}
}
