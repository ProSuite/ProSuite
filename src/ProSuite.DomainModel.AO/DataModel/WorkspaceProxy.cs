using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.DomainModel.Core.DataModel;
#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class WorkspaceProxy : IWorkspaceProxy
	{
		public abstract IFeatureWorkspace FeatureWorkspace { get; }

		public IWorkspace Workspace => (IWorkspace) FeatureWorkspace;

		public abstract ITable OpenTable(
			string name,
			string oidFieldName = null,
			SpatialReferenceDescriptor spatialReferenceDescriptor = null,
			esriGeometryType knownGeometryType = esriGeometryType.esriGeometryNull);

		public abstract IRelationshipClass OpenRelationshipClass(string name);

		public abstract ITopology OpenTopology(string name);

		public abstract IMosaicDataset OpenMosaicDataset(string name);

		public abstract IRasterDataset OpenRasterDataset(string name);
	}
}
