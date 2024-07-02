namespace ESRI.ArcGIS.Geometry
{
	public interface IGeometry
	{
		esriGeometryType GeometryType { get; }

		esriGeometryDimension Dimension { get; }

		ISpatialReference SpatialReference { get; set; }

		bool IsEmpty { get; }

		void SetEmpty();

		void QueryEnvelope(IEnvelope outEnvelope);

		IEnvelope Envelope { get; }

		void Project(ISpatialReference newReferenceSystem);

		void SnapToSpatialReference();

		void GeoNormalize();

		void GeoNormalizeFromLongitude(double Longitude);
	}

	public enum esriGeometryDimension
	{
		/// <summary>The dimension is unknown or unspecified.</summary>
		esriGeometryNoDimension = -1, // 0xFFFFFFFF
		/// <summary>A zero dimensional geometry (such as a point or multipoint).</summary>
		esriGeometry0Dimension = 1,
		/// <summary>A one dimensional geometry (such as a polyline).</summary>
		esriGeometry1Dimension = 2,
		/// <summary>A two dimensional geometry (such as a polygon).</summary>
		esriGeometry2Dimension = 4,
		/// <summary>A 2.5D geometry (such as a surface mesh).</summary>
		esriGeometry25Dimension = 5,
		/// <summary>A 3D geometry.</summary>
		esriGeometry3Dimension = 6,
	}
}
