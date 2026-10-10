namespace ProSuite.Commons.GeoDb
{
	/// <summary>
	/// Marker interface for point cloud datasets, i.e. a set of LAS files with a spatial
	/// partitioning. Tests / test definitions use this as a parameter type so that they can be
	/// instantiated on all platforms.
	/// </summary>
	public interface IPointCloudDatasetDef : IDatasetDef { }
}
