using System.Collections.Generic;

namespace ProSuite.Commons.Db
{
	public interface IDbDatasetContainer
	{
		IDbTableSchema OpenTable(string tableName);

		IEnumerable<IDbDataset> GetGdbDatasets();

		bool Equals(IDbDatasetContainer otherWorkspace);
	}

	public interface IDataStore : IDbDatasetContainer
	{
		WorkspaceDbType DbType { get; }

		/// <summary>
		/// The file system full path of the workspace.
		/// </summary>
		string Path { get; }

		// Consider ConnectionString in case there is no path?
	}
}
