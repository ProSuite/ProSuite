using System.Collections.Generic;
using log4net.Core;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging.Inspector
{
	public interface ILoggingContextInfo
	{
		[CanBeNull]
		string[] ContextHeaders { get; }

		[CanBeNull]
		ILoggingContext GetLoggingContext([CanBeNull] LoggingEvent loggingEvent);

		IEnumerable<string> FormatContextFields([CanBeNull] ILoggingContext loggingContext);
	}
}
