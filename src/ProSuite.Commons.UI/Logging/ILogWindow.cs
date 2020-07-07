using log4net.Core;

namespace ProSuite.Commons.UI.Logging
{
	public interface ILogWindow
	{
		void AddLogEvent(LoggingEvent loggingEvent);

		void ScrollToEnd();
	}
}
