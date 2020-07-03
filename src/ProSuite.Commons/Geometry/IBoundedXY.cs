namespace ProSuite.Commons.Geometry
{
	public interface IBoundedXY
	{
		double XMin { get; }
		double YMin { get; }
		double XMax { get; }
		double YMax { get; }
	}
}