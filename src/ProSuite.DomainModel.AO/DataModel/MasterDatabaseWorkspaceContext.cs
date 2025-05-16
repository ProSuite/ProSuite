#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class MasterDatabaseWorkspaceContext : WorkspaceContextBase
	{
		[NotNull] private readonly DdxModel _model;
		[NotNull] private readonly IWorkspaceProxy _workspaceProxy;

		/// <summary>
		/// Initializes a new instance of the <see cref="MasterDatabaseWorkspaceContext"/> class.
		/// </summary>
		/// <param name="featureWorkspace">The feature workspace.</param>
		/// <param name="model">The model.</param>
		public MasterDatabaseWorkspaceContext([NotNull] IFeatureWorkspace featureWorkspace,
		                                      [NotNull] DdxModel model)
			: base(featureWorkspace)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));
			Assert.ArgumentNotNull(model, nameof(model));

			_model = model;

			_workspaceProxy = model.KeepDatasetLocks
				                  ? (IWorkspaceProxy) new CachedWorkspaceProxy(featureWorkspace)
				                  : new SimpleWorkspaceProxy(featureWorkspace);
		}

		public override bool CanOpen(IDdxDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return _model.Contains(dataset);
		}

		public override IObjectClass OpenObjectClass(IObjectDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			SpatialReferenceDescriptor spatialReferenceDescriptor =
				dataset.Model?.SpatialReferenceDescriptor;

			return (IObjectClass) _workspaceProxy.OpenTable(
				GetGdbElementName(dataset),
				dataset.GetAttribute(AttributeRole.ObjectID)?.Name,
				spatialReferenceDescriptor);
		}

		public override TopologyReference OpenTopology(ITopologyDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			ITopology topology = _workspaceProxy.OpenTopology(GetGdbElementName(dataset));

			return new TopologyReference(topology);
		}

		public override RasterDatasetReference OpenRasterDataset(IDdxRasterDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			return new RasterDatasetReference(
				_workspaceProxy.OpenRasterDataset(GetGdbElementName(dataset)));
		}

		public override TerrainReference OpenTerrainReference(ISimpleTerrainDataset dataset)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));

			IList<SimpleTerrainDataSource> terrainSources =
				ModelElementUtils.GetTerrainDataSources(dataset, OpenObjectClass);

			return new SimpleTerrain(dataset.Name, terrainSources, dataset.PointDensity, null);
		}

		public override MosaicRasterReference OpenSimpleRasterMosaic(IRasterMosaicDataset dataset)
		{
			IMosaicDataset mosaic = _workspaceProxy.OpenMosaicDataset(dataset.Name);

			var simpleRasterMosaic = new SimpleRasterMosaic(mosaic);

			return new MosaicRasterReference(simpleRasterMosaic);
		}

		public override IRelationshipClass OpenRelationshipClass(Association association)
		{
			Assert.ArgumentNotNull(association, nameof(association));

			return _workspaceProxy.OpenRelationshipClass(GetGdbElementName(association));
		}

		public override Dataset GetDatasetByGdbName(string gdbDatasetName)
		{
			Assert.ArgumentNotNullOrEmpty(gdbDatasetName, nameof(gdbDatasetName));

			return _model.GetDatasetByModelName(GetModelElementName(gdbDatasetName));
		}

		public override Dataset GetDatasetByModelName(string modelDatasetName)
		{
			Assert.ArgumentNotNullOrEmpty(modelDatasetName, nameof(modelDatasetName));

			return _model.GetDatasetByModelName(modelDatasetName);
		}

		public override Association GetAssociationByRelationshipClassName(
			string relationshipClassName)
		{
			return _model.GetAssociationByModelName(GetModelElementName(relationshipClassName));
		}

		public override Association GetAssociationByModelName(string associationName)
		{
			Assert.ArgumentNotNullOrEmpty(associationName, nameof(associationName));

			return _model.GetAssociationByModelName(associationName);
		}

		public override bool Contains(IDdxDataset dataset)
		{
			return _model.Contains(dataset);
		}

		public override bool Contains(Association association)
		{
			return _model.Contains(association);
		}

		public override string ToString()
		{
			return $"Master DB Workspace Context for {_model}";
		}

		[NotNull]
		private string GetModelElementName([NotNull] string gdbElementName)
		{
			Assert.ArgumentNotNullOrEmpty(gdbElementName, nameof(gdbElementName));

			// the master database context does not support any prefix mappings etc.

			return _model.TranslateToModelElementName(gdbElementName);
		}

		[NotNull]
		private string GetGdbElementName([NotNull] IModelElement modelElement)
		{
			Assert.ArgumentNotNull(modelElement, nameof(modelElement));

			return GetGdbElementName(modelElement.Name);
		}

		[NotNull]
		private string GetGdbElementName([NotNull] string modelElementName)
		{
			Assert.ArgumentNotNullOrEmpty(modelElementName, nameof(modelElementName));

			return ModelElementUtils.TranslateToMasterDatabaseDatasetName(modelElementName, _model);
		}
	}
}
