namespace ProSuite.Processing
{
	public static class Constants
	{
		/// <remarks>
		/// The length of the typographic point is not well defined.
		/// However, PostScript defines the point to be 1/72 of an inch
		/// and this seems to be established practice in desktop publishing.
		/// Experiments show that ArcMap Representations and the ArcObjects
		/// UnitConverter also use PostScript points. So stick with that.
		/// </remarks>
		public const double PointsPerMillimeter = 2.83465;
	}
}
