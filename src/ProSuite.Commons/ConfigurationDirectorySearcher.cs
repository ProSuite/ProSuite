using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons
{
	public abstract class ConfigurationDirectorySearcher
	{
		[ContractAnnotation("required: true => notnull; required: false => canbenull")]
		public string GetConfigFilePath([NotNull] string configFileName,
		                                bool required = true)
		{
			Assert.ArgumentNotNullOrEmpty(configFileName, nameof(configFileName));

			List<string> paths = GetSearchPaths().ToList();

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
			}

			if (! required)
			{
				return null;
			}

			throw CreateFileNotFoundException(configFileName, paths);
		}

		[NotNull]
		public IEnumerable<string> GetSearchPaths()
		{
			var result = new List<string>();

			CollectCurrentUserSearchPaths(result);
			CollectAllUserSearchPaths(result);

			return GetUniquePaths(result);
		}

		[NotNull]
		protected virtual string ApplicationDataDirectory => string.Empty;

		protected virtual void CollectCurrentUserSearchPaths(
			[NotNull] ICollection<string> paths)
		{
			foreach (Environment.SpecialFolder folder
				in new[]
				   {
					   Environment.SpecialFolder.LocalApplicationData,
					   Environment.SpecialFolder.ApplicationData,
					   Environment.SpecialFolder.CommonApplicationData
				   })
			{
				string path = GetProfileConfigPath(folder);

				if (path != null)
				{
					paths.Add(path);
				}
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
		/// <returns></returns>
		[NotNull]
		[PublicAPI]
		protected virtual DirectoryInfo GetBinDirectory()
		{
			Assembly assembly = GetType().Assembly;

			if (assembly.Location == null)
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
			sb.AppendLine("The file does not exist in any of the following directories:");

			foreach (string path in paths)
			{
				sb.AppendFormat("- {0}", path);
				sb.AppendLine();
			}

			return new FileNotFoundException(sb.ToString());
		}

		[NotNull]
		private static IEnumerable<string> GetUniquePaths(
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