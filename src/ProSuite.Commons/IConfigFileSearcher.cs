using System.Collections.Generic;

namespace ProSuite.Commons
{
	public interface IConfigFileSearcher
	{
		/// <summary>
		/// Get the full path to the given <paramref name="configFileName"/>.
		/// If this file is not found amongst the search paths, throw an error
		/// (if <paramref name="required"/> is true) and return null otherwise.
		/// </summary>
		string GetConfigFilePath(string configFileName, bool required = true);

		/// <summary>
		/// The sequence of search paths consulted for locating a config file.
		/// </summary>
		IEnumerable<string> GetSearchPaths();
	}
}
