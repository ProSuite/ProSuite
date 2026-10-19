using System;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.Callbacks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Surface
{
	public class RasterDatasetReference : RasterReference
	{
		[NotNull] private readonly IRasterDataset2 _rasterDataset;
		[CanBeNull] private IRaster _fullRaster;

		public RasterDatasetReference([NotNull] IRasterDataset rasterDataset)
		{
			Assert.ArgumentNotNull(rasterDataset, nameof(rasterDataset));

			_rasterDataset = (IRasterDataset2) rasterDataset;
		}

		public override IReadOnlyDataset Dataset => new ReadOnlyDataset((IDataset) _rasterDataset);

		public override IReadOnlyGeoDataset GeoDataset => new ReadOnlyGeoDataset(
			(IGeoDataset) _rasterDataset);

		public override double CellSize => RasterUtils.GetMeanCellSize(FullRaster);

		public override DatasetType DatasetType => DatasetType.Raster;

		private IRaster FullRaster =>
			_fullRaster
			?? (_fullRaster = _rasterDataset.CreateFullRaster());

		public override ISimpleSurface CreateSurface(IEnvelope extent,
		                                             double? defaultValueForUnassignedZs = null,
		                                             UnassignedZValueHandling?
			                                             unassignedZValueHandling = null)
		{
			IDataset memoryRasterDataset;

			IRaster clipped =
				RasterUtils.GetClippedRaster(FullRaster, extent, out memoryRasterDataset);

			IDataset disposableDataset = memoryRasterDataset;

			IDisposable disposableCallback =
				new DisposableCallback(
					() => RasterUtils.ReleaseMemoryRasterDataset(disposableDataset));

			var simpleRasterDataset = new SimpleRasterDataset(
				clipped, GeometryFactory.CreatePolygon(extent), disposableCallback);

			ISpatialReference spatialReference = ((IGeoDataset) FullRaster).SpatialReference;

			var surface = new SimpleRasterSurface(simpleRasterDataset, spatialReference);
			if (defaultValueForUnassignedZs.HasValue)
			{
				surface.DefaultValueForUnassignedZs = defaultValueForUnassignedZs.Value;
			}

			if (unassignedZValueHandling.HasValue)
			{
				surface.UnassignedZValueHandling = unassignedZValueHandling.Value;
			}

			return surface;
		}

		public RasterTileInfo ExportTileAsTiff([NotNull] IEnvelope extent,
		                                       [NotNull] string outputPath)
		{
			Assert.ArgumentNotNull(extent, nameof(extent));
			Assert.ArgumentNotNullOrEmpty(outputPath, nameof(outputPath));

			string directory = System.IO.Path.GetDirectoryName(outputPath);
			Assert.NotNullOrEmpty(directory, "Output directory is undefined");
			System.IO.Directory.CreateDirectory(directory);

			IDataset memoryRasterDataset = null;
			IDataset savedRasterDataset = null;
			IRasterWorkspace2 workspace = null;
			IRaster clipped = null;
			try
			{
				clipped = RasterUtils.GetClippedRaster(
					FullRaster, extent, out memoryRasterDataset);

				var rasterProps = (IRasterProps) clipped;
				rasterProps.SpatialReference = ((IGeoDataset) FullRaster).SpatialReference;
				IEnvelope rasterExtent = GeometryFactory.Clone(rasterProps.Extent);

				var saveAs = (ISaveAs2) memoryRasterDataset;
				workspace = WorkspaceUtils.OpenRasterWorkspace(directory);
				string name = System.IO.Path.GetFileName(outputPath);

				savedRasterDataset = saveAs.SaveAs(name, (IWorkspace) workspace, "TIFF");

				return new RasterTileInfo(rasterExtent, rasterProps.Width,
				                          rasterProps.Height,
				                          ((IGeoDataset) FullRaster).SpatialReference);
			}
			finally
			{
				ComUtils.ReleaseComObject(savedRasterDataset);
				ComUtils.ReleaseComObject(workspace);
				ComUtils.ReleaseComObject(clipped);
				RasterUtils.ReleaseMemoryRasterDataset(memoryRasterDataset);
			}
		}

		public override bool EqualsCore(RasterReference rasterReference)
		{
			var other = rasterReference as RasterDatasetReference;

			return other != null && _rasterDataset == other._rasterDataset;
		}

		public override int GetHashCodeCore()
		{
			return _rasterDataset.GetHashCode();
		}
	}
}
