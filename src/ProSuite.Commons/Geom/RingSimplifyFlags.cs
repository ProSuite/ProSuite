using System;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// The individual repairs <see cref="RingSimplifier"/> performs on a ring. They are
	/// independent of each other and are always applied in the order in which they are
	/// listed here (cheapest and most local first).
	/// </summary>
	[Flags]
	public enum RingSimplifyFlags
	{
		None = 0,

		/// <summary>
		/// Delete zero-width spikes ("needles"): a vertex that is reached and left along the
		/// same segment. Detected by a linear vertex scan, i.e. without calculating any
		/// self-intersection.
		/// </summary>
		DeleteNeedles = 1,

		/// <summary>
		/// Delete all linear self-intersections, i.e. every out-and-back run of the ring,
		/// not just the zero-width ones (see
		/// <see cref="GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY"/>). Implies
		/// <see cref="DeleteNeedles"/>, which is the cheap special case of it.
		/// </summary>
		DeleteLinearSelfIntersections = 2,

		/// <summary>
		/// Split a ring that crosses itself (figure-8) into separate simple rings, see
		/// <see cref="GeomTopoOpUtils.TryCrackSelfCrossingRing"/>.
		/// </summary>
		CrackSelfCrossings = 4,

		/// <summary>
		/// Remove a boundary loop whose two flanks are closer to each other than the
		/// tolerance, see <see cref="RingSimplifier.RemoveSubToleranceBoundaryLoop"/>.
		/// </summary>
		RemoveSubToleranceBoundaryLoops = 8,

		/// <summary>
		/// Split an exterior ring that touches itself in a point into its two atomic loops.
		/// </summary>
		ExplodeExteriorBoundaryLoops = 16,

		/// <summary>
		/// What the incremental union / difference needs between two steps to keep its
		/// accumulated result simple. Deliberately uses the cheap
		/// <see cref="DeleteNeedles"/> rather than the full linear self-intersection
		/// deletion: only the zero-width spikes are known to derail the following
		/// navigation, and cancelling out wider out-and-back runs mid-fold would change
		/// the accumulated area.
		/// </summary>
		StepResult = DeleteNeedles | RemoveSubToleranceBoundaryLoops |
		             ExplodeExteriorBoundaryLoops,

		/// <summary>
		/// What the input rings of a union need before the first step: they come from an
		/// arbitrary source (a multipatch, a cracked ring set) and may be non-simple in
		/// every way. Boundary loops are NOT removed or exploded here - at this point they
		/// can still be a legitimate part of the input outline.
		/// </summary>
		Input = DeleteNeedles | DeleteLinearSelfIntersections | CrackSelfCrossings,

		All = DeleteNeedles | DeleteLinearSelfIntersections | CrackSelfCrossings |
		      RemoveSubToleranceBoundaryLoops | ExplodeExteriorBoundaryLoops
	}
}
