using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	public class OsUserNameProvider : IUserNameProvider
	{
		private readonly string _cachedDisplayName;

		public OsUserNameProvider() : this(true) { }

		public OsUserNameProvider(bool cacheDisplayName)
		{
			if (cacheDisplayName)
			{
				_cachedDisplayName = GetDisplayName();
			}
		}

		#region Implementation of IUserNameProvider

		public string DisplayName => _cachedDisplayName ?? GetDisplayName();

		#endregion

		[NotNull]
		private static string GetDisplayName()
		{
			return Environment.UserName;
		}
	}
}
