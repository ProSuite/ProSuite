#if Server
using ESRI.ArcGIS.DatasourcesRaster;
#else
using ESRI.ArcGIS.DataSourcesRaster;
#endif
using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class CachedWorkspaceProxy : WorkspaceProxy
	{
		[NotNull] private readonly Dictionary<string, ITable> _tablesByName =
			new Dictionary<string, ITable>(StringComparer.OrdinalIgnoreCase);

		[NotNull] private readonly Dictionary<string, ITopology> _topologiesByName =
			new Dictionary<string, ITopology>(StringComparer.OrdinalIgnoreCase);

		[NotNull] private readonly Dictionary<string, IRelationshipClass> _relClassesByName =
			new Dictionary<string, IRelationshipClass>(StringComparer.OrdinalIgnoreCase);

		[NotNull] private readonly Dictionary<string, IMosaicDataset> _mosaicDatasetsByName =
			new Dictionary<string, IMosaicDataset>(StringComparer.OrdinalIgnoreCase);

		[NotNull] private readonly Dictionary<string, IRasterDataset> _rasterDatasetsByName =
			new Dictionary<string, IRasterDataset>(StringComparer.OrdinalIgnoreCase);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedWorkspaceProxy"/> class.
		/// </summary>
		/// <param name="featureWorkspace">The workspace.</param>
		public CachedWorkspaceProxy([NotNull] IFeatureWorkspace featureWorkspace)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));

			FeatureWorkspace = featureWorkspace;
		}

		#endregion

		public override IFeatureWorkspace FeatureWorkspace { get; }

		public override ITable OpenTable(string name,
		                                 string oidFieldName = null,
		                                 SpatialReferenceDescriptor spatialReferenceDescriptor =
			                                 null)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			ITable table;
			if (! _tablesByName.TryGetValue(name, out table))
			{
				table = ModelElementUtils.OpenTable(FeatureWorkspace,
				                                    name,
				                                    oidFieldName,
				                                    spatialReferenceDescriptor);
				_tablesByName.Add(name, table);
			}

			return table;
		}

		public override IRelationshipClass OpenRelationshipClass(string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			IRelationshipClass relClass;
			if (! _relClassesByName.TryGetValue(name, out relClass))
			{
				relClass = DatasetUtils.OpenRelationshipClass(FeatureWorkspace, name);

				_relClassesByName.Add(name, relClass);
			}

			return relClass;
		}

		public override ITopology OpenTopology(string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			ITopology topology;
			if (! _topologiesByName.TryGetValue(name, out topology))
			{
				topology = TopologyUtils.OpenTopology(FeatureWorkspace, name);
				_topologiesByName.Add(name, topology);
			}

			return topology;
		}

		public override IMosaicDataset OpenMosaicDataset(string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			IMosaicDataset dataset;
			if (! _mosaicDatasetsByName.TryGetValue(name, out dataset))
			{
				dataset = MosaicUtils.OpenMosaicDataset((IWorkspace) FeatureWorkspace, name);
				_mosaicDatasetsByName.Add(name, dataset);
			}

			return dataset;
		}

		public override IRasterDataset OpenRasterDataset(string name)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			IRasterDataset dataset;
			if (! _rasterDatasetsByName.TryGetValue(name, out dataset))
			{
				dataset = DatasetUtils.OpenRasterDataset((IWorkspace) FeatureWorkspace, name);
				_rasterDatasetsByName.Add(name, dataset);
			}

			return dataset;
		}
	}
}
