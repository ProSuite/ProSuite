namespace ProSuite.GIS.Geometry.API
{
	public interface IPoint : IGeometry
	{
		new esriGeometryType GeometryType { get; }

		new esriGeometryDimension Dimension { get; }

		new ISpatialReference SpatialReference { get; set; }

		new bool IsEmpty { get; }

		new void SetEmpty();

		new void QueryEnvelope(IEnvelope outEnvelope);

		new IEnvelope Envelope { get; }

		new void Project(ISpatialReference newReferenceSystem);

		new void SnapToSpatialReference();

		new void GeoNormalize();

		new void GeoNormalizeFromLongitude(double Longitude);

		void QueryCoords(out double X, out double Y);

		void PutCoords(double X, double Y);

		double X { get; set; }

		double Y { get; set; }

		double Z { get; set; }

		double M { get; set; }

		int ID { get; set; }

		//double get_VertexAttribute(esriGeometryAttributes attributeType);

		//void set_VertexAttribute(esriGeometryAttributes attributeType, double attributeValue);

		//void ConstrainDistance(double constraintRadius, IPoint anchor);

		//void ConstrainAngle(double constraintAngle, IPoint anchor, bool allowOpposite);

		//int Compare(IPoint otherPoint);
	}
}
