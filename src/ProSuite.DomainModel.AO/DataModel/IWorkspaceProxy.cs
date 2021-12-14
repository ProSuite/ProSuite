#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IWorkspaceProxy
	{
		[NotNull]
		IFeatureWorkspace FeatureWorkspace { get; }

		[NotNull]
		IWorkspace Workspace { get; }

		/// <summary>
		/// Opens the table.
		/// </summary>
		/// <param name="name">The table name.</param>
		/// <param name="oidFieldName">Name of the oid field to be used if the table is not registered with the geodatabase, and no OID field can be automatically determined.</param>
		/// <param name="spatialReferenceDescriptor">The spatial reference descriptor to be used if the table is not registered with the geodatabase, is a spatial dataset (query layer),
		/// but the spatial reference could not be automatically determined.</param>
		/// <returns></returns>
		[NotNull]
		ITable OpenTable([NotNull] string name,
		                 [CanBeNull] string oidFieldName = null,
		                 [CanBeNull] SpatialReferenceDescriptor spatialReferenceDescriptor = null);

		[NotNull]
		IFeatureClass OpenFeatureClass([NotNull] string name);

		[NotNull]
		IRelationshipClass OpenRelationshipClass([NotNull] string name);

		[NotNull]
		ITopology OpenTopology([NotNull] string name);

		[NotNull]
		IMosaicDataset OpenMosaicDataset([NotNull] string name);

		[NotNull]
		IRaster OpenRaster([NotNull] string name,
		                   [NotNull] Func<IWorkspace, string, IRaster> openRaster);

		[NotNull]
		IRasterDataset OpenRasterDataset(string name);
	}
}
