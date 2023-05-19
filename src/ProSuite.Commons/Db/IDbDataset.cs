namespace ProSuite.Commons.Db
{
	/// <summary>
	/// Provides access to methods of datasets that can reside in a database. Common interface for
	/// DDX datasets and geo database implementations.
	/// </summary>
	public interface IDbDataset
	{
		string Name { get; }

		IDbDatasetContainer DbContainer { get; }

		// Consider IName property that allows for extensions (queryName implementation with extra properties, etc.)

		DatasetType DatasetType { get; }

		bool Equals(IDbDataset otherDataset);
	}
}
