using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests
{
	internal interface IRasterTileExporter
	{
		[NotNull]
		RasterTileInfo ExportTile([NotNull] RasterDatasetReference raster,
		                          [NotNull] IEnvelope extent,
		                          [NotNull] string outputPath);
	}

	internal class RasterDatasetTileExporter : IRasterTileExporter
	{
		public RasterTileInfo ExportTile(RasterDatasetReference raster,
		                                 IEnvelope extent,
		                                 string outputPath)
		{
			return raster.ExportTileAsTiff(extent, outputPath);
		}
	}
}
