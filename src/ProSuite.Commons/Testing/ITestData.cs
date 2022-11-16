namespace ProSuite.Commons.Testing
{
	public interface ITestData
	{
		string GetTestDataDirectory(string callerFilePath);

		//string GetPath([CallerFilePath] [CanBeNull] string callerFilePath = null);
	}
}
