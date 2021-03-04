using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.WPF
{
	public static class ViewUtils
	{
		public static void Try([NotNull] Action action, [NotNull] IMsg msg,
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
				ErrorHandler.HandleError(e, msg);
			}
		}

		public static async Task TryAsync([NotNull] Func<Task> func, [NotNull] IMsg msg,
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
				ErrorHandler.HandleError(e, msg);
			}
		}

		public static async Task TryAsync([NotNull] Task action, [NotNull] IMsg msg,
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
				ErrorHandler.HandleError(e, msg);
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

			msg.VerboseDebug($"{method}");
		}
	}
}
