using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Testing
{
	// Note Create a ZIP of PGDB (*.mdb) and use FromZip()

	public static class TestDataPreparer
	{
		private const string _defaultTestDataDir = "TestData";
		private const string _tempUnitTestData = @"C:\temp\UnitTestData";

		/// <summary>
		/// Normal use:
		/// <code>
		/// TestDataPreparer.ExtractZip("TestData.gdb.zip").GetPath();
		/// </code>
		/// Extracts the ZIP file to C:\temp and returns the full path.
		/// </summary>
		/// <param name="archiveName">Has to be the name of a ZIP file, e.g. "TestData.gdb.zip"</param>
		/// <param name="dirRelativeToProject">Relative path to test data. From where the .csproj file is to the directory with test data.
		/// </param>
		/// <example>
		/// TestDataPreparer.ExtractZip("unit_test_data_conflicts.gdb.zip").GetPath();
		/// TestDataPreparer.ExtractZip("unit_test_data_conflicts.gdb.zip", @"..\..\..\ProSuite.Shared\src\ProSuite.Shared.AGP.ConflictResolution.Test\TestData").GetPath()
		/// </example>
		[NotNull]
		public static ITestDataArchive ExtractZip([NotNull] string archiveName,
		                                          string dirRelativeToProject = _defaultTestDataDir)
		{
			return new TestDataArchive(archiveName,
			                           Assembly.GetCallingAssembly(), dirRelativeToProject);
		}

		[NotNull]
		public static ITestDataDirectory FromDirectory(
			string dirRelativeToProject = _defaultTestDataDir)
		{
			return new TestDataDirectory(Assembly.GetCallingAssembly(), dirRelativeToProject);
		}

		private abstract class TestDataBase
		{
			private static readonly IMsg _msg = Msg.ForCurrentClass();

			private readonly string _relativePath;

			protected readonly string ProjectName;

			protected TestDataBase([NotNull] Assembly callingAssembly,
			                       [CanBeNull] string relativePath)
			{
				Assert.ArgumentNotNull(callingAssembly, nameof(callingAssembly));

				_relativePath = relativePath;

				ProjectName = GetAssemblyProjectDirectoryName(callingAssembly);
			}

			public string GetTestDataDirectory(string callerFilePath)
			{
				string projectDir = GetProjectDirectory(callerFilePath, ProjectName);

				return GetTestDataDirectory(projectDir, _relativePath);
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
			private static string GetProjectDirectory([NotNull] string callerFilePath,
			                                          [NotNull] string projectName)
			{
				int length =
					callerFilePath.LastIndexOf(projectName, StringComparison.OrdinalIgnoreCase) +
					projectName.Length;

				return AssertPath(callerFilePath.Substring(0, length));
			}

			[NotNull]
			private static string GetTestDataDirectory([NotNull] string projectDirectory,
			                                           [NotNull]
			                                           string testDataDirRelativeToProject)
			{
				Assert.ArgumentNotNullOrEmpty(projectDirectory, nameof(projectDirectory));
				Assert.ArgumentNotNullOrEmpty(testDataDirRelativeToProject,
				                              nameof(testDataDirRelativeToProject));

				_msg.Debug(
					$"Search test data in {Path.Combine(projectDirectory, testDataDirRelativeToProject)}");

				string directory = Path.Combine(projectDirectory, testDataDirRelativeToProject);

				if (! Directory.Exists(directory))
				{
					throw new DirectoryNotFoundException($"{directory} not found");
				}

				_msg.Debug($"Found test data {directory}");

				return AssertPath(directory);
			}

			[NotNull]
			private static string AssertPath([CanBeNull] string path)
			{
				if (string.IsNullOrEmpty(path))
				{
					throw new ArgumentNullException($"{nameof(path)} is null");
				}

				if (Exists(path))
				{
					return path;
				}

				throw new FileNotFoundException($"File or directory not found: {path}");
			}

			private static bool Exists([CanBeNull] string path)
			{
				return File.Exists(path) || Directory.Exists(path);
			}

			protected static bool TryDeleteDirectory(string path)
			{
				if (! Directory.Exists(path))
				{
					return false;
				}

				FileSystemUtils.DeleteDirectory(path, true, true);
				return true;
			}

			protected static string GetTargetDirectory([NotNull] string projectName,
			                                           [CanBeNull] string testName,
			                                           string tempUnitTestData)
			{
				Assert.ArgumentNotNullOrEmpty(projectName);

				string tempDataDir = Path.Combine(tempUnitTestData, projectName);

				return string.IsNullOrEmpty(testName)
					       ? tempDataDir
					       : Path.Combine(tempDataDir, testName);
			}

			protected static bool IsEmpty(string path)
			{
				return ! Directory.EnumerateFileSystemEntries(path).Any();
			}
		}

		private class TestDataArchive : TestDataBase, ITestDataArchive
		{
			[NotNull] private readonly string _archiveName;
			private bool _overwrite;
			[CanBeNull] private string _subDirName;

			internal TestDataArchive([NotNull] string archiveName,
			                         [NotNull] Assembly callingAssembly,
			                         [CanBeNull] string relativePath = null) : base(
				callingAssembly, relativePath)
			{
				Assert.ArgumentNotNullOrEmpty(archiveName, nameof(archiveName));
				Assert.ArgumentCondition(
					archiveName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase),
					"Has to be a ZIP file");

				_archiveName = archiveName;
			}

			public string GetPath(string fileName = null,
			                      string callerFilePath = null,
			                      string testName = null)
			{
				Assert.ArgumentNotNullOrEmpty(callerFilePath, nameof(callerFilePath));
				Assert.ArgumentNotNullOrEmpty(testName, nameof(testName));

				string archivePath =
					Path.Combine(GetTestDataDirectory(callerFilePath), _archiveName);

				if (string.IsNullOrEmpty(_subDirName))
				{
					_subDirName = testName;
				}

				string targetDir = GetTargetDirectory(ProjectName, _subDirName, _tempUnitTestData);

				return CopyTo(archivePath, targetDir, fileName, _overwrite);
			}

			public string GetZipPath(string callerFilePath = null)
			{
				return Path.Combine(GetTestDataDirectory(callerFilePath), _archiveName);
			}

			public ITestDataArchive ExtractTo(string subDirName)
			{
				Assert.ArgumentNotNullOrEmpty(subDirName, nameof(subDirName));

				_subDirName = subDirName;

				return this;
			}

			public ITestDataArchive Overwrite()
			{
				_overwrite = true;

				return this;
			}

			private static string CopyTo(string sourcePath, string targetDir,
			                             string fileName = null, bool overwrite = false)
			{
				string result;
				if (sourcePath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
				{
					if (string.IsNullOrEmpty(fileName))
					{
						Extract(sourcePath, targetDir, overwrite);

						string[] files = Directory.GetFiles(targetDir, "*");
						string file = files.FirstOrDefault();

						string[] directories = Directory.GetDirectories(targetDir, "*");
						string directory = directories.FirstOrDefault();

						int itemCount = files.Length + directories.Length;

						if (fileName == null && itemCount > 1)
						{
							// No file provided -> return the extracted directory, unless it contains only a single item
							result = targetDir;
						}
						else if (file != null)
						{
							result = file;
						}
						else if (directory != null)
						{
							result = directory;
						}
						else
						{
							throw new ArgumentOutOfRangeException($"{sourcePath} is empty");
						}
					}
					else
					{
						string filePath = Path.Combine(targetDir, fileName);

						if (File.Exists(filePath) && overwrite)
						{
							Extract(sourcePath, targetDir, true);
						}
						else
						{
							Extract(sourcePath, targetDir, false);
						}

						result = filePath;
					}
				}
				else
				{
					throw new ArgumentException(
						$"{sourcePath} is not supported. Please create a zip archive.");
				}

				return result;
			}

			private static void Extract(string sourcePath, string targetDir, bool overwrite)
			{
				if (Directory.Exists(targetDir))
				{
					if (overwrite)
					{
						Assert.True(TryDeleteDirectory(targetDir), $"{targetDir} does not exist");

						// does not overwrite existing
						ZipFile.ExtractToDirectory(sourcePath, targetDir);
					}
					else if (IsEmpty(targetDir))
					{
						// does not overwrite existing
						ZipFile.ExtractToDirectory(sourcePath, targetDir);
					}
				}
				else
				{
					// does not overwrite existing
					ZipFile.ExtractToDirectory(sourcePath, targetDir);
				}
			}
		}

		private class TestDataDirectory : TestDataBase, ITestDataDirectory
		{
			public TestDataDirectory([NotNull] Assembly callingAssembly,
			                         [CanBeNull] string relativePath = null) : base(
				callingAssembly, relativePath)
			{
				Assert.ArgumentNotNull(callingAssembly, nameof(callingAssembly));
				Assert.ArgumentCondition(
					! string.IsNullOrEmpty(relativePath) &&
					! relativePath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase),
					$"{relativePath} must not be a zip");
			}

			public string GetPath(string fileName, string callerFilePath = null)
			{
				return Path.Combine(GetTestDataDirectory(callerFilePath), fileName);
			}

			public IEnumerable<string> GetPaths(IEnumerable<string> fileNames,
			                                    string callerFilePath = null)
			{
				string testDataDirectory = GetTestDataDirectory(callerFilePath);

				return fileNames.Select(fileName => Path.Combine(testDataDirectory, fileName));
			}

			public string GetDirectoryPath(string callerFilePath = null)
			{
				return GetTestDataDirectory(callerFilePath);
			}
		}
	}
}
