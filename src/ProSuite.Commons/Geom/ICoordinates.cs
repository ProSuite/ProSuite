namespace ProSuite.Commons.Geom
{
	public interface ICoordinates
	{
		double X { get; }
		double Y { get; }
		double? Z { get; }

		bool HasZ { get; }
	}
}
