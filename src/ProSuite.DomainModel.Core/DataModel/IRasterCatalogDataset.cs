using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// A raster mosaic DDX dataset that represents either a very basic raster catalog or a mosaic
	/// dataset with the additional properties, such as the boundary dataset, ZOrder field, etc.
	/// Even a plain vector dataset with a configured file-path field could serve the same role by
	/// implementing this interface in the future.
	/// </summary>
	public interface IRasterCatalogDataset : IRasterMosaicDataset
	{
		/// <summary>
		/// The feature class that catalogs the raster tiles (one feature per tile). This is the
		/// dataset itself in case of a basic raster catalog, but is modelled separately to keep the
		/// abstraction generic. The footprint class in case of a mosaic.
		/// </summary>
		[NotNull]
		IVectorDataset CatalogDataset { get; }

		/// <summary>
		/// Name of the field holding the path to each tile's raster file. Required.
		/// </summary>
		[NotNull]
		string FilePathFieldName { get; }

		/// <summary>
		/// Optional boundary polygon dataset. When null, the union of the catalog tiles is used as
		/// the interpolation domain.
		/// </summary>
		[CanBeNull]
		IVectorDataset BoundaryDataset { get; }

		/// <summary>
		/// Optional name of an integer field defining the Z-order of overlapping tiles. When null,
		/// no ordering is applied.
		/// </summary>
		[CanBeNull]
		string ZOrderFieldName { get; }

		/// <summary>
		/// Whether the ordering by <see cref="ZOrderFieldName"/> should be descending.
		/// </summary>
		bool ZOrderDescending { get; }

		/// <summary>
		/// Optional name of the field containing each tile's cell size. When null, the cell size is
		/// derived from the rasters.
		/// </summary>
		[CanBeNull]
		string CellSizeFieldName { get; }
	}
}
