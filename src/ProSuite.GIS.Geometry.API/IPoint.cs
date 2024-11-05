namespace ProSuite.GIS.Geometry.API
{
	public interface IPoint : IGeometry, IZAware, IMAware
	{
		void QueryCoords(out double x, out double y);

		void PutCoords(double x, double y);

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
		double GetDistance(IPoint otherPoint, bool in3d);

		bool EqualsInXy(IPoint other, double tolerance);
	}
}
