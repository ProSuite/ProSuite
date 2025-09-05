namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Alternative (lighter) implementation of Box.
	/// Extend as needed.
	/// The goal is to replace the Box class in the long run.
	/// </summary>
	public class BoundedBox : IBoundedXY
	{
		public BoundedBox(double xMin, double yMin, double xMax, double yMax)
		{
			XMin = xMin;
			YMin = yMin;
			XMax = xMax;
			YMax = yMax;
		}

		public double XMin { get; set; }
		public double YMin { get; set; }
		public double XMax { get; set; }
		public double YMax { get; set; }
	}
}
