using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// A DDX dataset whose content is a set of spatially partitioned files, catalogued by a polygon
	/// feature class with one feature per tile and a field holding that tile's file path.
	/// <para>
	/// Content-agnostic on purpose: rasters, LAS point clouds and future imagery all need exactly
	/// "a tile-index feature class and the name of its file-path field". What kind of file the
	/// paths point to is expressed by the marker interface a catalog additionally implements
	/// (<see cref="IRasterCatalogDataset"/>, <see cref="IPointCloudCatalogDataset"/>), because that
	/// is what decides which test parameters the dataset may be bound to.
	/// </para>
	/// </summary>
	public interface IFileCatalogDataset : IDdxDataset
	{
		/// <summary>
		/// The feature class that catalogs the tiles (one feature per tile). This is the dataset
		/// itself for a basic catalog, but is modelled separately to keep the abstraction generic
		/// (it is the footprint class in case of an Esri mosaic).
		/// </summary>
		[NotNull]
		IVectorDataset CatalogDataset { get; }

		/// <summary>
		/// Name of the field holding the path to each tile's file. Required.
		/// </summary>
		[NotNull]
		string FilePathFieldName { get; }
	}
}
