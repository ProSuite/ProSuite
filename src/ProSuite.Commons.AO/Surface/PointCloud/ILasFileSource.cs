using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface.PointCloud
{
	/// <summary>
	/// Abstracts over different LAS file container types (LasDatasetDef JSON, Esri .lasd,
	/// a point cloud catalog feature class, and single .las/.laz files).
	/// </summary>
	/// <remarks>
	/// The LAS counterpart of <see cref="Raster.IRasterProvider"/>, and it lives next to it for the
	/// same reason: quality tests must be able to consume it, and they see nothing above
	/// ProSuite.Commons.AO.
	/// </remarks>
	public interface ILasFileSource
	{
		[NotNull]
		ISpatialReference SpatialReference { get; }

		[NotNull]
		IEnvelope Extent { get; }

		/// <summary>
		/// Returns (filePath, tileExtent) pairs whose tile extent intersects the given search area.
		/// </summary>
		[NotNull]
		IEnumerable<(string FilePath, IEnvelope TileExtent)> GetIntersectingFiles(
			[NotNull] IEnvelope searchArea);

		/// <summary>
		/// Returns all (filePath, tileExtent) pairs in the source regardless of location.
		/// </summary>
		[NotNull]
		IEnumerable<(string FilePath, IEnvelope TileExtent)> GetAllFiles();
	}
}
