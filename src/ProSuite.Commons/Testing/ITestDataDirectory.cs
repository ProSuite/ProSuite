using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Testing
{
	public interface ITestDataDirectory : ITestData
	{
		string GetDirectoryPath([CallerFilePath] [CanBeNull] string callerFilePath = null);

		string GetPath([NotNull] string fileName,
		               [CallerFilePath] [CanBeNull] string callerFilePath = null);

		IEnumerable<string> GetPaths([NotNull] IEnumerable<string> fileNames,
		                             [CallerFilePath] [CanBeNull] string callerFilePath = null);
	}
}
