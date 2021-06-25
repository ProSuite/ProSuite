using System.Collections.Generic;
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
		public static IList<GeometryType> CreateGeometryTypes()
		{
			return new List<GeometryType>(CreateGeometryTypesCore());
		}

		[NotNull]
		private static IEnumerable<GeometryType> CreateGeometryTypesCore()
		{
			yield return new GeometryTypeShape("Point", ProSuiteGeometryType.Point);
			yield return new GeometryTypeShape("Polygon", ProSuiteGeometryType.Polygon);
			yield return new GeometryTypeShape("Polyline", ProSuiteGeometryType.Polyline);
			yield return new GeometryTypeShape("MultiPatch", ProSuiteGeometryType.MultiPatch);
			yield return new GeometryTypeShape("Multipoint", ProSuiteGeometryType.Multipoint);

			yield return new GeometryTypeTerrain("Terrain");
			yield return new GeometryTypeTopology("Topology");
			yield return new GeometryTypeGeometricNetwork("Geometric Network");
			yield return new GeometryTypeRasterMosaic("Raster Mosaic");
			yield return new GeometryTypeRasterDataset("Raster Dataset");

			yield return new GeometryTypeNoGeometry("No Geometry");
		}
	}
}
