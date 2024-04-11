using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	public abstract class ConfigurationDirectorySearcher : IConfigFileSearcher
	{
		[ContractAnnotation("required: true => notnull; required: false => canbenull")]
		public string GetConfigFilePath([NotNull] string configFileName,
		                                bool required = true)
		{
			Assert.ArgumentNotNullOrEmpty(configFileName, nameof(configFileName));

			var paths = GetSearchPaths();
			var misses = new List<string>();

			foreach (string path in paths)
			{
				if (path == null)
				{
					continue;
				}

				string filePath = Path.Combine(path, configFileName);

				if (File.Exists(filePath))
				{
					return filePath;
				}

				misses.Add(path);
			}

			if (! required)
			{
				return null;
			}

			throw CreateFileNotFoundException(configFileName, misses);
		}

		[NotNull]
		public IEnumerable<string> GetSearchPaths()
		{
			var result = new List<string>();

			CollectCurrentUserSearchPaths(result);
			CollectAllUserSearchPaths(result);

			return GetDistinctPaths(result);
		}

		/// <remarks>
		/// Override to something like COMPANY\PRODUCT\Config; the default is empty
		/// </remarks>
		[NotNull]
		protected virtual string ApplicationDataDirectory => string.Empty;

		protected virtual void CollectCurrentUserSearchPaths(
			[NotNull] ICollection<string> searchPaths)
		{
			if (searchPaths is null)
				throw new ArgumentNullException(nameof(searchPaths));

			// typically: $HOME\AppData\Local\COMPANY\PRODUCT\Config
			var localConfig = GetProfileConfigPath(Environment.SpecialFolder.LocalApplicationData);
			if (! string.IsNullOrEmpty(localConfig))
			{
				searchPaths.Add(localConfig);
			}

			// typically: $HOME\AppData\Roaming\COMPANY\PRODUCT\Config
			var roamingConfig = GetProfileConfigPath(Environment.SpecialFolder.ApplicationData);
			if (! string.IsNullOrEmpty(roamingConfig))
			{
				searchPaths.Add(roamingConfig);
			}

			// typically: C:\ProgramData\COMPANY\PRODUCT\Config (i.e., all users)
			var commonConfig = GetProfileConfigPath(Environment.SpecialFolder.CommonApplicationData);
			if (! string.IsNullOrEmpty(commonConfig))
			{
				searchPaths.Add(commonConfig);
			}
		}

		[CanBeNull]
		private string GetProfileConfigPath(Environment.SpecialFolder specialFolder)
		{
			string folderPath = Environment.GetFolderPath(specialFolder);

			return ! string.IsNullOrEmpty(ApplicationDataDirectory)
				       ? Path.Combine(folderPath, ApplicationDataDirectory)
				       : null;
		}

		protected virtual void CollectAllUserSearchPaths(
			[NotNull] ICollection<string> paths)
		{
			DirectoryInfo binDir = GetBinDirectory();
			if (binDir is null) return;

			paths.Add(binDir.FullName);

			DirectoryInfo up1Dir = binDir.Parent;

			if (up1Dir != null)
			{
				paths.Add(up1Dir.FullName);

				DirectoryInfo up2Dir = up1Dir.Parent;

				if (up2Dir != null)
				{
					paths.Add(up2Dir.FullName);

					DirectoryInfo up3Dir = up2Dir.Parent;
					if (up3Dir != null)
					{
						paths.Add(up3Dir.FullName);
					}
				}
			}

			var curDir = new DirectoryInfo(Environment.CurrentDirectory);
			paths.Add(curDir.FullName);
		}

		/// <summary>
		/// Gets the bin directory, i.e. the directory that contains the assembly which contains
		/// the concrete type (implementing subclass).
		/// Can be overridden by a subclass if a different directory should be considered the bin directory.
		/// </summary>
		[PublicAPI]
		[CanBeNull]
		protected virtual DirectoryInfo GetBinDirectory()
		{
			Assembly assembly = GetType().Assembly;

			if (assembly.Location is null)
			{
				throw new NullReferenceException("assembly location is undefined");
			}

			return Directory.GetParent(assembly.Location);
		}

		[NotNull]
		private static FileNotFoundException CreateFileNotFoundException(
			[NotNull] string configFileName,
			[NotNull] IEnumerable<string> paths)
		{
			var sb = new StringBuilder();

			sb.AppendFormat("Configuration file not found: {0}", configFileName);
			sb.AppendLine();

			sb.AppendLine();
			sb.AppendLine("The file does not exist in the following directories:");

			foreach (string path in paths)
			{
				sb.AppendFormat("- {0}", path);
				sb.AppendLine();
			}

			return new FileNotFoundException(sb.ToString(), configFileName);
		}

		[NotNull]
		private static IEnumerable<string> GetDistinctPaths(
			[NotNull] IEnumerable<string> paths)
		{
			var pathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (string path in paths)
			{
				if (pathSet.Add(path))
				{
					yield return path;
				}
			}
		}
	}
}
