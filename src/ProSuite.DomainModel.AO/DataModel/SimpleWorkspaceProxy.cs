#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class SimpleWorkspaceProxy : WorkspaceProxy
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleWorkspaceProxy"/> class.
		/// </summary>
		/// <param name="featureWorkspace">The feature workspace.</param>
		public SimpleWorkspaceProxy([NotNull] IFeatureWorkspace featureWorkspace)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));

			FeatureWorkspace = featureWorkspace;
		}

		public override IFeatureWorkspace FeatureWorkspace { get; }

		public override ITable OpenTable(string name,
		                                 string oidFieldName = null,
		                                 SpatialReferenceDescriptor spatialReferenceDescriptor =
			                                 null)
		{
			return ModelElementUtils.OpenTable(FeatureWorkspace,
			                                   name,
			                                   oidFieldName,
			                                   spatialReferenceDescriptor);
		}

		public override IRelationshipClass OpenRelationshipClass(string name)
		{
			return DatasetUtils.OpenRelationshipClass(FeatureWorkspace, name);
		}

		public override ITopology OpenTopology(string name)
		{
			return TopologyUtils.OpenTopology(FeatureWorkspace, name);
		}

		public override IMosaicDataset OpenMosaicDataset(string name)
		{
			return MosaicUtils.OpenMosaicDataset(Workspace, name);
		}

		public override IRasterDataset OpenRasterDataset(string name)
		{
			return DatasetUtils.OpenRasterDataset(Workspace, name);
		}
	}
}
