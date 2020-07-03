namespace ProSuite.Commons.Geometry
{
	/// <summary>
	/// Encapsulates the coordinates of a 2D envelope for high-performance use cases.
	/// Box provides more functionality but allocates arrays when created.
	/// </summary>
	public class EnvelopeXY : IBoundedXY
	{
		public double XMin { get; set; }
		public double YMin { get; set; }
		public double XMax { get; set; }
		public double YMax { get; set; }

		// TODO: Support and manage empty-ness and invalidity

		public EnvelopeXY(double xMin, double yMin, double xMax, double yMax)
		{
			XMin = xMin;
			YMin = yMin;
			XMax = xMax;
			YMax = yMax;
		}

		public EnvelopeXY(IBoundedXY geometry) : this(geometry.XMin, geometry.YMin,
		                                              geometry.XMax, geometry.YMax) { }

		public double Width => XMax - XMin;
		public double Height => YMax - YMin;

		public void EnlargeToInclude(IBoundedXY other)
		{
			if (other.XMin < XMin)
				XMin = other.XMin;

			if (other.YMin < YMin)
				YMin = other.YMin;

			if (other.XMax > XMax)
				XMax = other.XMax;

			if (other.YMax > YMax)
				YMax = other.YMax;
		}
	}
}