namespace ProSuite.GIS.Geometry.API
{
	public interface IRelationalOperator
	{
		//bool Equals(IGeometry other);

		bool Touches(IGeometry other, double? tolerance = null);

		//bool Contains(IGeometry other);

		//bool Within(IGeometry other);

		bool Intersects(IGeometry other, double? tolerance = null);

		//bool Crosses(IGeometry other);

		//bool Overlaps(IGeometry other);

		//bool Relation(string relationDescription);
	}
}
