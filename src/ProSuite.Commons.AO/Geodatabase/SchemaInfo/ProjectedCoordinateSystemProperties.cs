using System.ComponentModel;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	internal class ProjectedCoordinateSystemProperties : CoordinateSystemProperties
	{
		public ProjectedCoordinateSystemProperties(
			[NotNull] IProjectedCoordinateSystem projCs)
		{
			FactoryCode = projCs.FactoryCode;
			Name = projCs.Name;

			FalseEasting = GetParameterValue(() => projCs.FalseEasting);
			FalseNorthing = GetParameterValue(() => projCs.FalseNorthing);
			ScaleFactor = GetParameterValue(() => projCs.ScaleFactor);
			Azimuth = GetParameterValue(() => projCs.Azimuth);

			ILinearUnit linearUnit = projCs.CoordinateUnit;
			if (linearUnit != null)
			{
				LinearUnit = projCs.CoordinateUnit.Name;
			}

			var projCs2 = projCs as IProjectedCoordinateSystem2;
			if (projCs2 != null)
			{
				LongitudeOfCenter = GetParameterValue(() => projCs2.LongitudeOfCenter);
				LatitudeOfCenter = GetParameterValue(() => projCs2.LatitudeOfCenter);

				LatitudeOfOrigin = GetParameterValue(() => projCs2.LatitudeOfOrigin);
			}

			const bool inDegrees = true;
			CentralMeridian = GetParameterValue(() => projCs.CentralMeridian[inDegrees]);

			IProjection projection = projCs.Projection;
			if (projection != null)
			{
				Projection = projection.Name;
			}

			if (projCs.GeographicCoordinateSystem != null)
			{
				GeographicCoordinateSystem = new GeographicCoordinateSystemProperties(
					projCs.GeographicCoordinateSystem);
			}
		}

		[DisplayName("Factory Code")]
		[UsedImplicitly]
		public int FactoryCode { get; private set; }

		[UsedImplicitly]
		public string Name { get; private set; }

		[UsedImplicitly]
		public string Projection { get; private set; }

		[DisplayName("False Easting")]
		[UsedImplicitly]
		public string FalseEasting { get; private set; }

		[DisplayName("False Northing")]
		[UsedImplicitly]
		public string FalseNorthing { get; private set; }

		[DisplayName("Scale Factor")]
		[UsedImplicitly]
		public string ScaleFactor { get; private set; }

		[UsedImplicitly]
		public string Azimuth { get; private set; }

		[DisplayName("Longitude of Center")]
		[UsedImplicitly]
		public string LongitudeOfCenter { get; private set; }

		[DisplayName("Latitude of Center")]
		[UsedImplicitly]
		public string LatitudeOfCenter { get; private set; }

		[DisplayName("Central Meridian")]
		[UsedImplicitly]
		public string CentralMeridian { get; private set; }

		[DisplayName("Latitude of Origin")]
		[UsedImplicitly]
		public string LatitudeOfOrigin { get; private set; }

		[UsedImplicitly]
		[DisplayName("Linear Unit")]
		public string LinearUnit { get; private set; }

		[TypeConverter(typeof(AllPropertiesConverter))]
		[DisplayName("Geographic Coordinate System")]
		[UsedImplicitly]
		public GeographicCoordinateSystemProperties GeographicCoordinateSystem { get; private set; }

		public override string ToString()
		{
			return string.Format("Projected Coordinate System: {0}", Name);
		}
	}
}
