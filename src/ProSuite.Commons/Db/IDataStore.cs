using System.Collections.Generic;

namespace ProSuite.Commons.Db
{
	public interface IDatasetContainer
	{
		T GetDataset<T>(string tableName) where T : class, IDatasetDef;

		IEnumerable<IDatasetDef> GetDatasetDefs(DatasetType ofType = DatasetType.Any);

		bool Equals(IDatasetContainer otherWorkspace);
	}

	public interface IDataStore : IDatasetContainer
	{
		ITableData OpenDataset(string tableName);

		WorkspaceDbType DbType { get; }

		/// <summary>
		/// The file system full path of the workspace.
		/// </summary>
		string Path { get; }

		// Consider ConnectionString in case there is no path?
	}
}
