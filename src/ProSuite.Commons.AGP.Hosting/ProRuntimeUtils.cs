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

		const string esriSoftwareKeyPath = @"SOFTWARE\ESRI";
		const string _arcProRegistryKeyName = "ArcGISPro";
		const string _arcServerRegistryKeyNamePrefix = "Server";

		private static string _arcgisProAssemlyPath;

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
				throw new InvalidOperationException(
					"Version of ArcGIS Pro cannot be determined because the registry value is missing.");
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

			RegistryKey esriKey = GetRegistryKey(esriSoftwareKeyPath);

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

		public static void SetupProAssemblyResolver(string installDir)
		{
			//Resolve ArcGIS Pro assemblies.
			_arcgisProAssemlyPath = installDir;

			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.AssemblyResolve += ResolveProAssemblyPath;
		}

		/// <summary>
		/// Resolves the ArcGIS Pro Assembly Path.  Called when loading of an assembly fails.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		/// <returns>programmatically loaded assembly in the pro /bin path</returns>
		private static Assembly ResolveProAssemblyPath(object sender, ResolveEventArgs args)
		{
			Assert.NotNullOrEmpty(_arcgisProAssemlyPath, "Pro Assembly install dir not set");

			string assemblyPath = Path.Combine(_arcgisProAssemlyPath, "bin",
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
			string arcGISProRegPath = $@"{esriSoftwareKeyPath}\{regKeyName}";

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
