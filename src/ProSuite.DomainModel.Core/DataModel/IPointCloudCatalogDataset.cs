namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// A point cloud dataset backed by a catalog feature class: one polygon feature per tile, with
	/// the path to that tile's LAS file in a field. The point cloud counterpart of
	/// <see cref="IRasterCatalogDataset"/>.
	/// </summary>
	public interface IPointCloudCatalogDataset : IFileCatalogDataset, IPointCloudDataset { }
}
