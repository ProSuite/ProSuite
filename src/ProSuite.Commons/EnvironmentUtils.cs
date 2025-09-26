using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	public static class EnvironmentUtils
	{
		[NotNull] private static IUserNameProvider _userNameProvider =
			new OsUserNameProvider(false);

		[CanBeNull] private static IUserEmailProvider _userEmailProvider;

		[CanBeNull] private static IConfigurationDirectoryProvider
			_configurationDirectoryProvider;

		/// <summary>
		/// Indicates if the current process is 64 bit (true) or 32 bit (false)
		/// </summary>
		/// <remarks><see cref="Environment.Is64BitProcess"/> on <see cref="Environment"/> does not exist prior to .Net 4.0. Use this method instead.</remarks>
		public static bool Is64BitProcess => IntPtr.Size == 8; // 4 in 32 bit process

		[NotNull]
		public static SortedList<string, string> GetEnvironmentVariables()
		{
			var result = new SortedList<string, string>();

			foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
			{
				result.Add((string) de.Key, (string) de.Value);
			}

			return result;
		}

		[NotNull]
		public static SortedList<string, string> GetEnvironmentVariables(
			EnvironmentVariableTarget target)
		{
			var result = new SortedList<string, string>();

			foreach (DictionaryEntry de in Environment.GetEnvironmentVariables(target))
			{
				result.Add((string) de.Key, (string) de.Value);
			}

			return result;
		}

		public static bool GetBooleanEnvironmentVariableValue([NotNull] string variableName,
		                                                      bool defaultValue = false)
		{
			Assert.ArgumentNotNullOrEmpty(variableName, nameof(variableName));

			string value = Environment.GetEnvironmentVariable(variableName);

			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}

			return new[] {"yes", "true", "1"}.Any(
				yesValue => string.Equals(value, yesValue,
				                          StringComparison.OrdinalIgnoreCase));
		}

		public static void SetUserNameProvider([NotNull] IUserNameProvider userNameProvider)
		{
			Assert.ArgumentNotNull(userNameProvider, nameof(userNameProvider));

			_userNameProvider = userNameProvider;
		}

		public static IUserNameProvider GetUserNameProvider()
		{
			return _userNameProvider;
		}

		public static void SetUserEmailProvider([NotNull] IUserEmailProvider userEmailProvider)
		{
			Assert.ArgumentNotNull(userEmailProvider, nameof(userEmailProvider));

			_userEmailProvider = userEmailProvider;
		}

		public static void SetConfigurationDirectoryProvider(
			[NotNull] IConfigurationDirectoryProvider configurationDirectoryProvider)
		{
			Assert.ArgumentNotNull(configurationDirectoryProvider,
			                       nameof(configurationDirectoryProvider));

			_configurationDirectoryProvider = configurationDirectoryProvider;
		}

		[NotNull]
		public static string UserDisplayName => _userNameProvider.DisplayName;

		[CanBeNull]
		public static string UserEmailAddress => _userEmailProvider?.Email;

		[NotNull]
		public static IConfigurationDirectoryProvider ConfigurationDirectoryProvider =>
			Assert.NotNull(
				_configurationDirectoryProvider,
				"Configuration directory provider is not set. Please call " +
				"EnvironmentUtils.SetConfigurationDirectoryProvider() on startup of extension / executable");
	}
}
