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

		private const string _errortitle = "An error has occurred";

		private const string
			_failuretitle = "Attention"; // todo daro think of a better title. Warning? Ooops? Shit?

		public static void HandleError([NotNull] Exception exception,
		                               [CanBeNull] IMsg msg,
		                               [CanBeNull] string title = null)
		{
			Assert.ArgumentNotNull(exception, nameof(exception));

			HandleError(ExceptionUtils.FormatMessage(exception), exception, msg, title);
		}

		public static void HandleFailure([NotNull] string message,
		                                 [CanBeNull] IMsg msg = null,
		                                 [CanBeNull] string title = null)
		{
			string caption = string.IsNullOrEmpty(title) ? _failuretitle : title;

			if (msg == null)
			{
				msg = _msg;
			}

			// write to log
			msg.Error(message);

			Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		public static void HandleError([NotNull] string message,
		                               [CanBeNull] Exception exception,
		                               [CanBeNull] IMsg msg = null,
		                               [CanBeNull] string title = null)
		{
			string caption = string.IsNullOrEmpty(title) ? _errortitle : title;

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

		public static MessageBoxResult Show(string message, string caption,
		                                    MessageBoxButton button, MessageBoxImage image)
		{
			return Application.Current.Dispatcher.Invoke(
				() => MessageBox.Show(message, caption, button, image));
		}
	}
}
