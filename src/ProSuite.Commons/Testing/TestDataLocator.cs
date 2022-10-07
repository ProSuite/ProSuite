using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;

namespace ProSuite.Commons.Testing
{
	/// <summary>
	/// Helper class to get at test data in unit tests.
	/// Our convention is that test data is in the TestData
	/// folder within the project. The test runner seems to
	/// use the assembly from the main bin/Debug directory,
	/// so test data is at ../../src/$PROJECT/TestData/.
	/// </summary>
	public class TestDataLocator
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static string _testDataRoot;

		private const string _defaultTestDataDir = "TestData";
		private const string _tempUnitTestData = @"C:\temp\UnitTestData";

		[Obsolete("Use TestDataLocator.Prepare() instead")]
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

		[Obsolete("Use TestDataLocator.Prepare() instead")]
		public TestDataLocator()
			: this(Assembly.GetCallingAssembly(),
			       GetRelativePathFromBinToSrc("ProSuite"),
			       _defaultTestDataDir) { }

		[Obsolete("Use TestDataLocator.Prepare() instead")]
		public TestDataLocator([NotNull] string relativePath)
			: this(Assembly.GetCallingAssembly(), relativePath, _defaultTestDataDir) { }

		[Obsolete("Use TestDataLocator.Prepare() instead")]
		public TestDataLocator([NotNull] string relativePath,
		                       [NotNull] string testDataDirectory)
			: this(Assembly.GetCallingAssembly(), relativePath, testDataDirectory) { }

		[Obsolete("Use TestDataLocator.Prepare() instead")]
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
		[Obsolete("Use TestDataLocator.Prepare() instead")]
		public string GetPath([NotNull] string fileName)
		{
			return GetPath(fileName, _testDataRoot);
		}

		[NotNull]
		[Obsolete("Use TestDataLocator.Prepare() instead")]
		public string GetPath([NotNull] string fileName,
		                      [NotNull] string assemblyRelativePath,
		                      [NotNull] string currentDirRelativePath)
		{
			string testDataDirectory = GetTestDataRoot(assemblyRelativePath,
			                                           currentDirRelativePath);

			return GetPath(fileName, testDataDirectory);
		}

