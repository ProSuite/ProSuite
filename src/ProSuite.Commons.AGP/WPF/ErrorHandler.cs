using System;
using System.Windows;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace ProSuite.Commons.AGP.WPF
{
	public static class ErrorHandler
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _title = "An error has occurred";

		public static void HandleError([NotNull] Exception exception,
		                               [CanBeNull] IMsg msg,
		                               [CanBeNull] string title = null)
		{
			Assert.ArgumentNotNull(exception, nameof(exception));

			HandleError(ExceptionUtils.FormatMessage(exception), exception, msg, title);
		}

		public static void HandleError([NotNull] string message,
		                               [CanBeNull] Exception exception,
		                               [CanBeNull] IMsg msg = null,
		                               [CanBeNull] string title = null)
		{
			string caption = string.IsNullOrEmpty(title) ? _title : title;

			if (msg == null)
			{
				msg = _msg;
			}

			// write to log
			if (exception != null)
			{
				msg.Error(message, exception);
			}
			else
			{
				msg.Error(message);
			}

			Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private static MessageBoxResult Show(string message, string caption,
		                                     MessageBoxButton button, MessageBoxImage image)
		{
			return Application.Current.Dispatcher.Invoke(
				() => MessageBox.Show(message, caption, button, image));
		}
	}
}
