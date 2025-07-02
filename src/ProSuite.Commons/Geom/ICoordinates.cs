namespace ProSuite.Commons.Geom
{
	public interface ICoordinates : IBoundedXY
	{
		double X { get; set; }
		double Y { get; set; }
		double Z { get; set; }
	}
}