		[NotNull]
		[Obsolete("Use TestDataLocator.Prepare() instead")]
		public string GetPath([NotNull] string fileName,
		                      [NotNull] string testDataDirectory)
		{
			string path = Path.Combine(testDataDirectory, fileName);

			if (! File.Exists(path) && ! Directory.Exists(path))
			{
				throw new FileNotFoundException($"File or directory not found: {path}");
			}

			return Path.GetFullPath(path);
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

		#region Prepare test data

		/// <summary>
		/// Copies test data to C:\temp. It assumes that test data is in
		/// ..\project directory\TestData\.<br/>
		/// Test data is copied to C:\Temp\UnitTestData\project name\<see cref="testName"/>\.
		/// </summary>
		/// <param name="fileName">Test data file name.</param>
		/// <param name="overwrite">Should existing test data be overwritten?</param>
		/// <param name="testName">
		/// The sub directory name to where the test data is copied to. If <see cref="testName" />
		/// is null the test data is copied to C:\Temp\UnitTestData\TestData\.
		/// </param>
		/// <param name="callerFilePath">Full path to test class.</param>
		/// <returns>Path to test data in C:\Temp\UnitTestData.</returns>
		[NotNull]
		public static string Prepare([NotNull] string fileName,
		                             bool overwrite = false,
		                             [CallerMemberName] string testName = null,
		                             [CallerFilePath] string callerFilePath = null)
		{
			string subDirName = string.IsNullOrEmpty(testName) ? "TestData" : testName;

			return Prepare(fileName,
			               _defaultTestDataDir,
			               overwrite,
			               Assembly.GetCallingAssembly(),
			               subDirName,
			               AssertPath(callerFilePath));
		}

		/// <summary>
		/// Copies test data to C:\temp. It is looking for test data in
		/// ..\project directory\<see cref="testDataDirRelativeToProject"/>\.<br />
		/// Test data is copied to C:\Temp\UnitTestData\project name\<see cref="testName"/>\.
		/// </summary>
		/// <param name="fileName">Test data file name.</param>
		/// <param name="testDataDirRelativeToProject">Relative path from .csproj file to test data directory.</param>
		/// <param name="overwrite">Should existing test data be overwritten?</param>
		/// <param name="testName">
		/// The sub directory name to where the test data is copied to. If <see cref="testName" />
		/// is null the test data is copied to C:\Temp\UnitTestData\TestData\.
		/// </param>
		/// <param name="callerFilePath">Full path to test class.</param>
		/// <returns>Path to test data in C:\Temp\UnitTestData.</returns>
		[NotNull]
		public static string Prepare([NotNull] string fileName,
		                             [NotNull] string testDataDirRelativeToProject,
		                             bool overwrite = false,
		                             [CallerMemberName] string testName = null,
		                             [CallerFilePath] string callerFilePath = null)
		{
			string subDirName = string.IsNullOrEmpty(testName) ? "TestData" : testName;

			return Prepare(fileName,
			               testDataDirRelativeToProject,
			               overwrite,
			               Assembly.GetCallingAssembly(),
			               subDirName,
			               AssertPath(callerFilePath));
		}

		/// <summary>
		/// Copies test data to C:\temp. It is looking for test data in
		/// ..\project directory\<see cref="testDataDirRelativeToProject"/>\.<br />
		/// Test data is copied to C:\Temp\UnitTestData\project name\<see cref="testName"/>\.
		/// </summary>
		/// <param name="fileName">Test data file name.</param>
		/// <param name="testDataDirRelativeToProject">Relative path from .csproj file to test data directory.</param>
		/// <param name="overwrite">Should existing test data be overwritten?</param>
		/// <param name="callingAssembly">The assembly from where this method is called.</param>
		/// <param name="testName">
		/// The sub directory name to where the test data is copied to. If <see cref="testName" />
		/// is null the test data is copied to C:\Temp\UnitTestData\TestData\.
		/// </param>
		/// <param name="callerFilePath">Full path to test class.</param>
		/// <returns>Path to test data in C:\Temp\UnitTestData.</returns>
		[NotNull]
		public static string Prepare([NotNull] string fileName,
		                             [NotNull] string testDataDirRelativeToProject,
		                             bool overwrite,
		                             [NotNull] Assembly callingAssembly,
		                             [NotNull] string testName,
		                             [NotNull] string callerFilePath)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));
			Assert.NotNull(callingAssembly);
			Assert.ArgumentNotNullOrEmpty(testName, nameof(testName));
			Assert.ArgumentNotNullOrEmpty(callerFilePath, nameof(callerFilePath));

			// callerFilePath = "C:\\git\\Swisstopo.GeniusDB\\ProSuite\\src\\ProSuite.Commons.Test\\Testing\\TestDataLocatorTest.cs"
			if (fileName.EndsWith(".gdb", StringComparison.InvariantCultureIgnoreCase))
			{
				throw new ArgumentException(
					"File Geodatabases are not supported. Please create a zip archive.");
			}

			string projectName = GetAssemblyProjectDirectoryName(callingAssembly);

			string projectDir = GetProjectDirectory(callerFilePath, projectName);

			string source = GetSource(projectDir, testDataDirRelativeToProject, fileName);

			string targetDir = GetTargetDirectory(projectName, testName);

			return CopyToTemp(source, targetDir, overwrite);
		}

		#endregion

		#region Non-public members Prepare test data

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
		private static string GetSource([NotNull] string projectDirectory,
		                                [NotNull] string testDataDirRelativeToProject,
		                                [NotNull] string fileName)
		{
			Assert.ArgumentNotNullOrEmpty(projectDirectory, nameof(projectDirectory));
			Assert.ArgumentNotNullOrEmpty(testDataDirRelativeToProject,
			                              nameof(testDataDirRelativeToProject));
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			_msg.Debug(
				$"Search test data in {Path.Combine(projectDirectory, testDataDirRelativeToProject)}");

			string directory = Path.Combine(projectDirectory, testDataDirRelativeToProject);

			if (! Directory.Exists(directory))
			{
				throw new DirectoryNotFoundException($"{directory} not found");
			}

			string path = Path.Combine(directory, fileName);

			// Don't check for existence here. Path could be a directory
			// which is handled later..
			//if (! File.Exists(path))
			//{
			//	throw new FileNotFoundException($"{path} not found");
			//}

			_msg.Debug($"Found test data {path}");
			return AssertPath(path);
		}

		// todo C:\temp\UnitTestData\project\method
		// now it's C:\temp\UnitTestData\method
		[NotNull]
		private static string CopyToTemp([NotNull] string sourcePath,
		                                 [NotNull] string targetPath,
		                                 bool overwrite = false)
		{
			if (sourcePath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
			{
				Extract(sourcePath, targetPath, overwrite);

				string targetGdb = GetFileGeodatabasePath(targetPath);

				Assert.True(Directory.Exists(targetGdb), $"{targetGdb} does not exist");

				return targetGdb;
			}

			if (sourcePath.EndsWith(".7z", StringComparison.InvariantCultureIgnoreCase))
			{
				throw new ArgumentException(
					"7-zip archives are not supported. Please create a zip archive.");
			}

			if (sourcePath.EndsWith(".gdb", StringComparison.InvariantCultureIgnoreCase))
			{
				throw new ArgumentException(
					"File Geodatabases are not supported. Please create a zip archive.");
			}

			if (File.GetAttributes(sourcePath).HasFlag(FileAttributes.Directory))
			{
				throw new ArgumentException(
					"File system directories are not supported. Please create a zip archive.");
			}

			AssertPath(sourcePath);

			// assume it's a file
			string target = Path.Combine(targetPath, Path.GetFileName(sourcePath));

			CopyFile(sourcePath, target, overwrite);

			return target;
		}

		private static void Extract([NotNull] string sourcePath,
		                            [NotNull] string targetPath,
		                            bool overwrite)
		{
			Assert.ArgumentNotNullOrEmpty(sourcePath, nameof(sourcePath));
			Assert.ArgumentNotNullOrEmpty(targetPath, nameof(targetPath));

			string target = Path.Combine(targetPath, Path.GetFileNameWithoutExtension(sourcePath));

			if (Exists(target))
			{
				if (! overwrite)
				{
					_msg.Debug($"{target} already exists");
					return;
				}

				FileSystemUtils.DeleteDirectory(targetPath, true, true);
				ZipFile.ExtractToDirectory(sourcePath, targetPath);

				return;
			}

			ZipFile.ExtractToDirectory(sourcePath, targetPath);
		}

		private static string GetFileGeodatabasePath(string testDataDirectory)
		{
			string[] directories = Directory.GetDirectories(testDataDirectory, "*.gdb");

			string path = directories.FirstOrDefault();

			// todo inline
			return path;
		}

		private static void CopyFile([NotNull] string source,
		                             [NotNull] string target,
		                             bool overwrite)
		{
			string dirPath = Path.GetDirectoryName(target);

			if (! Exists(dirPath) && ! string.IsNullOrEmpty(dirPath))
			{
				Assert.NotNull(Directory.CreateDirectory(dirPath));
			}

			File.Copy(source, target, overwrite);
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

		private static string GetTargetDirectory([NotNull] string projectName,
		                                         [CanBeNull] string testName)
		{
			Assert.ArgumentNotNullOrEmpty(projectName);

			string tempDataDir = Path.Combine(_tempUnitTestData, projectName);

			return string.IsNullOrEmpty(testName)
				       ? tempDataDir
				       : Path.Combine(tempDataDir, testName);
		}

		#endregion

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

			string repoRoot = Path.Combine(binDir, @"..\..\");

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
