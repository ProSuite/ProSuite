namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// How an envelope relates to a closed polycurve.
	/// </summary>
	/// <remarks>
	/// The point of asking: an envelope the boundary stays clear of answers for everything in it
	/// at once, so only a <see cref="Straddling"/> one has to be looked into any further.
	/// </remarks>
	public enum EnvelopeRelation
	{
		/// <summary>The polycurve contains no point of the envelope.</summary>
		Disjoint,

		/// <summary>The polycurve contains every point of the envelope.</summary>
		Inside,

		/// <summary>The boundary crosses the envelope, or runs within the tolerance of it, so
		/// its points are not all on the same side.</summary>
		Straddling
	}
}
