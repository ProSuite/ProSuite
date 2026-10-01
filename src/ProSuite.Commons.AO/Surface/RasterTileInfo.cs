using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface
{
	public class RasterTileInfo
	{
		public RasterTileInfo([NotNull] IEnvelope extent,
		                      int width,
		                      int height,
		                      [CanBeNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(extent, nameof(extent));
			Assert.ArgumentCondition(width > 0, "Raster tile width must be greater than zero");
			Assert.ArgumentCondition(height > 0, "Raster tile height must be greater than zero");

			Extent = extent;
			Width = width;
			Height = height;
			SpatialReference = spatialReference;
		}

		[NotNull]
		public IEnvelope Extent { get; }

		public int Width { get; }

		public int Height { get; }

		[CanBeNull]
		public ISpatialReference SpatialReference { get; }
	}
}
