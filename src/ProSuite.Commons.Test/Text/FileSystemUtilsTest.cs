using System.Runtime.InteropServices;
using NUnit.Framework;
using ProSuite.Commons.IO;

namespace ProSuite.Commons.Test.Text
{
	[TestFixture]
	public class FileSystemUtilsTest
	{
		[Test]
		public void CanArePathsEqual()
		{
			bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			if (isWindows)
			{
				// Case shall be ignored on Windows
				Assert.IsTrue(FileSystemUtils.ArePathsEqual(@"C:\Foo\Bar.txt", @"c:\foo\bar.txt"));
			}
			else
			{
				// but shall be significant on non-Windows platforms
				Assert.IsFalse(FileSystemUtils.ArePathsEqual(@"C:\Foo\Bar.txt", @"c:\foo\bar.txt"));
			}

			Assert.IsFalse(FileSystemUtils.ArePathsEqual(@"C:\Foo\Bar.txt", @"C:\Foo\Baz.txt"));

			// Trailing directory separators shall be ignored:
			Assert.IsTrue(FileSystemUtils.ArePathsEqual(@"C:\My\Dir", @"C:\My\Dir\"));
			Assert.IsTrue(FileSystemUtils.ArePathsEqual(@"C:\My\Dir\", @"C:\My\Dir"));
			Assert.IsTrue(FileSystemUtils.ArePathsEqual("/home/me", "/home/me/"));
			Assert.IsTrue(FileSystemUtils.ArePathsEqual("/home/me/", "/home/me////"));

			// Paths with different directory separators shall be considered equal
			Assert.IsTrue(FileSystemUtils.ArePathsEqual(@"\My\Dir", "/My/Dir"));

			// For consistency with string.Equals(), null paths are considered equal
			Assert.IsTrue(FileSystemUtils.ArePathsEqual(null, null));
			Assert.IsFalse(FileSystemUtils.ArePathsEqual(null, "foo/bar"));
			Assert.IsFalse(FileSystemUtils.ArePathsEqual(string.Empty, null));
		}
	}
}
