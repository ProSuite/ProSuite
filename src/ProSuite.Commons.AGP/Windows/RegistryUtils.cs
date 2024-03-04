using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace ProSuite.Commons.AGP.Windows;

/// <summary>
/// Simple access to the Windows Registry.
/// Concepts: Keys are like folders that can contain
/// Values and Sub Keys. Keys are formatted like Windows
/// paths (backslash as separator).
/// </summary>
/// <remarks>
/// Depends on Microsoft.Win32 and thus in Commons.AGP (where
/// we have this dependency anyway) and NOT in ProSuite.Commons
/// </remarks>
public static class RegistryUtils
{
	public enum RootKey
	{
		Classes,
		CurrentUser,
		LocalMachine,
		Users,
		CurrentConfig,
		PerformanceData
	}

	public static string GetString(RootKey root, string path, string name)
	{
		var value = GetValue(root, path, name);
		return value is null ? null : Convert.ToString(value);
	}

	public static int? GetInt32(RootKey root, string path, string name)
	{
		var value = GetValue(root, path, name);
		return value is null ? null : Convert.ToInt32(value);
	}

	public static IEnumerable<string> GetValueNames(RootKey root, string path)
	{
		if (path is null) return Enumerable.Empty<string>();

		var rootKey = GetRootKey(root);
		var subKey = rootKey.OpenSubKey(path);

		return subKey?.GetValueNames() ?? Enumerable.Empty<string>();
	}

	public static IEnumerable<string> GetKeyNames(RootKey root, string path)
	{
		if (path is null) return Enumerable.Empty<string>();

		var rootKey = GetRootKey(root);
		var subKey = rootKey.OpenSubKey(path);

		return subKey?.GetSubKeyNames() ?? Enumerable.Empty<string>();
	}

	private static object GetValue(RootKey root, string path, string name)
	{
		if (path is null) return null;
		if (string.IsNullOrEmpty(name)) return null;

		var rootKey = GetRootKey(root);
		var subKey = rootKey.OpenSubKey(path);

		return subKey?.GetValue(name);
	}

	private static RegistryKey GetRootKey(RootKey rootKey)
	{
		switch (rootKey)
		{
			case RootKey.Classes:
				return Registry.ClassesRoot;

			case RootKey.CurrentUser:
				return Registry.CurrentUser;

			case RootKey.LocalMachine:
				return Registry.LocalMachine;

			case RootKey.Users:
				return Registry.Users;

			case RootKey.CurrentConfig:
				return Registry.CurrentConfig;

			case RootKey.PerformanceData:
				return Registry.PerformanceData;

			default:
				throw new ArgumentOutOfRangeException(
					nameof(rootKey), rootKey, "Unsupported root key");
		}
	}
}
