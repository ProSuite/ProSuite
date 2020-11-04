namespace ProSuite.Commons.AO.Geometry.ZAssignment
{
	/// <summary>
	/// Simple Z handling options for edit tools that change a source feature along
	/// a target feature to obtain Z values for changed vertices of the source feature.
	/// </summary>
	public enum ChangeAlongZSource
	{
		/// <summary>
		/// Use the Z values from the target feature(s). If the target has no Z values, fall back
		/// to <see cref="InterpolatedSource"/>.
		/// </summary>
		Target,

		/// <summary>
		/// Interpolate the Z values between existing (unchanged) source vertices.
		/// </summary>
		InterpolatedSource,

		/// <summary>
		/// Assume the source vertices are in a plane (such as roof multipatch features or
		/// footprint polygons) and obtain the new Z values from the plane calculated by
		/// fitting the existing vertices. If no plane can be inferred from the existing
		/// points, <see cref="InterpolatedSource"/> will be used.
		/// </summary>
		SourcePlane
	}
}
