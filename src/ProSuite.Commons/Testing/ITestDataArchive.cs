using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Testing
{
	public interface ITestDataArchive : ITestData
	{
		string GetZipPath([CallerFilePath] [CanBeNull] string callerFilePath = null);

		ITestDataArchive ExtractTo([NotNull] string subDirName);

		ITestDataArchive Overwrite();

		string GetPath([CanBeNull] string fileName = null,
		               [CallerFilePath] [CanBeNull] string callerFilePath = null,
		               [CallerMemberName] [CanBeNull] string testName = null);
	}
}
