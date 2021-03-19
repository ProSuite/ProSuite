#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Reflection;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Container
{
	internal class RasterRow : ISurfaceRow, IDataReference
	{
		[NotNull] private readonly ITestProgress _testProgress;

		[NotNull] private readonly IRaster _raster;

		[CanBeNull] private ISimpleSurface _rasterSurface;
		[CanBeNull] private IDataset _memoryRasterDataset;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TerrainRow"/> class.
		/// </summary>
		/// <param name="box">The box.</param>
		/// <param name="rasterReference">The dynamic surface.</param>
		/// <param name="raster">= rasterDataset.CreateFullRaster()</param>
		/// <param name="resolution">The resolution.</param>
		/// <param name="testProgress">The test progress reporting instance.</param>
		internal RasterRow([NotNull] IEnvelope box,
		                   [NotNull] RasterReference rasterReference,
		                   [NotNull] IRaster raster,
		                   double resolution,
		                   [NotNull] ITestProgress testProgress)
		{
			Assert.ArgumentNotNull(box, nameof(box));
			Assert.ArgumentNotNull(rasterReference, nameof(box));
			Assert.ArgumentNotNull(raster, nameof(raster));
			Assert.ArgumentNotNull(testProgress, nameof(testProgress));

			Extent = box;
			RasterReference = rasterReference;
			_raster = raster;

			_testProgress = testProgress;
			DatasetName = Assert.NotNull(rasterReference.RasterDataset.Name);
		}

		#endregion

		[NotNull]
		public RasterReference RasterReference { get; }

		public IEnvelope Extent { get; }

		public string DatasetName { get; }

		public string GetDescription()
		{
			return DatasetName;
		}

		public string GetLongDescription()
		{
			return GetDescription();
		}

		public ISimpleSurface Surface => RasterSurface;

		public bool HasLoadedSurface => _rasterSurface != null;

		public int Execute(ContainerTest containerTest, int occurance, out bool applicable)
		{
			int rasterIndex = containerTest.GetRasterIndex(RasterReference, occurance);
			applicable = true;
			return containerTest.Execute(this, rasterIndex);
		}

		public void DisposeSurface()
		{
			if (_rasterSurface == null)
			{
				return;
			}

			_msg.Debug("Disposing raster");

			_rasterSurface.Dispose();
			_rasterSurface = null;

			IWorkspace memoryWs = null;
			if (_memoryRasterDataset != null)
			{
				memoryWs = _memoryRasterDataset.Workspace;
				_memoryRasterDataset.Delete();
				ComUtils.ReleaseComObject(_memoryRasterDataset);
				_memoryRasterDataset = null;
			}

			if (memoryWs != null)
			{
				((IDataset) memoryWs).Delete();
				ComUtils.ReleaseComObject(memoryWs);
			}
		}

		[NotNull]
		private ISimpleSurface RasterSurface
		{
			get
			{
				if (_rasterSurface == null)
				{
					_msg.Debug("Getting rastersurface for tile");
					using (_testProgress.UseProgressWatch(Step.RasterLoading,
					                                      Step.RasterLoaded,
					                                      0, 1, RasterReference))
					{
						_rasterSurface = GetRasterSurface(_raster, Extent,
						                                  out _memoryRasterDataset);
					}
				}

				return _rasterSurface;
			}
		}

		[NotNull]
		private ISimpleSurface GetRasterSurface(
			[NotNull] IRaster sourceRaster,
			[NotNull] IEnvelope box,
			[CanBeNull] out IDataset memoryRasterDataset)
		{
			// Remark: 
			// surface values outside 'raster'.envelope (but within 'box') are 0

			Assert.ArgumentNotNull(sourceRaster, nameof(sourceRaster));
			Assert.ArgumentNotNull(box, nameof(box));


			IEnvelope clipBox = GetClipBox(box, sourceRaster);

			if (sourceRaster is ISimpleSurfaceProvider provider)
			{
				memoryRasterDataset = null;
				return provider.GetSurface(clipBox);
			}

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

			IRasterDataset rasterDataset = ((IRasterWorkspace2) ws).OpenRasterDataset("clipped");

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

			ISimpleSurface rasterSurface = RasterReference.CreateSurface(rasterData);

			return rasterSurface;
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
	}
}
