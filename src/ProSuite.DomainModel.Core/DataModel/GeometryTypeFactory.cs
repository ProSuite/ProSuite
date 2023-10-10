using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Factory for creating the standard list of geometry types
	/// </summary>
	public static class GeometryTypeFactory
	{
		[NotNull]
		public static IList<GeometryType> CreateGeometryTypes(
			bool excludeLegacyTypes = false)
		{
			return new List<GeometryType>(CreateGeometryTypesCore(excludeLegacyTypes));
		}

		public static IEnumerable<GeometryType> GetMissingGeometryTypes(
			[NotNull] IList<GeometryType> existingGeometryTypes,
			bool excludeLegacyTypes = false)
		{
			foreach (GeometryType entity in CreateGeometryTypes(excludeLegacyTypes))
			{
				if (existingGeometryTypes.All(gt => gt.Name != entity.Name))
				{
					yield return entity;
				}
			}
		}

		[NotNull]
		private static IEnumerable<GeometryType> CreateGeometryTypesCore(
			bool excludeLegacyTypes)
		{
			yield return new GeometryTypeShape("Point", ProSuiteGeometryType.Point);
			yield return new GeometryTypeShape("Polygon", ProSuiteGeometryType.Polygon);
			yield return new GeometryTypeShape("Polyline", ProSuiteGeometryType.Polyline);
			yield return new GeometryTypeShape("MultiPatch", ProSuiteGeometryType.MultiPatch);
			yield return new GeometryTypeShape("Multipoint", ProSuiteGeometryType.Multipoint);

			yield return new GeometryTypeTerrain("Terrain");
			yield return new GeometryTypeTopology("Topology");

			yield return new GeometryTypeRasterMosaic("Raster Mosaic");
			yield return new GeometryTypeRasterDataset("Raster Dataset");

			yield return new GeometryTypeNoGeometry("No Geometry");

			if (! excludeLegacyTypes)
			{
				yield return new GeometryTypeGeometricNetwork("Geometric Network");
			}
		}
	}
}
