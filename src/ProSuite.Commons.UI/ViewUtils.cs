using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI
{
	public static class ViewUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static void Try([NotNull] Action action, [NotNull] IMsg msg,
		                       bool suppressErrorMessageBox = false,
		                       [CallerMemberName] string caller = null)
		{
			Assert.ArgumentNotNull(action, nameof(action));
			Assert.ArgumentNotNull(msg, nameof(msg));

			try
			{
				Log(msg, caller);

				action();
			}
			catch (Exception e)
			{
				ShowError(e, msg, suppressErrorMessageBox);
			}
		}

		public static T Try<T>([NotNull] Func<T> func, [NotNull] IMsg msg,
		                       bool suppressErrorMessageBox = false,
		                       [CallerMemberName] string caller = null)
		{
			Assert.ArgumentNotNull(func, nameof(func));
			Assert.ArgumentNotNull(msg, nameof(msg));

			try
			{
				Log(msg, caller);

				return func();
			}
			catch (Exception e)
			{
				ShowError(e, msg, suppressErrorMessageBox);
			}

			return default;
		}

		// todo daro revise method signature. Could be replaces with
		// async Task TryAsync([NotNull] Task action, [NotNull] IMsg msg,[CallerMemberName] string caller = null)
		// ??
		public static async Task TryAsync([NotNull] Func<Task> func,
		                                  [NotNull] IMsg msg,
		                                  bool suppressErrorMessageBox = false,
		                                  [CallerMemberName] string caller = null)
		{
			Assert.ArgumentNotNull(func, nameof(func));
			Assert.ArgumentNotNull(msg, nameof(msg));

			try
			{
				Log(msg, caller);

				await func();
			}
			catch (Exception e)
			{
				ShowError(e, msg, suppressErrorMessageBox);
			}
		}

		public static async Task TryAsync([NotNull] Task action,
		                                  [NotNull] IMsg msg,
		                                  bool suppressErrorMessageBox = false,
		                                  [CallerMemberName] string caller = null)
		{
			Assert.ArgumentNotNull(action, nameof(action));
			Assert.ArgumentNotNull(msg, nameof(msg));

			try
			{
				Log(msg, caller);

				await action;
			}
			catch (Exception e)
			{
				ShowError(e, msg, suppressErrorMessageBox);
			}
		}

		public static async Task<T> TryAsync<T>([NotNull] Task<T> action,
		                                        [NotNull] IMsg msg,
		                                        bool suppressErrorMessageBox = false,
		                                        [CallerMemberName] string caller = null)
		{
			Assert.ArgumentNotNull(action, nameof(action));
			Assert.ArgumentNotNull(msg, nameof(msg));

			try
			{
				Log(msg, caller);

				return await action;
			}
			catch (Exception e)
			{
				ShowError(e, msg, suppressErrorMessageBox);
			}

			return await Task.FromResult(default(T));
		}

		private static void Log([NotNull] IMsg msg, [CanBeNull] string method)
		{
			Assert.ArgumentNotNull(msg, nameof(msg));

			msg.VerboseDebug(() => $"{method}");
		}

		public static void ShowError(Exception exception, IMsg msg,
		                             bool suppressErrorMessageBox = false)
		{
			try
			{
				Dispatcher dispatcher = Application.Current.Dispatcher;

				dispatcher.Invoke(() =>
				{
					string message = ExceptionUtils.FormatMessage(exception);
					msg.Error(message, exception);

					if (suppressErrorMessageBox)
					{
						return;
					}

					MessageBox.Show(GetMainWindow(), message, "An error has occurred",
					                MessageBoxButton.OK, MessageBoxImage.Error);
				});
			}
			catch (Exception ex)
			{
				_msg.Error($"{nameof(ShowError)}: {ex.Message}", ex);
			}
		}

		public static void RunOnUIThread([NotNull] Action action)
		{
			RunOnUIThread(() =>
			{
				action();
				return Task.CompletedTask;
			});
		}

		public static Task RunOnUIThread([NotNull] Func<Task> action)
		{
			Assert.ArgumentNotNull(action, nameof(action));

			Dispatcher dispatcher = Application.Current?.Dispatcher;

			// Do not throw from this method! A crash is almost guaranteed.

			if (dispatcher == null)
			{
				_msg.Warn("No dispatcher in this application");
				return Task.CompletedTask;
			}

			try
			{
				if (dispatcher.CheckAccess())
				{
					//No invoke needed
					return action();
				}

				//We are not on the UI
				dispatcher.BeginInvoke(new Action(() =>
				{
					try
					{
						action();
					}
					catch (Exception e)
					{
						// Prevent crashes by catching the exception here:
						_msg.Error($"Error running action on UI thread: {e.Message}", e);
					}
				}));
			}
			catch (Exception e)
			{
				_msg.Error($"Error running action on UI thread: {e.Message}", e);
			}

			return Task.CompletedTask;
		}

		public static bool IsOnUIThread()
		{
			try
			{
				Dispatcher dispatcher = Application.Current?.Dispatcher;

				if (dispatcher == null)
				{
					return false;
				}

				return dispatcher.CheckAccess();
			}
			catch (Exception)
			{
				// If we can't determine the thread context, assume we're not on the UI thread
				return false;
			}
		}

		private static Window GetMainWindow()
		{
			try
			{
				// Available only on thread that created the Application:
				return Application.Current.MainWindow;
			}
			catch
			{
				return null;
			}
		}
	}
}
