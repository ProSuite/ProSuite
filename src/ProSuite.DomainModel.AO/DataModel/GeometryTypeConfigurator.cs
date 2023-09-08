using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public class GeometryTypeConfigurator : IGeometryTypeConfigurator
	{
		[NotNull] private readonly IList<GeometryType> _geometryTypes;

		public GeometryTypeConfigurator([NotNull] IList<GeometryType> geometryTypes)
		{
			Assert.ArgumentNotNull(geometryTypes, nameof(geometryTypes));

			_geometryTypes = geometryTypes;
		}

		[CanBeNull]
		public T GetGeometryType<T>() where T : GeometryType
		{
			foreach (GeometryType geometryType in _geometryTypes)
			{
				if (geometryType is T)
				{
					return (T) geometryType;
				}
			}

			return null;
		}

		[CanBeNull]
		public GeometryTypeShape GetGeometryType(esriGeometryType esriGeometryType)
		{
			foreach (GeometryType geometryType in _geometryTypes)
			{
				var geomTypeShape = geometryType as GeometryTypeShape;
				if (geomTypeShape != null &&
				    geomTypeShape.IsEqual(esriGeometryType))
				{
					return geomTypeShape;
				}
			}

			return null;
		}

		[CanBeNull]
		public GeometryType GetGeometryType(Dataset dataset)
		{
			if (dataset is IVectorDataset vectorDataset)
			{
				// Cannot determine VDS subtype here
				return null;
			}

			if (dataset is ITableDataset)
			{
				return GetGeometryType<GeometryTypeNoGeometry>();
			}

			if (dataset is ITopologyDataset)
			{
				return GetGeometryType<GeometryTypeTopology>();
			}

			if (dataset is ISimpleTerrainDataset)
			{
				return GetGeometryType<GeometryTypeTerrain>();
			}

			if (dataset is IRasterMosaicDataset)
			{
				return GetGeometryType<GeometryTypeRasterMosaic>();
			}

			if (dataset is IDdxRasterDataset)
			{
				return GetGeometryType<GeometryTypeRasterDataset>();
			}

			return null;
		}
	}
}
