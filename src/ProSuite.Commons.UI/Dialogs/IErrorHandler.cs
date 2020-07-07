using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.Dialogs
{
	public interface IErrorHandler
	{
		[StringFormatMethod("format")]
		void HandleError([NotNull] Exception exception,
		                 [NotNull] IMsg msg,
		                 [NotNull] string format, params object[] args);
	}
}
