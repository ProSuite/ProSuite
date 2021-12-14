#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Logging;

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class WorkspaceProxy : IWorkspaceProxy
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public abstract IFeatureWorkspace FeatureWorkspace { get; }

		public IWorkspace Workspace => (IWorkspace) FeatureWorkspace;

		public abstract ITable OpenTable(string name,
		                                 string oidFieldName = null,
		                                 SpatialReferenceDescriptor spatialReferenceDescriptor =
			                                 null);

		public abstract IFeatureClass OpenFeatureClass(string name);

		public abstract IRelationshipClass OpenRelationshipClass(string name);

		public abstract ITopology OpenTopology(string name);

		public abstract IMosaicDataset OpenMosaicDataset(string name);

		public abstract IRasterDataset OpenRasterDataset(string name);

		public abstract IRaster OpenRaster(string name,
		                                   Func<IWorkspace, string, IRaster> openRaster);
	}
}
