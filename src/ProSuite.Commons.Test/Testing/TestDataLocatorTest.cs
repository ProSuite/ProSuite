using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using ProSuite.Commons.IO;
using ProSuite.Commons.Testing;

namespace ProSuite.Commons.Test.Testing
{
	[TestFixture]
	public class TestDataLocatorTest
	{
		const string BasePath = @"C:\temp\UnitTestData\ProSuite.Commons.Test";

		[Test]
		public void Can_copy_test_data_and_get_path()
		{
			TryDelete(BasePath);

			string fileName = "FileGDB.gdb.zip";
			string result = "FileGDB.gdb";

			string path = TestDataLocator.Prepare(fileName);
			Assert.True(Directory.Exists(path));
			Assert.AreEqual(Path.Combine(BasePath, nameof(Can_copy_test_data_and_get_path), result),
			                path);

			TryDelete(BasePath);

			fileName = "FileGDB.zip";
			path = TestDataLocator.Prepare(fileName);
			Assert.True(Directory.Exists(path));
			Assert.AreEqual(Path.Combine(BasePath, nameof(Can_copy_test_data_and_get_path), result),
			                path);
		}

		[Test]
		public void Can_copy_test_data_with_relative_path_and_get_path()
		{
			var fileName = "almost_linear_intersection_target.wkb";

			TryDelete(BasePath);

			string path = TestDataLocator.Prepare(fileName, @"TestData\Geom");
			Assert.True(File.Exists(path));
			Assert.AreEqual(
				Path.Combine(BasePath, nameof(Can_copy_test_data_with_relative_path_and_get_path),
				             fileName), path);
		}

		[Test]
		public void Can_throw_exception_if_it_is_FGDB()
		{
			TryDelete(BasePath);

			try
			{
				TestDataLocator.Prepare("FileGDB.gdb");
			}
			catch (ArgumentException e)
			{
				Console.WriteLine(e);
			}
		}

		[Test]
		public void Can_throw_exception_if_it_is_directory()
		{
			TryDelete(BasePath);

			try
			{
				TestDataLocator.Prepare("Geom", "TestData");
			}
			catch (ArgumentException e)
			{
				Console.WriteLine(e);
			}
		}

		[Test]
		public void Can_throw_exception_when_file_is_not_found()
		{
			TryDelete(BasePath);

			try
			{
				TestDataLocator.Prepare("almost_linear_intersection_target.wkb");
			}
			catch (FileNotFoundException e)
			{
				Console.Write(e);
			}

			Console.WriteLine("----------------------");

			try
			{
				TestDataLocator.Prepare("does_not_exist.wkb", "TestData");
			}
			catch (FileNotFoundException e)
			{
				Console.Write(e);
			}
		}

		[Test]
		public void Can_throw_exception_when_directory_is_not_found()
		{
			TryDelete(BasePath);

			try
			{
				TestDataLocator.Prepare("almost_linear_intersection_target.wkb",
				                        @"does_not_exist\somewhere");
			}
			catch (DirectoryNotFoundException e)
			{
				Console.Write(e);
			}
		}

		private static void TryDelete(string path)
		{
			if (! Directory.Exists(path))
			{
				return;
			}

			FileSystemUtils.DeleteDirectory(path, recursive: true, force: true);
			Thread.Sleep(500); // give him some time..
		}
	}
}
