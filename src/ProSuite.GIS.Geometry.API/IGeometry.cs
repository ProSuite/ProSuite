namespace ProSuite.GIS.Geometry.API
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

		//void Project(ISpatialReference newReferenceSystem);

		void SnapToSpatialReference();

		//void GeoNormalize();

		//void GeoNormalizeFromLongitude(double Longitude);

		// TODO: Clone<T>()
		IGeometry Clone();

		// TODO: ZAware, MAware, PointIDAware
	}

	public interface IMutableGeometry //<T> where T : class
	{
		object ToNativeImplementation();
	}

	public interface IZAware
	{
		bool ZAware { get; set; }

		bool ZSimple { get; }

		//void DropZs();
	}

	public interface IMAware
	{
		bool MAware { get; set; }

		bool MSimple { get; }

		//void DropMs();
	}

	public enum esriGeometryDimension
	{
		esriGeometryNoDimension = -1,
		esriGeometry0Dimension = 1,
		esriGeometry1Dimension = 2,
		esriGeometry2Dimension = 4,
		esriGeometry25Dimension = 5,
		esriGeometry3Dimension = 6,
	}
}
