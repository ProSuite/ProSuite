using System;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// How the cluster tolerance and the minimum segment length are derived from the XY
	/// tolerance.
	/// <para>
	/// Before two rings are combined they are snapped onto each other, so that places where
	/// they very nearly touch become places where they exactly touch. Two distances control
	/// this, and every strategy below sets both:
	/// </para>
	/// <list type="bullet">
	/// <item>
	/// The CLUSTER tolerance. Two vertices closer together than this are moved onto one
	/// common position, and a segment that passes such a position without having a vertex
	/// there is cracked, i.e. gets one inserted. Both the clustering and the cracking happen
	/// at this one distance. It is how far a vertex can travel, so it is also the largest
	/// error the snapping can introduce.
	/// </item>
	/// <item>
	/// The MINIMUM SEGMENT LENGTH. A segment shorter than this is dropped afterwards. Such a
	/// segment is normally a leftover of the snapping - both of its endpoints have just been
	/// pulled almost onto each other - and not a segment the input meant to have. It is
	/// always smaller than the cluster tolerance, otherwise the vertices cracking has just
	/// inserted would be dropped again right away.
	/// </item>
	/// </list>
	/// <para>
	/// A larger setting joins up geometry that a smaller one leaves separate, at the price
	/// of moving the boundary further away from where the input had it.
	/// </para>
	/// </summary>
	public enum CrackAndClusterToleranceStrategy
	{
		/// <summary>
		/// The careful setting: cluster and crack at 1 x the tolerance, drop segments shorter
		/// than 0.71 x the tolerance (tolerance / sqrt(2)).
		/// <para>Vertices are never moved by more than the tolerance, which keeps the
		/// snapped geometry within the accuracy the tolerance promises. The price is that
		/// two rings which should meet but are further apart than the tolerance are left not
		/// quite meeting.</para>
		/// </summary>
		Uniform,

		/// <summary>
		/// The setting that imitates ArcObjects: cluster and crack at 2.83 x the tolerance
		/// (2 * sqrt(2)), drop segments shorter than 1.41 x the tolerance (sqrt(2)).
		/// <para>It resolves proximities that <see cref="Uniform"/> leaves in place, but a
		/// boundary can end up almost three tolerances away from where the input had it.
		/// Where the surrounding data is stored at the same tolerance, a shift that large
		/// shows up as a sliver between the footprint and its neighbours - which is what it
		/// did in the Swissbuildings verification (TOP-5999).</para>
		/// </summary>
		Aggressive,

		/// <summary>
		/// Half way between <see cref="Uniform"/> and <see cref="Aggressive"/>:
		/// - Cluster and crack at sqrt(2) * tolerance,
		/// - Drop segments shorter than the tolerance.
		/// A vertex can move by up to 1.41 * tolerance.
		/// <para>This is the smallest pair that satisfies both preconditions of the
		/// subsequent turning-left walk, which runs at the plain tolerance: the minimum
		/// segment length must be at least the walk tolerance (otherwise a vertex can end up
		/// within the walk tolerance of a segment it was not inserted into - the case
		/// <see cref="Uniform"/> leaves open at 0.71 * tolerance), and the cluster tolerance
		/// must exceed the minimum segment length so that the iteration can absorb the
		/// vertices cracking just introduced.</para>
		/// <para>Note that the ratio between the two is NOT the same for all three
		/// strategies: it is sqrt(2) here and for <see cref="Uniform"/>, but 2 for
		/// <see cref="Aggressive"/>.</para>
		/// <para>Measured over 137'042 TLM_GEBAEUDEKOERPER in the Lugano and Bern extents
		/// (2026-08-27, tolerance 0.01), this agrees with the ArcObjects footprint better
		/// than <see cref="Aggressive"/> on every count. Bern: 334 -> 276 footprints the
		/// union cannot produce at all, 1556 -> 901 off by more than 0.05 m2, 20 -> 13 off
		/// by more than 1 m2. Lugano: 110 -> 99, 547 -> 316, 2 -> 1 collapses. Compared per
		/// building it also breaks nothing: in Bern 667 buildings improve and 92 get worse,
		/// and the worst single one is off by 9 m2 more. <see cref="Uniform"/> is better
		/// still on the everyday agreement, but it breaks roughly eight buildings per
		/// 100'000 badly - an area doubling, or a collapse to almost nothing - which this
		/// setting leaves intact.</para>
		/// </summary>
		Balanced
	}

	/// <summary>
	/// Options for the crack-and-cluster pass of <see cref="SimplificationUtils"/>.
	/// </summary>
	/// <remarks>
	/// The invariant clusterTolerance &gt; minimumSegmentLength must hold for every strategy:
	/// cracking inserts vertices at the cluster tolerance, and a minimum segment length at or
	/// above it would drop the segments those vertices create in the same pass, so the
	/// iteration would never settle.
	/// </remarks>
	public class CrackAndClusterOptions
	{
		/// <summary>
		/// Options that leave the geometry untouched.
		/// </summary>
		public static CrackAndClusterOptions Disabled =>
			new CrackAndClusterOptions { Enabled = false };

		/// <summary>
		/// Whether the crack-and-cluster pass runs at all. When false,
		/// <see cref="SimplificationUtils"/> returns its input unchanged and
		/// <see cref="RingOperator"/> does not snap the two operands of a union step onto
		/// each other.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <remarks>
		/// <see cref="CrackAndClusterToleranceStrategy.Balanced"/> by default - see the
		/// measurements quoted there. It replaced
		/// <see cref="CrackAndClusterToleranceStrategy.Aggressive"/> as the default on
		/// 2026-08-27. Aggressive was chosen because it matches the radius ArcObjects
		/// clusters with, and the ArcObjects footprint is the reference; by now it no longer
		/// matches it best, most likely because <see cref="RingSimplifier"/> and the
		/// ring-relationship simplification have since started repairing the same
		/// proximities properly, so the extra movement only adds error.
		/// </remarks>
		public CrackAndClusterToleranceStrategy ToleranceStrategy { get; set; } =
			CrackAndClusterToleranceStrategy.Balanced;

		/// <summary>
		/// The maximum number of cluster/crack iterations. If the fixpoint is not reached
		/// within this many iterations the input is returned UNCHANGED (see
		/// <see cref="SimplificationUtils.CrackAndCluster(Polyhedron,double,CrackAndClusterOptions,out int)"/>);
		/// a half-snapped geometry would be worse than the original.
		/// </summary>
		public int MaxIterations { get; set; } = 12;

		/// <summary>
		/// The distance within which two vertices are moved onto one common position, and
		/// within which a segment passing such a position is cracked (given a vertex there).
		/// This is the furthest the snapping can move the geometry.
		/// </summary>
		public double GetClusterTolerance(double tolerance)
		{
			switch (ToleranceStrategy)
			{
				case CrackAndClusterToleranceStrategy.Uniform:
					return tolerance;

				case CrackAndClusterToleranceStrategy.Aggressive:
					return 2.0 * Math.Sqrt(2.0) * tolerance;

				case CrackAndClusterToleranceStrategy.Balanced:
					return Math.Sqrt(2.0) * tolerance;

				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported tolerance strategy: {ToleranceStrategy}");
			}
		}

		/// <summary>
		/// The length below which a segment is dropped after the snapping. Always smaller than
		/// <see cref="GetClusterTolerance"/>. Note that this is NOT a cracking distance:
		/// cracking happens at the cluster tolerance.
		/// </summary>
		public double GetMinimumSegmentLength(double tolerance)
		{
			switch (ToleranceStrategy)
			{
				case CrackAndClusterToleranceStrategy.Uniform:
					return tolerance / Math.Sqrt(2.0);

				case CrackAndClusterToleranceStrategy.Aggressive:
					return Math.Sqrt(2.0) * tolerance;

				case CrackAndClusterToleranceStrategy.Balanced:
					return tolerance;

				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported tolerance strategy: {ToleranceStrategy}");
			}
		}
	}
}
