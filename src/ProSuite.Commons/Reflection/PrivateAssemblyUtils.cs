using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Reflection
{
	/// <summary>
	/// Methods for loading assemblies/types based on assembly/type names. If the assembly name
	/// is not fully qualified (i.e. containing public key/version information), then the 
	/// assembly is loaded from the bin directory (the directory containing the executing assembly)
	/// </summary>
	public static class PrivateAssemblyUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static string _binDirectory;

		[ThreadStatic] private static Dictionary<string, bool> _checkedAssemblyFiles;

		// Redirect legacy references from existing installations:
		private static readonly Dictionary<string, string> _knownSubstitutes =
			new Dictionary<string, string>
			{
				{"EsriDE.ProSuite.QA.Tests", "ProSuite.QA.Tests"},
				{"EsriDE.ProSuite.QA.TestFactories", "ProSuite.QA.TestFactories"}
			};

		[NotNull]
		private static string BinDirectory
		{
			get { return _binDirectory ?? (_binDirectory = GetBinDirectory()); }
		}

		public static Assembly LoadAssembly(
			[NotNull] string assemblyName,
			[CanBeNull] IReadOnlyDictionary<string, string> assemblySubstitutes = null)
		{
			IReadOnlyDictionary<string, string> substitutes = GetSubstitutes(assemblySubstitutes);

			bool throwOnError =
				! substitutes.TryGetValue(assemblyName, out string substituteAssembly);

			Assembly assembly;
			try
			{
				AssemblyName name = GetAssemblyName(BinDirectory, assemblyName);

				if (string.IsNullOrEmpty(name.CodeBase))
				{
					_msg.VerboseDebug(() => $"Loading assembly from {name}");
				}
				else
				{
					_msg.VerboseDebug(
						() => $"Loading assembly from {name} (codebase: {name.CodeBase})");
				}

				assembly = Assembly.Load(name);
			}
			catch (Exception e)
			{
				_msg.Debug($"Loading {assemblyName} from {BinDirectory} failed.", e);

				if (throwOnError)
				{
					throw;
				}

				_msg.DebugFormat("Trying assembly substitute {0}...", substituteAssembly);

				assembly = Assembly.Load(substituteAssembly);
			}

			return assembly;
		}

		[NotNull]
		public static Type LoadType([NotNull] string assemblyName,
		                            [NotNull] string typeName,
		                            IReadOnlyDictionary<string, string> assemblySubstitutes = null)
		{
			Assert.ArgumentNotNullOrEmpty(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNullOrEmpty(typeName, nameof(typeName));

			var substitutes = assemblySubstitutes ?? _knownSubstitutes;

			Assembly assembly = LoadAssembly(assemblyName, substitutes);

			bool throwOnError =
				! substitutes.TryGetValue(assemblyName, out string substituteAssembly);

			Type type = assembly.GetType(typeName, throwOnError);
			if (type == null)
			{
				string substituteType = typeName.Replace(assemblyName, substituteAssembly);

				_msg.DebugFormat("Failed loading type {0} from {1}, trying {2} from {3}",
				                 typeName, assemblyName, substituteType, substituteAssembly);

				return LoadType(Assert.NotNull(substituteAssembly), substituteType,
				                new Dictionary<string, string>(0));
			}

			return type;
		}

		public static string GetSubsituteType(
			[NotNull] string assemblyName,
			[NotNull] string typeName,
			IReadOnlyDictionary<string, string> assemblySubstitutes = null)
		{
			Assert.ArgumentNotNullOrEmpty(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNullOrEmpty(typeName, nameof(typeName));

			var substitutes = assemblySubstitutes ?? _knownSubstitutes;

			if (substitutes.TryGetValue(assemblyName, out string substituteAssembly))
			{
				string substituteType = typeName.Replace(assemblyName, substituteAssembly);
				return substituteType;
			}

			return typeName;
		}

		[CanBeNull]
		public static string GetCoreName([CanBeNull] string fullName)
		{
			return fullName?.Substring(fullName.LastIndexOf('.') + 1);
		}

		private static IReadOnlyDictionary<string, string> GetSubstitutes(
			IReadOnlyDictionary<string, string> assemblySubstitutes)
		{
			if (assemblySubstitutes == null)
			{
				return _knownSubstitutes;
			}

			Dictionary<string, string> substitutes =
				assemblySubstitutes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

			foreach (KeyValuePair<string, string> knownSubstitute in _knownSubstitutes)
			{
				if (! substitutes.ContainsKey(knownSubstitute.Key))
				{
					substitutes.Add(knownSubstitute.Key, knownSubstitute.Value);
				}
			}

			return substitutes;
		}

		[NotNull]
		private static AssemblyName GetAssemblyName([NotNull] string binDirectory,
		                                            [NotNull] string assemblyName)
		{
			Assert.ArgumentNotNullOrEmpty(binDirectory, nameof(binDirectory));
			Assert.ArgumentNotNullOrEmpty(assemblyName, nameof(assemblyName));

			if (IsFullyQualified(assemblyName))
			{
				return new AssemblyName(assemblyName);
			}

			if (_checkedAssemblyFiles == null)
			{
				_checkedAssemblyFiles =
					new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
			}

			string dllFileName = string.Format("{0}.dll", assemblyName);
			string exeFileName = string.Format("{0}.exe", assemblyName);

			foreach (string fileName in new[] {dllFileName, exeFileName})
			{
				string filePath = Path.Combine(binDirectory, fileName);

				bool fileExists;
				if (! _checkedAssemblyFiles.TryGetValue(filePath, out fileExists))
				{
					fileExists = File.Exists(filePath);

					_checkedAssemblyFiles.Add(filePath, fileExists);
				}

				if (fileExists)
				{
					return new AssemblyName(assemblyName) {CodeBase = filePath};
				}
			}

			throw new ArgumentException(
				string.Format("Assembly file not found for {0} in directory {1}",
				              assemblyName, binDirectory),
				nameof(assemblyName));
		}

		private static bool IsFullyQualified([NotNull] string assemblyName)
		{
			int firstCommaIndex = assemblyName.IndexOf(',');

			if (firstCommaIndex < 0)
			{
				return false;
			}

			const string pkToken = "PublicKeyToken";

			return assemblyName.IndexOf(pkToken,
			                            firstCommaIndex,
			                            StringComparison.OrdinalIgnoreCase) > 0;
		}

		[NotNull]
		private static string GetBinDirectory()
		{
			string assemblyPath = Assembly.GetExecutingAssembly().Location;
			Assert.NotNullOrEmpty(assemblyPath, "Undefined assembly path");

			string directory = Path.GetDirectoryName(assemblyPath);
			Assert.NotNullOrEmpty(directory, "Unable to get parent directory for assembly");
			return directory;
		}
	}
}
