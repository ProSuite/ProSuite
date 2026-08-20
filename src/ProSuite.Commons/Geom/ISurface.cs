namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// A surface that provides a Z value for any X/Y location.
	/// </summary>
	public interface ISurface
	{
		/// <summary>
		/// Sample the surface's Z value at the given location.
		/// Implementations with bounded support (e.g. a TIN hull or a search radius)
		/// return <see cref="double.NaN" /> outside their support;
		/// total surfaces (e.g. planes) are defined everywhere.
		/// </summary>
		double GetZ(double x, double y);
	}
}
