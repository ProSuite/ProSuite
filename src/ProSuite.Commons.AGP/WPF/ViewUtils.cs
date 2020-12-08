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

		public static async Task TryAsync(Func<Task> func, IMsg msg,
		                                  [CallerMemberName] string caller = null)
		{
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

		private static void Log([NotNull] IMsg msg, [CanBeNull] string method)
		{
			Assert.ArgumentNotNull(msg, nameof(msg));

			msg.VerboseDebug($"{method}");
		}
	}
}
