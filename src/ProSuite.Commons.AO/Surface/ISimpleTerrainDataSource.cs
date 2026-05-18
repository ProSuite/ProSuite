using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// Common spatial metadata for any terrain data source, regardless of whether it is backed
	/// by a feature class, a LAS point cloud, or another geometry provider.
	/// Implementations may additionally implement <see cref="ITerrainPointSource" />-compatible
	/// interfaces for sources that yield mass points directly.
	/// </summary>
	public interface ISimpleTerrainDataSource
	{
		[NotNull]
		ISpatialReference SpatialReference { get; }

		[NotNull]
		IEnvelope Extent { get; }
	}
}
