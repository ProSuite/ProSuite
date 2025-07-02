namespace ProSuite.Commons.Geom
{
	public interface IPnt : ICoordinates, IGmtry, IBoundedXY
	{
		double this[int index] { get; set; }

		IPnt Clone();
	}
}
