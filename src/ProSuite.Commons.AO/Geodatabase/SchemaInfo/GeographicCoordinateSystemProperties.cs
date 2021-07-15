using System.ComponentModel;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	internal class GeographicCoordinateSystemProperties : CoordinateSystemProperties
	{
		public GeographicCoordinateSystemProperties(
			IGeographicCoordinateSystem geographicCoordinateSystem)
		{
			FactoryCode = geographicCoordinateSystem.FactoryCode;
			Name = geographicCoordinateSystem.Name;

			IDatum datum = geographicCoordinateSystem.Datum;
			if (datum != null)
			{
				Datum = datum.Name;
			}
		}

		[DisplayName("Factory Code")]
		[UsedImplicitly]
		public int FactoryCode { get; private set; }

		[UsedImplicitly]
		public string Name { get; private set; }

		[UsedImplicitly]
		public string Datum { get; private set; }

		public override string ToString()
		{
			return string.Format("Geographic Coordinate System: {0}", Name);
		}
	}
}
