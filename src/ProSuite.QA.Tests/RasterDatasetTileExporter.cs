using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
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

		/// <summary>
		/// A mosaic is exported through its raster dataset.
		/// </summary>
		[NotNull]
		public static RasterDatasetReference GetRasterDatasetReference(
			[NotNull] RasterReference rasterReference)
		{
			if (rasterReference is RasterDatasetReference rasterDatasetReference)
			{
				return rasterDatasetReference;
			}

			if (rasterReference is MosaicRasterReference)
			{
				IRasterDataset rasterDataset = DatasetUtils.OpenRasterDataset(
					rasterReference.Dataset.Workspace, rasterReference.Name);
				return new RasterDatasetReference(rasterDataset);
			}

			throw new ArgumentException(
				$"Unsupported image raster type {rasterReference.GetType().Name}",
				nameof(rasterReference));
		}
	}
}
