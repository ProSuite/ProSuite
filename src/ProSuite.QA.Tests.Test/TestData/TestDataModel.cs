using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;
using ProSuite.QA.Tests.Test.TestRunners;

namespace ProSuite.QA.Tests.Test.TestData
{
	public class TestDataModel : MasterDatabaseDatasetContext, IDatasetContext
	{
		private readonly IFeatureWorkspace _workspace;
		private readonly SimpleModel _model;

		private const string _featureClassFootprints = "footprints";
		private const string _featureClassMasspoints = "masspoints";
		private const string _featureClassPoints = "points";
		private const string _featureClassPolylines = "lines";
		private const string _table = "table";
		private const string _rasterName = "raster";
		private const string _terrainName = "terrain";
		private const string _mosaicName = "mosaic";
		private const string _featureClassMultipatch = "multipatch";
		private const string _topology = "topology";

		/// <summary>
		/// Creates a new geodatabase with test data and its associated model.
		/// In-memory geodatabase is used by default, but it does not support topologies.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="inMemory"></param>
		public TestDataModel(string name,
		                     bool inMemory = true)
		{
			_workspace = CreateFullGeodatabase(name, inMemory);

			_model = new SimpleModel(name, (IWorkspace) _workspace);

			_model.AddDataset(new ModelVectorDataset(_featureClassMasspoints));
			_model.AddDataset(new ModelTableDataset(_table));
			_model.AddDataset(new ModelVectorDataset(_featureClassPoints));
			_model.AddDataset(new ModelVectorDataset(_featureClassPolylines));
			_model.AddDataset(new ModelVectorDataset(_featureClassFootprints));
			_model.AddDataset(new VerifiedRasterDataset(_rasterName));
			_model.AddDataset(new VerifiedRasterMosaicDataset(_mosaicName));
			_model.AddDataset(new VerifiedTopologyDataset(_topology));
			_model.AddDataset(new ModelVectorDataset(_featureClassMultipatch));
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

		public VectorDataset GetPointDataset()
		{
			return (VectorDataset) _model.GetDatasetByModelName(_featureClassPoints);
		}

		public VectorDataset GetVectorDataset()
		{
			return (VectorDataset) _model.GetDatasetByModelName(_featureClassPolylines);
		}

		public VectorDataset GetPolygonDataset()
		{
			return (VectorDataset) _model.GetDatasetByModelName(_featureClassFootprints);
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

		public VectorDataset GetMultipatchDataset()
		{
			return (VectorDataset) _model.GetDatasetByModelName(_featureClassMultipatch);
		}

		public TopologyDataset GetTopologyDataset()
		{
			return (TopologyDataset) _model.GetDatasetByModelName(_topology);
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

		private static IFeatureWorkspace CreateFullGeodatabase(string name,
		                                                       bool inMemory)
		{
			IFeatureWorkspace workspace = CreateWorkspace(name, inMemory);

			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				WellKnownHorizontalCS.LV95,
				WellKnownVerticalCS.LHN95);

			IFieldsEdit pointFields = new FieldsClass();
			pointFields.AddField(FieldUtils.CreateOIDField());
			pointFields.AddField(
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPoint, sr, 0D, true));
			pointFields.AddField(
				FieldUtils.CreateField("XCoordinate", esriFieldType.esriFieldTypeDouble));
			pointFields.AddField(
				FieldUtils.CreateField("YCoordinate", esriFieldType.esriFieldTypeDouble));
			pointFields.AddField(
				FieldUtils.CreateField("ZCoordinate", esriFieldType.esriFieldTypeDouble));

			DatasetUtils.CreateSimpleFeatureClass(workspace, _featureClassPoints, pointFields);

			IFieldsEdit lineFields = new FieldsClass();
			lineFields.AddField(FieldUtils.CreateOIDField());
			lineFields.AddField(
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryPolyline, sr, 0D, true,
				                            true));
			lineFields.AddField(
				FieldUtils.CreateField("MY_DATE_FIELD1", esriFieldType.esriFieldTypeDate));
			lineFields.AddField(
				FieldUtils.CreateField("MY_DATE_FIELD2", esriFieldType.esriFieldTypeDate));
			lineFields.AddField(
				FieldUtils.CreateField("MY_STRING_FIELD1", esriFieldType.esriFieldTypeString));
			lineFields.AddField(
				FieldUtils.CreateField("MY_STRING_FIELD2", esriFieldType.esriFieldTypeString));

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

			DatasetUtils.CreateSimpleFeatureClass(
				workspace, _featureClassMultipatch, null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateShapeField(esriGeometryType.esriGeometryMultiPatch, sr, 0, true));

			IFieldsEdit tableFields = new FieldsClass();
			tableFields.AddField(FieldUtils.CreateOIDField());

			DatasetUtils.CreateTable(workspace, _table, FieldUtils.CreateOIDField());

			IRasterWorkspace2 rasterWorkspace = (IRasterWorkspace2) workspace;

			IEnvelope dataEnvelope =
				GeometryFactory.CreateEnvelope(2600000, 1200000, 2610000, 1210000, sr);

			CreateElevationRasterDataset(rasterWorkspace, _rasterName, "TIFF", dataEnvelope, 100.0,
			                             sr);

			if (! inMemory)
			{
				// The FGDB supports the topology!
				IFeatureDataset featureDataset =
					DatasetUtils.CreateFeatureDataset(workspace, "TopoDataset", sr);

				ITopologyContainer topologyContainer = (ITopologyContainer) featureDataset;

				topologyContainer.CreateTopology(_topology, 0.01, 1000, null);
			}

			return workspace;
		}

		private static IFeatureWorkspace CreateWorkspace(string name,
		                                                 bool inMemory)
		{
			if (inMemory)
			{
				IWorkspaceName wsName = WorkspaceUtils.CreateInMemoryWorkspace(name);

				IFeatureWorkspace workspace = (IFeatureWorkspace) ((IName) wsName).Open();
				return workspace;
			}

			return TestWorkspaceUtils.CreateTestFgdbWorkspace(name);
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
