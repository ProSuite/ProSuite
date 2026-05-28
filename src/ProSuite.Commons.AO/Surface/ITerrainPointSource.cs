using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// Abstraction over a mass-point source for TIN generation. Inputs and outputs use the
	/// source coordinate system. Coordinate transformation is the responsibility of the caller.
	/// </summary>
	public interface ITerrainPointSource
	{
		[NotNull]
		ISpatialReference SpatialReference { get; }

		[NotNull]
		IEnvelope Extent { get; }

		/// <summary>
		/// Estimates the number of points within the given area of interest in the source
		/// coordinate system. The estimate is based on file-header totals without applying
		/// classification filtering and is intentionally coarse — suitable for subdivision
		/// decisions only.
		/// </summary>
		long EstimatePointCount([NotNull] IEnvelope aoiInSourceSr);

		/// <summary>
		/// Returns all points from tiles whose extent intersects <paramref name="searchAreaInSourceSr" />.
		/// Points from outside the search area may be included when a tile only partially overlaps it.
		/// </summary>
		[NotNull]
		IEnumerable<(double x, double y, double z)> GetPoints(
			[NotNull] IEnvelope searchAreaInSourceSr,
			[CanBeNull] ITrackCancel trackCancel);
	}
}
