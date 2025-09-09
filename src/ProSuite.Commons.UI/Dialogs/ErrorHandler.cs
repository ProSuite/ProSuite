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

		/// <summary>
		/// Logs the formatted exception on log level Error using the provided message sink.
		/// </summary>
		/// <param name="exception"></param>
		/// <param name="msg"></param>
		public static void LogError([NotNull] Exception exception,
		                            [NotNull] IMsg msg)
		{
			Assert.ArgumentNotNull(exception, nameof(exception));

			string message = ExceptionUtils.FormatMessage(exception);

			msg.Error(message, exception);
		}

		/// <summary>
		/// Logs the formatted exception on log level Warn using the provided message sink.
		/// </summary>
		/// <param name="exception"></param>
		/// <param name="msg"></param>
		public static void LogWarn([NotNull] Exception exception,
		                           [NotNull] IMsg msg)
		{
			Assert.ArgumentNotNull(exception, nameof(exception));

			string message = ExceptionUtils.FormatMessage(exception);

			msg.Warn(message, exception);
		}

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
