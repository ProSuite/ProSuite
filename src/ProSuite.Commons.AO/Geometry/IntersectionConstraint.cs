namespace ProSuite.Commons.AO.Geometry
{
	/// <summary>
	/// Intersection constraints for individual cells in the <see cref="IntersectionMatrix"></see>
	/// </summary>
	public enum IntersectionConstraint
	{
		/// <summary>
		/// Intersection is not checked ("*" in matrix string)
		/// </summary>
		NotChecked,

		/// <summary>
		/// There must be an intersetion, of any dimension ("T" in matrix string)
		/// </summary>
		MustIntersect,

		/// <summary>
		/// There must be no intersection ("F" in matrix string)
		/// </summary>
		MustNotIntersect,

		/// <summary>
		/// There must be an intersection of maximum dimension 0 ("0" in matrix string)
		/// </summary>
		MustIntersectWithMaxDimension0,

		/// <summary>
		/// There must be an intersection of maximum dimension 1 ("1" in matrix string)
		/// </summary>
		MustIntersectWithMaxDimension1,

		/// <summary>
		/// There must be an intersection of maximum dimension 2 ("2" in matrix string)
		/// </summary>
		MustIntersectWithMaxDimension2
	}
}
