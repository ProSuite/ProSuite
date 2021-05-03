#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Surface.Raster
{
	public static class RasterUtils
	{
		[NotNull]
		public static IRaster GetClippedRaster(
			[NotNull] IRaster sourceRaster,
			[NotNull] IEnvelope box,
			[CanBeNull] out IDataset memoryRasterDataset)
		{
			// Remark: 
			// surface values outside 'raster'.envelope (but within 'box') are 0

			Assert.ArgumentNotNull(sourceRaster, nameof(sourceRaster));
			Assert.ArgumentNotNull(box, nameof(box));

			IEnvelope clipBox = GetClipBox(box, sourceRaster);

			IRasterFunction rasterFunction = new ClipFunctionClass();
			IClipFunctionArguments functionArguments = new ClipFunctionArgumentsClass();
			functionArguments.Raster = sourceRaster;

			functionArguments.Extent = clipBox;
			functionArguments.ClippingGeometry = clipBox;
			functionArguments.ClippingType = esriRasterClippingType.esriRasterClippingOutside;

			IFunctionRasterDataset functionRasterDataset = new FunctionRasterDataset();
			functionRasterDataset.Init(rasterFunction, functionArguments);
			functionRasterDataset.RasterInfo.NoData = (float) -9999;

			// unsicher, wie die memory verwaltung ist

			// zum sicherstellen der memory-verwaltung
			// --> inmemory raster erstellen
			var save = (ISaveAs2) functionRasterDataset;
			IWorkspaceName wsName = WorkspaceUtils.CreateInMemoryWorkspace("raster");
			var ws = (IWorkspace) ((IName) wsName).Open();
			save.SaveAs("clipped", ws, "MEM");

			IRasterDataset rasterDataset =
				((IRasterWorkspace2) ws).OpenRasterDataset("clipped");

			IRaster rasterData = rasterDataset.CreateDefaultRaster();

			// Problems and workarounds for raster(-mosaic) data in surfaces
			// -------------------------------------------------------------
			// 
			// Extent completly within footprint of mosaic dataset: -> no NoDataValues -> no problems
			// Extent partly within footprint of mosaic dataset : -> double.NaN returned for values outside footprint -> no problems (TODO: verifiy)
			// Extent completely outside footprint of mosaic dataset, but within extent of mosaic data set: 
			//    0-values returned for Null-raster values 
			//    workaround: ((IRasterProps) rasterData).NoDataValue = (float)0; works, if ((IRasterProps) raster).NoDataValue = null and no Height == 0
			//    workaround to test: use CustomPixelFilter (see https://github.com/Esri/arcobjects-sdk-community-samples/tree/master/Net/Raster/CustomNodataFilter/CSharp
			// Extent completely outside extent of mosaic dataset 
			//    0-values returned for Null-raster values, rasterData.Height = 1, rasterData.Width = 1 
			//    workaround 1: use custom RasterSurface class.
			//    workaround 2: ((IRasterProps) rasterData).NoDataValue = (float)0; works, if ((IRasterProps) raster).NoDataValue = null and no Height == 0

			memoryRasterDataset = (IDataset) rasterDataset;

			return rasterData;
		}

		/// <summary>
		/// Bugfix for TGS-1119:
		/// Extend box, because raster points outside box may be assigned with 0 when using ClipFunctionClass in ArcGis 10.4
		/// (sides are depending of the difference between raster origin, cell size and box position)
		/// </summary>
		/// <param name="box"></param>
		/// <param name="raster"></param>
		/// <returns></returns>
		[NotNull]
		private static IEnvelope GetClipBox([NotNull] IEnvelope box,
		                                    [NotNull] IRaster raster)
		{
			IEnvelope result = GeometryFactory.Clone(box);

			WKSEnvelope wks;
			result.QueryWKSCoords(out wks);
			double expandX = 0;
			double expandY = 0;

			var props = raster as IRasterProps;
			if (props != null)
			{
				IPnt pnt = props.MeanCellSize();
				expandX = 2 * Math.Abs(pnt.X);
				expandY = 2 * Math.Abs(pnt.Y);
			}

			wks.XMin -= expandX;
			wks.XMax += expandX;
			wks.YMin -= expandY;
			wks.YMax += expandY;

			result.PutWKSCoords(wks);

			return result;
		}

		public static void ReleaseMemoryRasterDataset(
			[CanBeNull] IDataset disposableMemoryRasterDataset)
		{
			IWorkspace memoryWs = null;
			if (disposableMemoryRasterDataset != null)
			{
				memoryWs = disposableMemoryRasterDataset.Workspace;
				disposableMemoryRasterDataset.Delete();
				ComUtils.ReleaseComObject(disposableMemoryRasterDataset);
			}

			if (memoryWs != null)
			{
				((IDataset) memoryWs).Delete();
				ComUtils.ReleaseComObject(memoryWs);
			}
		}

		public static float GetNoDataValue(IRasterProps rasterProperties)
		{
			object noDataValueObj = rasterProperties.NoDataValue;

			if (noDataValueObj == null)
			{
				return float.MinValue;
			}

			float result = ((float[]) noDataValueObj)[0];

			return result;
		}
	}
}
