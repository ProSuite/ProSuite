using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test.TestData
{
	public class InMemoryTestDataModel : MasterDatabaseDatasetContext, IDatasetContext
	{
		private readonly IFeatureWorkspace _workspace;
		private readonly SimpleModel _model;

		private const string _featureClassFootprints = "footprints";
		private const string _featureClassMasspoints = "masspoints";
		private const string _featureClassPolylines = "lines";
		private const string _table = "table";
		private const string _rasterName = "raster";
		private const string _terrainName = "terrain";
		private const string _mosaicName = "mosaic";

		public InMemoryTestDataModel(string name)
		{
			_workspace = CreateInMemoryWorkspace(name);

			_model = new SimpleModel(name, (IWorkspace) _workspace);

			_model.AddDataset(new ModelVectorDataset(_featureClassMasspoints));
			_model.AddDataset(new ModelTableDataset(_table));
			_model.AddDataset(new ModelVectorDataset(_featureClassPolylines));
			_model.AddDataset(new ModelVectorDataset(_featureClassFootprints));
			_model.AddDataset(new VerifiedRasterDataset(_rasterName));
			_model.AddDataset(new VerifiedRasterMosaicDataset(_mosaicName));
			_model.AddDataset(CreateTerrainDataset());
		}

		#region IDatasetContext override -> Mock the mosaic

		public new MosaicRasterReference OpenSimpleRasterMosaic(IRasterMosaicDataset dataset)
		{
			// Use polygon feature class as 'catalog'
			VectorDataset footprints =
				_model.GetDatasetByModelName(_featureClassFootprints) as VectorDataset;

			Assert.NotNull(footprints);

			IFeatureClass footprintClass = OpenFeatureClass(footprints);

			Assert.NotNull(footprintClass);

			SimpleRasterMosaic simpleMosaic = new SimpleRasterMosaic(
				dataset.Name, footprintClass, null, "ZORDER", false, "RASTER", null);

			return new MosaicRasterReference(simpleMosaic);
		}

		#endregion

		public VectorDataset GetVectorDataset()
		{
			return (VectorDataset) _model.GetDatasetByModelName(_featureClassPolylines);
		}

		public TableDataset GetObjectDataset()
		{
			return (TableDataset) _model.GetDatasetByModelName(_table);
		}

		public ISimpleTerrainDataset GetTerrainDataset()
		{
			return (ISimpleTerrainDataset) _model.GetDatasetByModelName(_terrainName);
		}

		public RasterDataset GetRasterDataset()
		{
			return (RasterDataset) _model.GetDatasetByModelName(_rasterName);
		}

		public RasterMosaicDataset GetMosaicDataset()
		{
			return (RasterMosaicDataset) _model.GetDatasetByModelName(_mosaicName);
		}

		private SimpleTerrainDataset CreateTerrainDataset()
		{
			VectorDataset masspointDataset =
				(VectorDataset) _model.GetDatasetByModelName(_featureClassMasspoints);

			Assert.NotNull(masspointDataset);

			var sourceDataset =
				new TerrainSourceDataset(masspointDataset,
				                         TinSurfaceType.MassPoint);

			SimpleTerrainDataset terrainDataset =
				new ModelSimpleTerrainDataset(_terrainName, new[] { sourceDataset })
				{
					PointDensity = 1
				};

			return terrainDataset;
		}

		private static IFeatureWorkspace CreateInMemoryWorkspace(string name)
		{
			IWorkspaceName wsName = WorkspaceUtils.CreateInMemoryWorkspace(name);

			IFeatureWorkspace workspace = (IFeatureWorkspace) ((IName) wsName).Open();

			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LHN95);

			IFieldsEdit lineFields = new FieldsClass();
			lineFields.AddField(FieldUtils.CreateOIDField());
			lineFields.AddField(
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolyline, sr));

			DatasetUtils.CreateSimpleFeatureClass(workspace, _featureClassPolylines,
			                                      lineFields);

			DatasetUtils.CreateSimpleFeatureClass(
				workspace, _featureClassFootprints, null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolygon, sr));

			DatasetUtils.CreateSimpleFeatureClass(
				workspace, _featureClassMasspoints, null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryMultipoint, sr));

			IFieldsEdit tableFields = new FieldsClass();
			tableFields.AddField(FieldUtils.CreateOIDField());

			DatasetUtils.CreateTable(workspace, _table, FieldUtils.CreateOIDField());

			IRasterWorkspace2 rasterWorkspace = (IRasterWorkspace2) workspace;

			IEnvelope dataEnvelope =
				GeometryFactory.CreateEnvelope(2600000, 1200000, 2610000, 1210000, sr);

			CreateElevationRasterDataset(rasterWorkspace, _rasterName, "TIFF", dataEnvelope, 100.0,
			                             sr);

			return workspace;
		}

		private static IRasterDataset CreateElevationRasterDataset(
			[NotNull] IRasterWorkspace2 rasterWorkspace,
			[NotNull] string name,
			[NotNull] string rasterFormat,
			[NotNull] IEnvelope extent,
			double cellSize,
			[NotNull] ISpatialReference spatialReference)
		{
			var columns = (int) Math.Round(extent.Width / cellSize);
			var rows = (int) Math.Round(extent.Height / cellSize);

			const int bandCount = 1;
			const rstPixelType pixelType = rstPixelType.PT_FLOAT;

			IRasterDataset result = rasterWorkspace.CreateRasterDataset(
				name, rasterFormat, extent.LowerLeft, columns, rows, cellSize, cellSize,
				bandCount,
				pixelType, spatialReference);

			return result;
		}
	}
}
