﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS;
using Microsoft.Win32;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;

namespace ProSuite.Commons.AO
{
	public static class RuntimeUtils
	{
		private static string _version;
		private static bool? _is10_3;
		private static bool? _is10_2;
		private static bool? _is10_1;
		private static bool? _is10_0;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static bool Is10_4orHigher => ! Is10_0 && ! Is10_1 && ! Is10_2 && ! Is10_3;

		public static bool Is10_3
		{
			get
			{
				if (_is10_3 == null)
				{
					_is10_3 = Equals(Version, "10.3");
				}

				return _is10_3 == true;
			}
		}

		public static bool Is10_2
		{
			get
			{
				if (_is10_2 == null)
				{
					_is10_2 = Equals(Version, "10.2");
				}

				return _is10_2 == true;
			}
		}

		public static bool Is10_1
		{
			get
			{
				if (_is10_1 == null)
				{
					_is10_1 = Equals(Version, "10.1");
				}

				return _is10_1 == true;
			}
		}

		public static bool Is10_0
		{
			get
			{
				if (_is10_0 == null)
				{
					_is10_0 = Equals(Version, "10.0");
				}

				return _is10_0 == true;
			}
		}

		[CanBeNull]
		public static string Version
		{
			get
			{
				if (_version == null)
				{
					string version;
					if (TryGetVersion(out version))
					{
						_version = version;
					}
				}

				return _version;
			}
		}

		[CanBeNull]
		public static string GetInstalledDesktopVersion()
		{
			RuntimeInfo info = GetInstalledRuntime(ProductCode.Desktop);
			return info?.Version;
		}

		[CanBeNull]
		[CLSCompliant(false)]
		public static string GetInstalledVersion(ProductCode productCode)
		{
			RuntimeInfo info = GetInstalledRuntime(productCode);
			return info?.Version;
		}

		[CanBeNull]
		[CLSCompliant(false)]
		public static RuntimeInfo GetInstalledRuntime(ProductCode productCode)
		{
			return RuntimeManager.InstalledRuntimes.FirstOrDefault(
				runtimeInfo => runtimeInfo.Product == productCode);
		}

		[NotNull]
		public static string GetInstallationDirectory(bool preferRunningProduct = false)
		{
			string version = Version;

			Assert.NotNull(version, "Unable to determine runtime version");

			List<string> registryKeys;

			string result;
			if (preferRunningProduct && TryGetInstallDirForActiveRuntime(out result))
			{
				return result;
			}

			if (SystemUtils.Is64BitProcess)
			{
				// must be either background geoprocessing or server
				registryKeys =
					new List<string>
					{
						@"HKEY_LOCAL_MACHINE\SOFTWARE\ESRI\Desktop Background Geoprocessing (64-bit)",
						$@"HKEY_LOCAL_MACHINE\SOFTWARE\ESRI\Server{version}"
					};
			}
			else
			{
				// must be either desktop or engine
				registryKeys =
					new List<string>
					{
						$@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\ESRI\Desktop{version}",
						$@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\ESRI\Engine{version}"
					};
			}

			const string valueName = "InstallDir";

			foreach (string key in registryKeys)
			{
				object value = Registry.GetValue(key, valueName, null);
				if (value == null)
				{
					continue;
				}

				var installDirectory = value as string;
				Assert.NotNull(installDirectory,
				               "unexpected value for '{0}' in key '{1}': {2}",
				               valueName, key, value);

				Assert.True(Directory.Exists(installDirectory),
				            "Install directory does not exist: {0}", installDirectory);

				return installDirectory;
			}

			throw new InvalidOperationException("Installation directory not found");
		}

		/// <summary>
		/// Tries to the get the version from the ESRI.ArcGIS.Version assembly.
		/// </summary>
		/// <param name="version">The [Major.Minor] version.</param>
		/// <returns></returns>
		/// <remarks>Partial workaround for COM-221 (only for direct calls, does not solve failures within called standard gp tools</remarks>
		private static bool TryGetVersionFromVersionAssembly(
			[CanBeNull] out string version)
		{
			Assembly esriVersionAssembly = typeof(RuntimeManager).Assembly;

			string fullVersion =
				ReflectionUtils.GetAssemblyVersionString(esriVersionAssembly);

			string[] tokens = fullVersion.Split('.');

			if (tokens.Length == 4)
			{
				version = $"{tokens[0]}.{tokens[1]}";
				return true;
			}

			version = null;
			return false;
		}

		private static bool TryGetVersion([CanBeNull] out string version)
		{
			try
			{
				// the following code fails in the 64bit background environment (see COM-221)
				RuntimeInfo runtime = RuntimeManager.ActiveRuntime;

				if (runtime == null)
				{
					// not bound yet?

					// There seems to be another scenario where this is null (observed
					// for background gp on a particular setup, which also includes server).

					_msg.Debug(
						"RuntimeInfo not available. Trying to get version from assembly");

					if (TryGetVersionFromVersionAssembly(out version))
					{
						return true;
					}

					_msg.DebugFormat("Unable to get ArcGIS version from assembly");
					version = null;
					return false;
				}

				version = runtime.Version;
				return true;
			}
			catch (DllNotFoundException e)
			{
				_msg.VerboseDebugFormat(
					"Error accessing RuntimeManager: {0}; trying to get version from assembly",
					e.Message);

				if (TryGetVersionFromVersionAssembly(out version))
				{
					return true;
				}

				_msg.DebugFormat("Unable to get ArcGIS version from assembly");
				return false;
			}
		}

		private static bool TryGetInstallDirForActiveRuntime(out string installDir)
		{
			installDir = null;

			try
			{
				// the following code fails in the 64bit background environment (see COM-221)
				RuntimeInfo runtime = RuntimeManager.ActiveRuntime;

				if (runtime == null)
				{
					// not bound yet?

					// There seems to be another scenario where this is null (observed
					// for background gp on a particular setup, which also includes server).
					_msg.Debug("RuntimeInfo not available.");

					return false;
				}

				if (Process.GetCurrentProcess().ProcessName.Equals("python"))
				{
					// in x64-bit python the active runtime is Server (even if background GP is installed too).
					// However, the (contour) GP tool fails (E_FAIL, TOP-5169) unless the Desktop(!) toolbox is referenced.
					_msg.DebugFormat(
						"Called from python. The active runtime ({0}) might be incorrect -> using default implementation, favoring Desktop.",
						runtime.Product);
					return false;
				}

				installDir = runtime.Path;
				return true;
			}
			catch (DllNotFoundException e)
			{
				_msg.VerboseDebugFormat(
					"Error accessing RuntimeManager: {0}.",
					e.Message);

				return false;
			}
		}
	}
}