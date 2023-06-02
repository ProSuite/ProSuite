namespace ProSuite.Commons.GeoDb
{
	/// <summary>
	/// Provides access to methods of datasets that can reside in a database. Common interface for
	/// DDX datasets and geo database implementations.
	/// </summary>
	public interface IDatasetDef
	{
		string Name { get; }

		IDatasetContainer DbContainer { get; }

		// Consider IName property that allows for extensions (queryName implementation with extra properties, etc.)

		DatasetType DatasetType { get; }

		bool Equals(IDatasetDef otherDataset);
	}
}
