/*
   Copyright 2019 Esri
   Copyright 2022 The ProSuite Authors

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
       https://www.apache.org/licenses/LICENSE-2.0
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Hosting
{
	/// <summary>
	/// Encapsulates functionality to retrieve information about the installed Pro Runtime Version
	/// and use the runtime without having to deploy the CoreHost dll
	/// Original inspiration by:
	/// https://github.com/Esri/arcgis-pro-sdk-community-samples/blob/master/CoreHost/CoreHostResolveAssembly/Program.cs
	/// </summary>
	public static class ProRuntimeUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _esriSoftwareKeyPath = @"SOFTWARE\ESRI";
		private const string _arcProRegistryKeyName = "ArcGISPro";
		private const string _arcServerRegistryKeyNamePrefix = "Server";

		private static string _arcGisProAssemblyPath;

		public static bool IsProInstalled(out Version version)
		{
			RegistryKey proRegKey = GetProductRegKey(_arcProRegistryKeyName);

			if (proRegKey == null)
			{
				version = null;
				return false;
			}

			string versionString = proRegKey.GetValue("Version") as string;

			if (versionString == null)
			{
				// Likely it was installed in the past and the key still exists, but the Version value is gone:
				version = new Version(0, 0);
				_msg.Debug("The ArcGIS Pro 'Version' registry value was not found. " +
				           "Assuming that ArcGIS Pro is not installed.");
				return false;
			}

			version = new Version(versionString);

			return true;
		}

		public static bool IsServerInstalled(out Version version)
		{
			version = null;

			if (! TryGetServerProductKey(out RegistryKey serverRegKey))
			{
				return false;
			}

			string versionString = serverRegKey.GetValue("Version") as string;

			if (versionString == null)
			{
				throw new InvalidOperationException(
					"Version of ArcGIS Server cannot be determined because the expected registry value is missing.");
			}

			version = new Version(versionString);

			return true;
		}

		[NotNull]
		public static string GetServerInstallDir()
		{
			if (! TryGetServerProductKey(out RegistryKey serverRegKey))
			{
				throw new InvalidOperationException(
					"The registry key for ArcGIS server was not found. It might not be installed.");
			}

			string installDir = serverRegKey.GetValue("InstallDir") as string;

			if (string.IsNullOrEmpty(installDir))
			{
				throw new InvalidOperationException(
					"Install directory of ArcGIS Server cannot be found because the expected registry value is missing.");
			}

			return installDir;
		}

		private static bool TryGetServerProductKey(out RegistryKey serverRegistryKey)
		{
			// Alternative: Use Environment Variable AGSSERVER
			serverRegistryKey = null;

			RegistryKey esriKey = GetRegistryKey(_esriSoftwareKeyPath);

			if (esriKey == null)
			{
				_msg.Debug($"Registry key {esriKey} not found");
				return false;
			}

			string serverProductKey = null;
			foreach (string subKeyName in esriKey.GetSubKeyNames())
			{
				if (subKeyName.StartsWith(_arcServerRegistryKeyNamePrefix))
				{
					serverProductKey = subKeyName;
				}
			}

			if (serverProductKey == null)
			{
				return false;
			}

			serverRegistryKey = Assert.NotNull(GetProductRegKey(serverProductKey));
			return true;
		}

		[NotNull]
		public static string GetProInstallDir()
		{
			RegistryKey proRegKey = GetProductRegKey(_arcProRegistryKeyName);

			if (proRegKey == null)
			{
				throw new InvalidOperationException(
					$"The registry key for {_arcProRegistryKeyName} was not found.");
			}

			string installDir = proRegKey.GetValue("InstallDir") as string;

			if (string.IsNullOrEmpty(installDir))
			{
				throw new InvalidOperationException(
					"Install directory of ArcGIS Pro cannot be found because the registry value is missing.");
			}

			return installDir;
		}

		public static string GetProArcPyEnvironment()
		{
			RegistryKey proRegKey = GetProductRegKey(_arcProRegistryKeyName);

			if (proRegKey == null)
			{
				throw new InvalidOperationException(
					$"The registry key for {_arcProRegistryKeyName} was not found.");
			}

			// Get the Python Conda Root path from registry
			object pythonCondaRoot = proRegKey.GetValue("PythonCondaRoot");

			if (pythonCondaRoot != null)
			{
				string pythonPath = pythonCondaRoot.ToString();
				string executablePath = Path.Combine(pythonPath, @"Scripts\propy.bat");

				if (File.Exists(executablePath))
				{
					return executablePath;
				}
			}

			// Fallback: return empty string or throw exception based on requirements
			throw new InvalidOperationException(
				"Could not locate ArcGIS Pro Python environment (PythonCondaRoot not found in registry).");
		}

		public static void SetupProAssemblyResolver(string installDir)
		{
			//Resolve ArcGIS Pro assemblies.
			_arcGisProAssemblyPath = installDir;

			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.AssemblyResolve += ResolveProAssemblyPath;
		}

		/// <summary>
		/// Add the specified install dir to the PATH environment variable (process scope).
		/// Empirically, this is sometimes required for Core Host applications
		/// to successfully find and load CoreInterop.dll (and its dependencies).
		/// </summary>
		/// <param name="installDir">The ArcGIS Pro installation directory
		/// (without the trailing \bin), e.g. C:\Program Files\ArcGIS\Pro</param>
		/// <remarks>
		/// We prepend (not append) to PATH because otherwise Pro
		/// might load a DLL from another Program and likely the wrong
		/// version (typical example: freetype.dll, which is a component
		/// of many programs). Original inspiration for adding to PATH:
		/// https://github.com/Esri/arcgis-pro-sdk-community-samples/issues/30#issuecomment-424999941
		/// Windows searches for a DLL along the PATH env var as a last resort,
		/// see https://stackoverflow.com/questions/2463243/dll-search-on-windows
		/// </remarks>
		public static void AddBinDirectoryToPath(string installDir)
		{
			string proBinDir = Path.Combine(installDir, "bin");

			const string name = "PATH";
			const EnvironmentVariableTarget scope = EnvironmentVariableTarget.Process;

			var oldValue = Environment.GetEnvironmentVariable(name, scope);
			var newValue = $"{proBinDir};{oldValue}"; // prepend
			Environment.SetEnvironmentVariable(name, newValue, scope);
		}

		/// <summary>
		/// Resolves the ArcGIS Pro Assembly Path.  Called when loading of an assembly fails.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		/// <returns>programmatically loaded assembly in the pro /bin path</returns>
		private static Assembly ResolveProAssemblyPath(object sender, ResolveEventArgs args)
		{
			Assert.NotNullOrEmpty(_arcGisProAssemblyPath, "Pro Assembly install dir not set");

			string assemblyPath = Path.Combine(_arcGisProAssemblyPath, "bin",
			                                   new AssemblyName(args.Name).Name + ".dll");

			if (! File.Exists(assemblyPath))
			{
				return null;
			}

			Assembly assembly = Assembly.LoadFrom(assemblyPath);
			return assembly;
		}

		[CanBeNull]
		private static RegistryKey GetProductRegKey([NotNull] string regKeyName)
		{
			string arcGISProRegPath = $@"{_esriSoftwareKeyPath}\{regKeyName}";

			RegistryKey esriKey = GetRegistryKey(arcGISProRegPath);

			if (esriKey == null)
			{
				_msg.Debug(
					$@"The registry key HKLM\{arcGISProRegPath} or HKCU\{arcGISProRegPath} was not found. "
					+ $@"The product {regKeyName} might not be installed.");
			}

			return esriKey;
		}

		[CanBeNull]
		private static RegistryKey GetRegistryKey(string regKeyPathWithoutRoot)
		{
			// HKLM
			RegistryKey localKey =
				RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

			RegistryKey esriKey = localKey.OpenSubKey(regKeyPathWithoutRoot);

			// HKCU
			if (esriKey == null)
			{
				localKey =
					RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

				esriKey = localKey.OpenSubKey(regKeyPathWithoutRoot);
			}

			return esriKey;
		}
	}
}
