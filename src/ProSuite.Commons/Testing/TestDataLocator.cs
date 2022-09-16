using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Reflection;

namespace ProSuite.Commons.Testing
{
	public class TestDataLocator
	{
		private readonly string _testDataRoot;

		private const string _defaultTestDataDir = "TestData";

		public static TestDataLocator Create(string repositoryName,
		                                     string testDataDirRelativeToProject =
			                                     _defaultTestDataDir)
		{
			return new TestDataLocator(
				Assembly.GetCallingAssembly(),
				GetRelativePathFromBinToSrc(repositoryName),
				testDataDirRelativeToProject);
		}

		#region Constructors

		public TestDataLocator()
			: this(Assembly.GetCallingAssembly(),
			       GetRelativePathFromBinToSrc("ProSuite"),
			       _defaultTestDataDir) { }

		public TestDataLocator([NotNull] string relativePath)
			: this(Assembly.GetCallingAssembly(), relativePath, _defaultTestDataDir) { }

		public TestDataLocator([NotNull] string relativePath,
		                       [NotNull] string testDataDirectory)
			: this(Assembly.GetCallingAssembly(), relativePath, testDataDirectory) { }

		public TestDataLocator([NotNull] Assembly callingAssembly,
		                       [NotNull] string relativePath,
		                       [NotNull] string testDataDirectory)
		{
			_testDataRoot =
				GetTestDataRoot(
					GetAssemblyRelativePath(callingAssembly, relativePath,
					                        testDataDirectory),
					GetCurrentDirRelativePath(callingAssembly, relativePath,
					                          testDataDirectory));
		}

		#endregion

		[NotNull]
		public string GetPath([NotNull] string fileName, [CallerMemberName] string testName = null)
		{
			return GetPath(fileName, _testDataRoot, testName);
		}

		[NotNull]
		public string GetPath([NotNull] string fileName,
		                      [NotNull] string assemblyRelativePath,
		                      [NotNull] string currentDirRelativePath,
		                      string testName)
		{
			string testDataDirectory = GetTestDataRoot(assemblyRelativePath,
			                                           currentDirRelativePath);

			return GetPath(fileName, testDataDirectory, testName);
		}

		[NotNull]
		private static string GetPath([NotNull] string fileName,
		                              [NotNull] string testDataDirectory,
		                              [CanBeNull] string testName)
		{
			string path = Path.Combine(testDataDirectory, fileName);

			if (! File.Exists(path) && ! Directory.Exists(path))
			{
				throw new FileNotFoundException($"File or directory not found: {path}");
			}

			if (path.EndsWith(".7z", StringComparison.InvariantCultureIgnoreCase))
			{
				throw new ArgumentException(
					"7-zip archives are not supported, please create a zip archive");
			}

			string targetGdb;
			string targetDir;

			if (! string.IsNullOrEmpty(testName))
			{
				targetDir = Path.Combine(@"C:\temp\UnitTestData", testName);

				if (Directory.Exists(targetDir))
				{
					try
					{
						FileSystemUtils.DeleteDirectory(targetDir, true);
					}
					catch (Exception)
					{
						// caught deliberately
					}
				}

				Directory.CreateDirectory(targetDir);
			}
			else
			{
				targetDir = @"C:\temp\UnitTestData";
				targetGdb = GetFileGeodatabasePath(targetDir, fileName);

				if (Directory.Exists(targetGdb))
				{
					try
					{
						FileSystemUtils.DeleteDirectory(targetGdb, true);
					}
					catch (Exception)
					{
						// caught deliberately
					}
				}
			}

			if (fileName.EndsWith(".gdb", StringComparison.InvariantCultureIgnoreCase))
			{
				targetGdb = Path.Combine(targetDir, fileName);

				FileSystemUtils.CopyDirectory(path, targetGdb, true);

				return targetGdb;
			}
			if (fileName.EndsWith(".mdb", StringComparison.InvariantCultureIgnoreCase))
			{
				targetGdb = Path.Combine(targetDir, fileName);

				File.Copy(path, targetGdb);

				return targetGdb;
			}
			if (fileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
			{
				ZipFile.ExtractToDirectory(path, targetDir);
			
				targetGdb = GetFileGeodatabasePath(targetDir, fileName);
			
				Assert.True(Directory.Exists(targetGdb), $"{targetGdb} does not exist");

				return targetGdb;
			}

			throw new ArgumentException(
				$"{Path.GetExtension(path)} files are not supported, please create a zip archive");
		}

		private static string GetFileGeodatabasePath(string testDataDirectory, string fileName)
		{
			string path = Directory.GetDirectories(testDataDirectory, $"{Path.GetFileNameWithoutExtension(fileName)}*")
			                         .FirstOrDefault(candidate => candidate.EndsWith(".gdb"));

			return path;
		}

		/// <summary>
		/// Ensures that a file from the testdata directory is available as a writable copy. The file is copied
		/// to the local temp directory, using its original file name.
		/// </summary>
		/// <param name="fileName">Name of the file (local file name under test data directory).</param>
		/// <param name="overwrite">if set to <c>true</c> an existing local copy is overwritten. Otherwise, an
		/// existing local copy is reused.</param>
		/// <param name="copyName">Name of the copy.</param>
		/// <returns>
		/// Absolute path to the writable local copy.
		/// </returns>
		[NotNull]
		public string EnsureLocalWritableFile([NotNull] string fileName,
		                                      bool overwrite,
		                                      [CanBeNull] string copyName = null)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			string filePath = GetPath(fileName);

			// try to copy the mdb file

			string newName = string.IsNullOrEmpty(copyName)
				                 ? Path.GetFileName(filePath)
				                 : copyName;
			Assert.NotNullOrEmpty(newName, "newName");

			string tempPath = Path.Combine(Path.GetTempPath(), newName);

			try
			{
				File.Copy(filePath, tempPath, overwrite);
			}
			catch
			{
				// could be because workspace is open 
				// --> workspace might be usable for test(don't throw here)
				Console.WriteLine(@"Unable to copy local file ({0})", tempPath);
			}

			File.SetAttributes(tempPath, FileAttributes.Normal);

			return tempPath;
		}

