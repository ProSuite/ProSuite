using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.AO.Surface.Raster;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class WorkspaceContextBase : IWorkspaceContext
	{
		protected WorkspaceContextBase([NotNull] IFeatureWorkspace featureWorkspace)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));

			Workspace = (IWorkspace) featureWorkspace;
			FeatureWorkspace = featureWorkspace;
		}

		public IWorkspace Workspace { get; }

		public IFeatureWorkspace FeatureWorkspace { get; }

		public abstract bool CanOpen(IDdxDataset dataset);

		public IFeatureClass OpenFeatureClass(IVectorDataset dataset)
		{
			return (IFeatureClass) OpenObjectClass(dataset);
		}

		public ITable OpenTable(IObjectDataset dataset)
		{
			return (ITable) OpenObjectClass(dataset);
		}

		public abstract IObjectClass OpenObjectClass(IObjectDataset dataset);

		public abstract ITopology OpenTopology(ITopologyDataset dataset);

		public abstract IRasterDataset OpenRasterDataset(IDdxRasterDataset dataset);

		public abstract TerrainReference OpenTerrainReference(ISimpleTerrainDataset dataset);

		public abstract SimpleRasterMosaic OpenSimpleRasterMosaic(
			IRasterMosaicDataset dataset);

		public abstract IRelationshipClass OpenRelationshipClass(Association association);

		public abstract Dataset GetDatasetByGdbName(string gdbDatasetName);

		public abstract Dataset GetDatasetByModelName(string modelDatasetName);

		public abstract Association GetAssociationByRelationshipClassName(
			string relationshipClassName);

		public abstract Association GetAssociationByModelName(string associationName);

		public abstract bool Contains(IDdxDataset dataset);

		public abstract bool Contains(Association association);
	}
}
