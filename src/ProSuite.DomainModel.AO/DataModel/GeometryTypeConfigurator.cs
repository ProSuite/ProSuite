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
	}
}
