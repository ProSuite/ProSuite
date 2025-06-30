namespace ProSuite.Commons.Geom
{
	public interface IPntZ : IGmtry, IBoundedXY
	{
		double X { get; set; }
		double Y { get; set; }
		double Z { get; set; }
	}
}
