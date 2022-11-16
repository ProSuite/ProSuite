using System;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using ProSuite.Commons.Testing;

namespace ProSuite.Commons.Test.Testing
{
	[TestFixture]
	public class TestDataPreparerTest
	{
		private const string BasePath = @"C:\temp\UnitTestData\ProSuite.Commons.Test";

		[Test]
		public void GetResultPath__overwrite__archive_and_file_name_same()
		{
			var archive = "FileGDB.gdb.zip";
			var gdb = "FileGDB.gdb";
			string expected =
				Path.Combine(BasePath, nameof(GetResultPath__overwrite__archive_and_file_name_same),
				             gdb);

			if (Directory.Exists(expected))
			{
				Directory.Delete(expected, true);
			}

			TestDataPreparer.ExtractZip(archive).GetPath();

			string path = TestDataPreparer.ExtractZip(archive).Overwrite().GetPath();

			Assert.True(Directory.Exists(path));
			Assert.AreEqual(expected, path);

			path = TestDataPreparer.ExtractZip(archive).Overwrite().GetPath();

			Assert.True(Directory.Exists(path));
			Assert.AreEqual(expected, path);
		}

		[Test]
		public void GetResultPath__overwrite__archive_and_file_name_different()
		{
			var archive = "foo.zip";
			var file = "bar.txt";
			string expected =
				Path.Combine(
					BasePath,
					$"{nameof(GetResultPath__overwrite__archive_and_file_name_different)}", file);

			if (File.Exists(expected))
			{
				File.Delete(expected);
			}

			string path = TestDataPreparer.ExtractZip(archive).Overwrite().GetPath(file);

			Assert.True(File.Exists(path));
			Assert.AreEqual(expected, path);

			path = TestDataPreparer.ExtractZip(archive).Overwrite().GetPath(file);

			Assert.True(File.Exists(path));
			Assert.AreEqual(expected, path);
		}

		[Test]
		public void GetResultPath__cannot_find_file()
		{
			var archive = "FileGDB.gdb.zip";
			var gdb = "_different_name_.gdb";
			string notExisting =
				Path.Combine(BasePath, nameof(GetResultPath__cannot_find_file), gdb);

			if (Directory.Exists(notExisting))
			{
				Directory.Delete(notExisting, true);
			}

			string path = TestDataPreparer.ExtractZip(archive).GetPath();

			Assert.True(Directory.Exists(path));
			Assert.AreNotEqual(notExisting, path);
			Assert.AreEqual(
				Path.Combine(BasePath, nameof(GetResultPath__cannot_find_file), "FileGDB.gdb"),
				path);

			path = TestDataPreparer.ExtractZip(archive).GetPath();

			Assert.True(Directory.Exists(path));
			Assert.AreNotEqual(notExisting, path);
			Assert.AreEqual(
				Path.Combine(BasePath, nameof(GetResultPath__cannot_find_file), "FileGDB.gdb"),
				path);
		}

		[Test]
		public void GetResultPath__archive_and_file_name_same()
		{
			var archive = "FileGDB.gdb.zip";
			var gdb = "FileGDB.gdb";
			string expectedDir =
				Path.Combine(BasePath, nameof(GetResultPath__archive_and_file_name_same), gdb);

			if (Directory.Exists(expectedDir))
			{
				Directory.Delete(expectedDir, true);
			}

			string path = TestDataPreparer.ExtractZip(archive).GetPath();

			Assert.True(Directory.Exists(path));
			Assert.AreEqual(expectedDir, path);

			path = TestDataPreparer.ExtractZip(archive).GetPath();

			Assert.True(Directory.Exists(path));
			Assert.AreEqual(expectedDir, path);
		}

		[Test]
		public void GetResultPath__archive_and_file_name_different()
		{
			var archive = "foo.zip";
			var file = "bar.txt";
			string expected =
				Path.Combine(BasePath, $"{nameof(GetResultPath__archive_and_file_name_different)}",
				             file);

			if (File.Exists(expected))
			{
				File.Delete(expected);
			}

			string path = TestDataPreparer.ExtractZip(archive).GetPath(file);

			Assert.True(File.Exists(path));
			Assert.AreEqual(expected, path);

			path = TestDataPreparer.ExtractZip(archive).GetPath(file);

			Assert.True(File.Exists(path));
			Assert.AreEqual(expected, path);
		}

		[Test]
		public void GetResultPath__of_directory()
		{
			var directory = @"Geom";
			string expected = GetExpectedPath(directory);

			string path = TestDataPreparer.FromDirectory().GetPath(directory);

			Assert.True(Directory.Exists(path));
			Assert.AreEqual(expected, path);
		}

		[Test]
		public void GetResultPath__of_archive_with_relative_path()
		{
			var archive = "foo.zip";
			var file = "bar.txt";
			string expected =
				Path.Combine(BasePath, $"{nameof(GetResultPath__of_archive_with_relative_path)}",
				             file);

			string path = TestDataPreparer.ExtractZip(archive, @"TestData\Geom").Overwrite()
			                              .GetPath(file);

			Assert.True(File.Exists(path));
			Assert.AreEqual(expected, path);
		}

		[Test]
		public void GetFilePath__of_archive()
		{
			var archive = "foo.zip";
			var file = "bar.txt";
			string expected = GetExpectedPath(file);

			try
			{
				TestDataPreparer.FromDirectory(archive).GetPath(file);
			}
			catch (ArgumentException e)
			{
				Console.WriteLine(e.Message);
			}

			string path = TestDataPreparer.FromDirectory().GetPath(file);

			Assert.True(File.Exists(path));
			Assert.AreEqual(expected, path);
		}

		[Test]
		public void GetFilePath__of_file()
		{
			var file = "union_multipart_touching_target.wkb";
			string expected = GetExpectedPath(file, "Geom");

			string path = TestDataPreparer.FromDirectory(@"TestData\Geom").GetPath(file);

			Assert.True(File.Exists(path));
			Assert.AreEqual(expected, path);
		}

		[Test]
		public void GetPath_of_directory()
		{
			string expected = GetExpectedPath("Geom");

			string path = TestDataPreparer.FromDirectory(@"TestData\Geom").GetDirectoryPath();

			Assert.True(Directory.Exists(path));
			Assert.AreEqual(expected, path);
		}

		[Test]
		public void GetPath_of_archive()
		{
			var archive = "foo.zip";
			string expected = GetExpectedPath(archive);

			string path = TestDataPreparer.ExtractZip(archive).GetZipPath();

			Assert.True(File.Exists(path));
			Assert.AreEqual(expected, path);
		}

		[Test]
		public void GetFilePath__of_directory()
		{
			var directory = @"Geom";

			try
			{
				TestDataPreparer.FromDirectory().GetPath(directory);
			}
			catch (ArgumentException e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private static string GetExpectedPath(string fileName, string subDir = null,
		                                      [CallerFilePath] string callerFilePath = null)
		{
			string testDataDir =
				TestDataPreparer.FromDirectory().GetTestDataDirectory(callerFilePath);

			return string.IsNullOrEmpty(subDir)
				       ? Path.Combine(testDataDir, fileName)
				       : Path.Combine(testDataDir, subDir, fileName);
		}
	}
}
