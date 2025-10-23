using System;
using System.Collections.Concurrent;
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
		public static Dictionary<string, string> KnownSubstitutes { get; } =
			new Dictionary<string, string>
			{
				{ "EsriDE.ProSuite.QA.Tests", "ProSuite.QA.Tests" },
				{ "EsriDE.ProSuite.QA.TestFactories", "ProSuite.QA.TestFactories" }
			};

		private static ConcurrentBag<string> _extraCodeBase;

		[NotNull]
		private static string BinDirectory
		{
			get { return _binDirectory ?? (_binDirectory = GetBinDirectory()); }
		}

		/// <summary>
		/// In case the code base is distributed across multiple directories, adds an extra
		/// bin directory which is searched in the LoadAssembly method. This 
		/// </summary>
		/// <param name="additionalBinDir"></param>
		public static void AddCodeBaseDir(string additionalBinDir)
		{
			if (_extraCodeBase == null)
			{
				_extraCodeBase = new ConcurrentBag<string>();
			}

			_extraCodeBase.Add(additionalBinDir);
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
				AssemblyName name = GetAssemblyName(assemblyName, BinDirectory, _extraCodeBase);

				if (string.IsNullOrEmpty(name.CodeBase))
				{
					_msg.VerboseDebug(() => $"Loading assembly from {name}");
				}
				else
				{
					_msg.VerboseDebug(
						() => $"Loading assembly from {name} (codebase: {name.CodeBase})");
				}

				AppDomain.CurrentDomain.AssemblyResolve +=
					(sender, args) => AssemblyResolveHandler(sender, args);

				assembly = Assembly.Load(name);
			}
			catch (Exception)
			{
				if (throwOnError)
				{
					throw;
				}

				_msg.VerboseDebug(
					() => $"Loading {assemblyName} from {BinDirectory} failed. " +
					      $"Trying assembly substitute {substituteAssembly}...");

				assembly = Assembly.Load(substituteAssembly);
			}

			return assembly;
		}

		private static Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
		{
			_msg.DebugFormat("Resolving {0}", args.Name);

			return null;
		}

		[NotNull]
		public static Type LoadType([NotNull] string assemblyName,
		                            [NotNull] string typeName,
		                            IReadOnlyDictionary<string, string> assemblySubstitutes = null)
		{
			Assert.ArgumentNotNullOrEmpty(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNullOrEmpty(typeName, nameof(typeName));

			var substitutes = assemblySubstitutes ?? KnownSubstitutes;

			Assembly assembly = LoadAssembly(assemblyName, substitutes);

			bool throwOnError =
				! substitutes.TryGetValue(assemblyName, out string substituteAssembly);

			Type type = assembly.GetType(typeName, throwOnError);
			if (type == null)
			{
				string substituteType = typeName.Replace(assemblyName, substituteAssembly);

				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.DebugFormat("Failed loading type {0} from {1}, trying {2} from {3}",
					                 typeName, assemblyName, substituteType, substituteAssembly);
				}

				return LoadType(Assert.NotNull(substituteAssembly), substituteType,
				                new Dictionary<string, string>(0));
			}

			return type;
		}

		public static string GetSubstituteType(
			[NotNull] string assemblyName,
			[NotNull] string typeName,
			IReadOnlyDictionary<string, string> assemblySubstitutes = null)
		{
			Assert.ArgumentNotNullOrEmpty(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNullOrEmpty(typeName, nameof(typeName));

			var substitutes = assemblySubstitutes ?? KnownSubstitutes;

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
				return KnownSubstitutes;
			}

			Dictionary<string, string> substitutes =
				assemblySubstitutes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

			foreach (KeyValuePair<string, string> knownSubstitute in KnownSubstitutes)
			{
				if (! substitutes.ContainsKey(knownSubstitute.Key))
				{
					substitutes.Add(knownSubstitute.Key, knownSubstitute.Value);
				}
			}

			return substitutes;
		}

		[NotNull]
		private static AssemblyName GetAssemblyName(
			[NotNull] string assemblyNameString,
			[NotNull] string binDirectory,
			[CanBeNull] IEnumerable<string> alternateBinDirectories)
		{
			Assert.ArgumentNotNullOrEmpty(binDirectory, nameof(binDirectory));
			Assert.ArgumentNotNullOrEmpty(assemblyNameString, nameof(assemblyNameString));

			if (IsFullyQualified(assemblyNameString))
			{
				return new AssemblyName(assemblyNameString);
			}

			AssemblyName assemblyName = TryGetAssemblyName(assemblyNameString, binDirectory);

			if (assemblyName != null)
			{
				return assemblyName;
			}

			if (alternateBinDirectories != null)
			{
				foreach (string alternateBinDirectory in alternateBinDirectories)
				{
					assemblyName = TryGetAssemblyName(assemblyNameString, alternateBinDirectory);

					if (assemblyName != null)
					{
						return assemblyName;
					}
				}
			}

			throw new ArgumentException(
				string.Format("Assembly file not found for {0} in directory {1}",
				              assemblyNameString, binDirectory),
				nameof(assemblyNameString));
		}

		private static AssemblyName TryGetAssemblyName(
			[NotNull] string assemblyNameString,
			[NotNull] string binDirectory)
		{
			if (_checkedAssemblyFiles == null)
			{
				_checkedAssemblyFiles =
					new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
			}

			string dllFileName = string.Format("{0}.dll", assemblyNameString);
			string exeFileName = string.Format("{0}.exe", assemblyNameString);

			foreach (string fileName in new[] { dllFileName, exeFileName })
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
					return new AssemblyName(assemblyNameString) { CodeBase = filePath };
				}
			}

			return null;
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
