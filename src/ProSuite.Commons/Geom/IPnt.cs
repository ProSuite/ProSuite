namespace ProSuite.Commons.Geom
{
	public interface IPnt : IGmtry
	{
		double X { get; set; }

		double Y { get; set; }

		double this[int index] { get; set; }

		IPnt Clone();
	}
}
