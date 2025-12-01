using System.Threading;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons
{
	public class ThreadAffineUseNameProvider : IUserNameProvider
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly ThreadLocal<string> _threadLocalUserName =
			new ThreadLocal<string>();

		public void SetDisplayName(string displayName)
		{
			_msg.VerboseDebug(() => $"Setting user name to '{displayName}' for " +
			                        $"thread {Thread.CurrentThread.ManagedThreadId}");

			_threadLocalUserName.Value = displayName;
		}

		public string DisplayName
		{
			get
			{
				_msg.VerboseDebug(() => $"Returning user name '{_threadLocalUserName.Value}' for " +
				                        $"thread {Thread.CurrentThread.ManagedThreadId}");

				return _threadLocalUserName.Value;
			}
		}
	}
}