		#region Non-public members

		[NotNull]
		private static string GetCurrentDirRelativePath([NotNull] Assembly assembly,
		                                                [NotNull] string relativePath,
		                                                [NotNull] string testDataDirectory)
		{
			string assemblyProjectPath = Path.Combine(
				relativePath,
				GetAssemblyProjectDirectoryName(assembly));

			return Path.Combine(assemblyProjectPath, testDataDirectory);
		}

		[NotNull]
		private static string GetAssemblyRelativePath([NotNull] Assembly assembly,
		                                              [NotNull] string relativePath,
		                                              [NotNull] string testDataDirectory)
		{
			var asmFile = new FileInfo(assembly.Location);
			DirectoryInfo binDirectory = Assert.NotNull(asmFile.Directory);

			string path1 = Path.Combine(binDirectory.FullName, relativePath);
			string assemblyProjectPath = Path.Combine(
				path1, GetAssemblyProjectDirectoryName(assembly));

			return Path.Combine(assemblyProjectPath, testDataDirectory);
		}

		[NotNull]
		private static string GetAssemblyProjectDirectoryName([NotNull] Assembly assembly)
		{
			// NOTE assumes same name for assembly and project directory

			var asmFile = new FileInfo(assembly.Location);

			return Assert.NotNull(
				Path.GetFileNameWithoutExtension(asmFile.Name), "file name is null");
		}

		[NotNull]
		private static string GetTestDataRoot([NotNull] string assemblyRelativePath,
		                                      [NotNull] string currentDirRelativePath)
		{
			string candidate = GetTestDataRootRelativeToAssembly(assemblyRelativePath);

			if (! Directory.Exists(candidate))
			{
				candidate = currentDirRelativePath;

				if (! Directory.Exists(candidate))
				{
					throw new FileNotFoundException($"File not found: {candidate}");
				}
			}

			return candidate;
		}

		[NotNull]
		private static string GetTestDataRootRelativeToAssembly(
			[NotNull] string relativePath)
		{
			var assembly = new FileInfo(Assembly.GetExecutingAssembly().Location);
			DirectoryInfo assemblyDirectory = Assert.NotNull(assembly.Directory);

			return Path.GetFullPath(Path.Combine(assemblyDirectory.FullName,
			                                     relativePath));
		}

		private static string GetRelativePathFromBinToSrc(string repositoryName)
		{
			string binDir = ReflectionUtils.GetAssemblyDirectory(Assembly.GetCallingAssembly());

			// for Swisstopo.Topgis.Server.sln
			string repoRoot =
				Path.Combine(binDir, EnvironmentUtils.Is64BitProcess
					                     ? @"..\..\..\"
					                     : @"..\..\");

			var directory = new DirectoryInfo(repoRoot);

			if (! directory.Name.Equals(repositoryName,
			                            StringComparison.InvariantCultureIgnoreCase))
			{
				// It is a sub-repository in a different repo
				repoRoot = Path.Combine(repoRoot, repositoryName);
			}

			var result = Path.Combine(repoRoot, "src");

			Assert.True(Directory.Exists(result), "Directory does not exist: {0}", result);

			return result;
		}

		#endregion
	}
}
