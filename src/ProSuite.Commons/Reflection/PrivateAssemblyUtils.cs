using System;
using System.Collections.Generic;
using System.IO;
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
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private static string _binDirectory;

		[ThreadStatic] private static Dictionary<string, bool> _checkedAssemblyFiles;

		[NotNull]
		public static Assembly LoadAssembly([NotNull] string assemblyName)
		{
			Assert.ArgumentNotNullOrEmpty(assemblyName, nameof(assemblyName));

			AssemblyName name = GetAssemblyName(BinDirectory, assemblyName);

			if (string.IsNullOrEmpty(name.CodeBase))
			{
				_msg.VerboseDebugFormat("Loading assembly from {0}", name);
			}
			else
			{
				_msg.VerboseDebugFormat("Loading assembly from {0} (codebase: {1})",
				                        name, name.CodeBase);
			}

			return Assembly.Load(name);
		}

		[NotNull]
		public static Type LoadType([NotNull] string assemblyName,
		                            [NotNull] string typeName,
		                            IReadOnlyDictionary<string, string> assemblySubstitutes = null)
		{
			Assert.ArgumentNotNullOrEmpty(assemblyName, nameof(assemblyName));
			Assert.ArgumentNotNullOrEmpty(typeName, nameof(typeName));

			AssemblyName name = GetAssemblyName(BinDirectory, assemblyName);

			if (string.IsNullOrEmpty(name.CodeBase))
			{
				_msg.VerboseDebugFormat("Loading type {0} from {1}",
				                        typeName, name);
			}
			else
			{
				_msg.VerboseDebugFormat("Loading type {0} from {1} (codebase: {2})",
				                        typeName, name, name.CodeBase);
			}

			Assembly assembly = Assembly.Load(name);

			string substituteAssembly = null;

			bool throwOnError =
				assemblySubstitutes == null ||
				! assemblySubstitutes.TryGetValue(assemblyName, out substituteAssembly);

			Type type = assembly.GetType(typeName, throwOnError);

			if (type == null)
			{
				Assert.NotNull(substituteAssembly);
				string substituteType = typeName.Replace(assemblyName, substituteAssembly);

				_msg.Debug($"Failed loading type {typeName} from {assemblyName}, " +
				           $"trying {substituteType} from {substituteAssembly}");

				return LoadType(Assert.NotNull(substituteAssembly), substituteType);
			}

			return type;
		}

		[NotNull]
		private static string BinDirectory
		{
			get { return _binDirectory ?? (_binDirectory = GetBinDirectory()); }
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
