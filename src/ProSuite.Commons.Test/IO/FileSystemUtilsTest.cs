using System;
using System.IO;
using NUnit.Framework;
using ProSuite.Commons.IO;

namespace ProSuite.Commons.Test.IO
{
	[TestFixture]
	public class FileSystemUtilsTest
	{
		[Test]
		public void CanExpandPathVariables()
		{
			const string suffix = @"\test";

			Assert.AreEqual(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + suffix,
				FileSystemUtils.ExpandPathVariables("%localappdata%" + suffix));

			Assert.AreEqual(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + suffix,
				FileSystemUtils.ExpandPathVariables("${localappdata}" + suffix));

			Assert.AreEqual(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + suffix,
				FileSystemUtils.ExpandPathVariables("%appdata%" + suffix));

			Assert.AreEqual(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + suffix,
				FileSystemUtils.ExpandPathVariables("${appdata}" + suffix));

			Assert.AreEqual(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) +
				suffix,
				FileSystemUtils.ExpandPathVariables("${programdata}" + suffix));

			Assert.AreEqual(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) +
				suffix,
				FileSystemUtils.ExpandPathVariables("%programdata%" + suffix));
		}

		[Test]
		public void CanReplaceInvalidPathChars()
		{
			Console.WriteLine(FileSystemUtils.InvalidPathChars);

			const string invalidPath = @"C:\a<a\b|b\c>c\d""d";

			Assert.IsTrue(FileSystemUtils.HasInvalidPathChars(invalidPath));
			Assert.AreEqual(@"C:\a_a\b_b\c_c\d_d",
			                FileSystemUtils.ReplaceInvalidPathChars(invalidPath, '_'));
		}

		[Test]
		public void CanReplaceInvalidFileNameChars()
		{
			Console.WriteLine(FileSystemUtils.InvalidFileNameChars);

			const string invalidFileName = @"a""b:c:d/e";

			Assert.IsTrue(FileSystemUtils.HasInvalidFileNameChars(invalidFileName));

			Assert.AreEqual("a_b_c_d_e",
			                FileSystemUtils.ReplaceInvalidFileNameChars(invalidFileName, '_'));
		}

		[Test]
		public void CanExpandPathVariablesEmptyPath()
		{
			Assert.AreEqual(string.Empty,
			                FileSystemUtils.ExpandPathVariables(string.Empty));
		}

		[Test]
		public void CanExpandPathVariablesConstantPath()
		{
			const string path = @"c:\x\y\z.txt";
			Assert.AreEqual(path, FileSystemUtils.ExpandPathVariables(path));
		}

		[Test]
		public void CanMoveDirectory()
		{
			const string sourceDir = "C:\\temp\\MoveSourceTest";
			const string targetDir = "C:\\temp\\MoveTargetTest";

			Assert.True(Directory.Exists(sourceDir) == false, sourceDir + " exists");
			Assert.True(Directory.Exists(targetDir) == false, targetDir + " exists");

			Directory.CreateDirectory(sourceDir);

			string file0 = Path.Combine(sourceDir, "file0");
			FileStream f = File.Create(file0);
			f.Close();
			File.SetAttributes(file0, FileAttributes.ReadOnly);

			FileSystemUtils.MoveDirectory(sourceDir, targetDir);

			FileSystemUtils.DeleteDirectory(targetDir, true, true);
		}

		[Test, Ignore("Requires access to local test machine")]
		public void CanGetGetAvailableFreeBytes()
		{
			const string temp = "C:\\temp";
			const string tempWithBackslash = "C:\\temp\\";
			const string uncPath = @"\\coronet\data";

			Assert.True(Directory.Exists(temp));
			Assert.True(Directory.Exists(tempWithBackslash));
			Assert.True(Directory.Exists(uncPath));

			long freeBytes;
			double freeGigaBytes;
			Assert.True(FileSystemUtils.TryGetAvailableFreeBytes(temp, out freeBytes));
			Assert.True(freeBytes > 0);
			Assert.True(FileSystemUtils.TryGetAvailableFreeGigaBytes(temp, out freeGigaBytes));
			Assert.AreEqual(freeBytes / Math.Pow(1024, 3), freeGigaBytes);

			Assert.True(FileSystemUtils.TryGetAvailableFreeBytes(tempWithBackslash,
			                                                     out freeBytes));
			Assert.True(freeBytes > 0);
			Assert.True(FileSystemUtils.TryGetAvailableFreeGigaBytes(tempWithBackslash,
				            out freeGigaBytes));
			Assert.AreEqual(freeBytes / Math.Pow(1024, 3), freeGigaBytes);

			Assert.True(FileSystemUtils.TryGetAvailableFreeBytes(uncPath, out freeBytes));
			Assert.True(freeBytes > 0);
			Assert.True(FileSystemUtils.TryGetAvailableFreeGigaBytes(uncPath, out freeGigaBytes));
			Assert.AreEqual(freeBytes / Math.Pow(1024, 3), freeGigaBytes);
		}

		[Test]
		public void CanGetRelativePath()
		{
			string relativeBase = @"C:\temp\";
			string path = @"C:\temp\filegeodatabase.gdb";
			Assert.AreEqual(FileSystemUtils.GetRelativePath(relativeBase, path),
			                @"filegeodatabase.gdb");

			relativeBase = @"C:\temp\";
			path = @"C:\temp\data\filegeodatabase.gdb";
			Assert.AreEqual(FileSystemUtils.GetRelativePath(relativeBase, path),
			                @"data\filegeodatabase.gdb");

			relativeBase = @"C:\my temp\data\";
			path = @"C:\my temp\filegeodatabase.gdb";
			Assert.AreEqual(FileSystemUtils.GetRelativePath(relativeBase, path),
			                @"..\filegeodatabase.gdb");

			relativeBase = @"\\unc\data$\";
			path = @"\\unc\data$\filegeodatabase.gdb";
			Assert.AreEqual(FileSystemUtils.GetRelativePath(relativeBase, path),
			                "filegeodatabase.gdb");

			// Test for paths that are not related
			relativeBase = @"X:\anywhere\";
			path = @"C:\temp\filegeodatabase.gdb";
			Assert.AreEqual(FileSystemUtils.GetRelativePath(relativeBase, path), path);
			relativeBase = @"\\unc\data\";
			path = @"C:\temp\filegeodatabase.gdb";
			Assert.AreEqual(FileSystemUtils.GetRelativePath(relativeBase, path), path);
			relativeBase = @"C:\anywhere\";
			path = @"\\unc\data\filegeodatabase.gdb";
			Assert.AreEqual(FileSystemUtils.GetRelativePath(relativeBase, path), path);
		}
	}
}
