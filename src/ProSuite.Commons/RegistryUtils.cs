using System;
using System.Collections.Generic;
using Microsoft.Win32;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	public static class RegistryUtils
	{
		[CanBeNull]
		public static string GetString(RegistryRootKey rootKey,
		                               [NotNull] string path,
		                               [NotNull] string name)
		{
			object value = GetValue(rootKey, path, name);

			return value == null
				       ? null
				       : Convert.ToString(value);
		}

		[CanBeNull]
		public static int? GetInt32(RegistryRootKey rootKey,
		                            [NotNull] string path,
		                            [NotNull] string name)
		{
			object value = GetValue(rootKey, path, name);

			return value == null
				       ? (int?) null
				       : Convert.ToInt32(value);
		}

		public static IEnumerable<string> GetKeyNames(RegistryRootKey rootKey,
		                                              [NotNull] string path)
		{
			RegistryKey rootRegKey = GetRootKey(rootKey);

			RegistryKey subKey = rootRegKey.OpenSubKey(path);

			if (subKey == null)
			{
				yield break;
			}

			foreach (string subKeyName in subKey.GetSubKeyNames())
			{
				yield return subKeyName;
			}
		}

		[CanBeNull]
		private static object GetValue(RegistryRootKey rootKey,
		                               [NotNull] string path,
		                               [NotNull] string name)
		{
			RegistryKey rootRegKey = GetRootKey(rootKey);

			return GetValue(rootRegKey, path, name);
		}

		[CanBeNull]
		private static object GetValue(RegistryKey registryKey,
		                               [NotNull] string path,
		                               [NotNull] string name)
		{
			Assert.ArgumentNotNullOrEmpty(path, nameof(path));
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			RegistryKey subKey = registryKey.OpenSubKey(path);

			return subKey?.GetValue(name);
		}

		[NotNull]
		private static RegistryKey GetRootKey(RegistryRootKey rootKey)
		{
			switch (rootKey)
			{
				case RegistryRootKey.Classes:
					return Registry.ClassesRoot;

				case RegistryRootKey.CurrentUser:
					return Registry.CurrentUser;

				case RegistryRootKey.LocalMachine:
					return Registry.LocalMachine;

				case RegistryRootKey.Users:
					return Registry.Users;

				case RegistryRootKey.CurrentConfig:
					return Registry.CurrentConfig;

				case RegistryRootKey.PerformanceData:
					return Registry.PerformanceData;

				default:
					throw new ArgumentOutOfRangeException(nameof(rootKey), rootKey,
					                                      "Unsupported root key");
			}
		}
	}
}
