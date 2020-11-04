using System.IO;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

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
		private readonly string _testDataRoot;
		private const string DefaultTestDataDir = "TestData";
		private const string DefaultRelativePath = @"..\..\src";

		public TestDataLocator(string relativePath = null, string testDataDirectory = null)
			: this(Assembly.GetCallingAssembly(), relativePath, testDataDirectory) { }

		private TestDataLocator([NotNull] Assembly assembly,
		                        string relativePath,
		                        string testDataDirectory)
		{
			var projectFolder = GetProjectDirectoryName(assembly);
			var fileInfo = new FileInfo(assembly.Location);
			var path = Assert.NotNull(fileInfo.Directory).FullName;

			path = Path.Combine(path, relativePath ?? DefaultRelativePath);
			path = Path.Combine(path, projectFolder);
			path = Path.Combine(path, testDataDirectory ?? DefaultTestDataDir);

			_testDataRoot = Path.GetFullPath(path);
		}

		public string GetPath(string fileName)
		{
			string path = Path.Combine(_testDataRoot, fileName);

			if (! File.Exists(path) && ! Directory.Exists(path))
			{
				throw new FileNotFoundException($"File or directory not found: {path}");
			}

			return Path.GetFullPath(path); // normalize relative components
		}

		#region Private utils

		[NotNull]
		private static string GetProjectDirectoryName([NotNull] Assembly assembly)
		{
			// Note: assumes same name for assembly and project directory

			var fileName = new FileInfo(assembly.Location).Name;

			return Assert.NotNull(Path.GetFileNameWithoutExtension(fileName),
			                      "assembly file name is null");
		}

		#endregion
	}
}
