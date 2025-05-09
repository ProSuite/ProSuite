using System;
using System.Collections.Generic;
using System.IO;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainServices.AO.Test.QA
{
	public class TestDatasetContext : IDatasetContext
	{
		private readonly IFeatureWorkspace _workspace;

		public TestDatasetContext([NotNull] string connection, object factoryId = null)
			: this(GetWorkspace(connection, factoryId)) { }

		public TestDatasetContext([NotNull] IFeatureWorkspace workspace)
		{
			Assert.ArgumentNotNull(workspace, nameof(workspace));

			_workspace = workspace;
		}

		private static IFeatureWorkspace GetWorkspace(string connection, object factoryId)
		{
			string ext = Path.GetExtension(connection);

			if (ext == ".gdb")
			{
				return WorkspaceUtils.OpenFileGdbFeatureWorkspace(
					connection);
			}

			return (IFeatureWorkspace) WorkspaceUtils.OpenWorkspace(connection, $"{factoryId}");
		}

		protected IFeatureWorkspace Workspace => _workspace;

		public bool CanOpen(IDdxDataset dataset)
		{
			return true;
		}

		public IFeatureClass OpenFeatureClass(IVectorDataset dataset)
		{
			return DatasetUtils.OpenFeatureClass(_workspace, dataset.Name);
		}

		public ITable OpenTable(IObjectDataset dataset)
		{
			return DatasetUtils.OpenTable(_workspace, dataset.Name);
		}

		public IObjectClass OpenObjectClass(IObjectDataset dataset)
		{
			return DatasetUtils.OpenObjectClass(_workspace, dataset.Name);
		}

		public TerrainReference OpenTerrainReference(ISimpleTerrainDataset dataset)
		{
			IList<SimpleTerrainDataSource> terrainSources =
				ModelElementUtils.GetTerrainDataSources(dataset, OpenObjectClass);

			return new SimpleTerrain(dataset.Name, terrainSources, dataset.PointDensity, null);
		}

		public virtual MosaicRasterReference OpenSimpleRasterMosaic(IRasterMosaicDataset dataset)
		{
			throw new NotImplementedException();
		}

		public TopologyReference OpenTopology(ITopologyDataset dataset)
		{
			ITopology topology = TopologyUtils.OpenTopology(_workspace, dataset.Name);

			return new TopologyReference(topology);
		}

		public RasterDatasetReference OpenRasterDataset(IDdxRasterDataset dataset)
		{
			IRasterDataset rasterDataset =
				DatasetUtils.OpenRasterDataset((IWorkspace) _workspace, dataset.Name);

			return new RasterDatasetReference(rasterDataset);
		}

		public IRelationshipClass OpenRelationshipClass(Association association)
		{
			return DatasetUtils.OpenRelationshipClass(_workspace, association.Name);
		}
	}
}
