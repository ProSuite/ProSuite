using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging.Inspector
{
	public interface ILoggingContextFormat
	{
		[CanBeNull]
		string[] ContextHeaders { get; }

		IEnumerable<string> FormatContextFields([CanBeNull] ILoggingContext loggingContext);
	}
}
