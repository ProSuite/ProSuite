using System;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.UI.Dialogs
{
	public static class ErrorHandler
	{
		private const string _title = "An error has occurred";

		public static void HandleError([NotNull] Exception exception,
		                               [CanBeNull] IMsg msg = null,
		                               [CanBeNull] IWin32Window owner = null,
		                               [CanBeNull] string title = null)
		{
			Assert.ArgumentNotNull(exception, nameof(exception));

			HandleError(ExceptionUtils.FormatMessage(exception), exception, msg, owner, title);
		}

		public static void HandleError([NotNull] string message,
		                               [CanBeNull] Exception exception,
		                               [CanBeNull] IMsg msg = null,
		                               [CanBeNull] IWin32Window owner = null,
		                               [CanBeNull] string title = null)
		{
			Assert.ArgumentNotNullOrEmpty(message, nameof(message));

			Dialog.Error(owner,
			             title != null && StringUtils.IsNotEmpty(title)
				             ? title
				             : _title,
			             message, exception, msg);
		}
	}
}
