using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
#if !ARCGIS_11_0_OR_GREATER
using System.Linq;
#endif

namespace ProSuite.Commons.AO
{
	public static class RuntimeUtils
	{
		private static string _version;
		private static bool? _is10_3;
		private static bool? _is10_2;
		private static bool? _is10_1;
		private static bool? _is10_0;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

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

#if !ARCGIS_11_0_OR_GREATER
		[CanBeNull]
		public static string GetInstalledDesktopVersion()
		{
			RuntimeInfo info = GetInstalledRuntime(ProductCode.Desktop);
			return info?.Version;
		}

		[CanBeNull]
		public static string GetInstalledVersion(ProductCode productCode)
		{
			RuntimeInfo info = GetInstalledRuntime(productCode);
			return info?.Version;
		}

		[CanBeNull]
		public static RuntimeInfo GetInstalledRuntime(ProductCode productCode)
		{
			return RuntimeManager.InstalledRuntimes.FirstOrDefault(
				runtimeInfo => runtimeInfo.Product == productCode);
		}
#endif

		/// <summary>
		/// Tries to the get the version from the ESRI.ArcGIS.Version assembly.
		/// </summary>
		/// <param name="version">The [Major.Minor] version.</param>
		/// <returns></returns>
		/// <remarks>Partial workaround for COM-221 (only for direct calls, does not solve failures within called standard gp tools</remarks>
		private static bool TryGetVersionFromVersionAssembly(
			[CanBeNull] out string version)
		{
			Assembly esriVersionAssembly = typeof(LicenseLevel).Assembly;

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
#if !ARCGIS_11_0_OR_GREATER
				// the following code fails
				// - in the 64bit background environment (see COM-221): DllNotFound
				// - in specific cross-thread situations: InvalidComObject
				RuntimeInfo runtime = RuntimeManager.ActiveRuntime;

				if (runtime != null)
				{
					version = runtime.Version;
					return true;
				}
#endif
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
			catch (DllNotFoundException e)
			{
				return HandleErrorAndGetVersionFromAssembly(e, out version);
			}
			catch (InvalidComObjectException e)
			{
				return HandleErrorAndGetVersionFromAssembly(e, out version);
			}
		}

		private static bool HandleErrorAndGetVersionFromAssembly(Exception exception,
		                                                         out string version)
		{
			_msg.DebugFormat(
				"Error accessing RuntimeManager: {0}; trying to get version from assembly",
				exception.Message);

			if (TryGetVersionFromVersionAssembly(out version))
			{
				return true;
			}

			_msg.DebugFormat("Unable to get ArcGIS version from assembly");

			return false;
		}
	}
}
