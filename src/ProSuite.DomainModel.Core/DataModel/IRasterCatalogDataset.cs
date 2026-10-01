using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// A raster mosaic DDX dataset that represents either a very basic raster catalog or a mosaic
	/// dataset with the additional properties, such as the boundary dataset, ZOrder field, etc.
	/// Even a plain vector dataset with a configured file-path field could serve the same role by
	/// implementing this interface in the future.
	/// <para>
	/// The catalog and the file-path field are inherited from <see cref="IFileCatalogDataset"/>,
	/// which a point cloud catalog carries too; only the raster-specific extras are declared here.
	/// </para>
	/// </summary>
	public interface IRasterCatalogDataset : IFileCatalogDataset, IRasterMosaicDataset
	{
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
