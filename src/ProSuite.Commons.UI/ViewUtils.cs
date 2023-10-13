using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;

namespace ProSuite.Commons.UI
{
	public static class ViewUtils
	{
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
				HandleError(e, msg, suppressErrorMessageBox);
			}
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
				HandleError(e, msg, suppressErrorMessageBox);
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
				HandleError(e, msg, suppressErrorMessageBox);
			}
		}

		public static async Task<T> TryAsync<T>([NotNull] Task<T> action, IMsg msg,
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
				ErrorHandler.HandleError(e, msg);
			}

			return await Task.FromResult(default(T));
		}

		private static void Log([NotNull] IMsg msg, [CanBeNull] string method)
		{
			Assert.ArgumentNotNull(msg, nameof(msg));

			msg.VerboseDebug(() => $"{method}");
		}

		private static void HandleError(Exception exception, IMsg msg,
		                                bool suppressErrorMessageBox)
		{
			if (suppressErrorMessageBox)
			{
				msg.Error(ExceptionUtils.FormatMessage(exception), exception);
			}
			else
			{
				ErrorHandler.HandleError(exception, msg);
			}
		}

		public static void RunOnUIThread([NotNull] Action action)
		{
			Assert.ArgumentNotNull(action, nameof(action));

			if (Application.Current.Dispatcher.CheckAccess())
			{
				//No invoke needed
				action();
			}
			else
			{
				//We are not on the UI
				Application.Current.Dispatcher.BeginInvoke(action);
			}
		}
	}
}
