using System.Threading;

namespace ProSuite.Commons
{
	public class ThreadAffineUseNameProvider : IUserNameProvider
	{
		private readonly ThreadLocal<string> _threadLocalUserName =
			new ThreadLocal<string>();

		public void SetDisplayName(string displayName)
		{
			_threadLocalUserName.Value = displayName;
		}

		public string DisplayName => _threadLocalUserName.Value;
	}
}
