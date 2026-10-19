namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// How the spike-free algorithm remembers that a triangle is frozen.
	/// </summary>
	/// <remarks>
	/// The paper assumes frozen triangles persist, but an incremental Delaunay can flip them,
	/// so the choice changes the resulting surface.
	/// </remarks>
	public enum SpikeFreeFreezeCarrier
	{
		/// <summary>
		/// Tag plus all three edges as breaklines. Keeps the frozen triangle part of the
		/// surface, at the price of a node wherever two breaklines cross.
		/// </summary>
		HardEdges,

		/// <summary>
		/// No stored state: the freeze condition is re-evaluated per point against the
		/// containing triangle and the neighbours whose circumcircle covers it.
		/// </summary>
		NeighbourCircumcircle,

		/// <summary>
		/// The frozen triangles themselves, tested exactly. No tag to be inherited and no
		/// constraint on the triangulation.
		/// </summary>
		FrozenTriangleIndex
	}
}
