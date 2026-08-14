using System;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// How the cluster and crack tolerances are derived from the XY tolerance.
	/// </summary>
	public enum CrackAndClusterToleranceStrategy
	{
		/// <summary>
		/// Cluster at the XY tolerance, crack at tolerance / sqrt(2). Vertices are never
		/// moved by more than the tolerance, which keeps the snapped geometry within the
		/// accuracy the tolerance promises.
		/// </summary>
		Uniform,

		/// <summary>
		/// The tolerances that results in similar behavior to ArcObjects:
		/// - Cluster at 2 * sqrt(2) * tolerance,
		/// - Crack at sqrt(2) * tolerance.
		/// Considerably more aggressive than <see cref="Uniform"/> - a vertex can move by up to
		/// 2.83 * tolerance - but it resolves proximities that <see cref="Uniform"/> leaves in
		/// place.
		/// </summary>
		Aggressive
	}

	/// <summary>
	/// Options for the crack-and-cluster pass of <see cref="SimplificationUtils"/>.
	/// </summary>
	/// <remarks>
	/// The invariant clusterTolerance &gt; crackTolerance must hold for every strategy:
	/// the vertices introduced by cracking lie (up to the crack tolerance) on the segment
	/// they were inserted into, so the next clustering pass can only absorb them if it
	/// works with the larger radius.
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
		/// <see cref="SimplificationUtils"/> returns its input unchanged.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <remarks>
		/// Aggressive by default: measured on 137'042 TLM_GEBAEUDEKOERPER in two
		/// independent extents it beats Uniform on every metric (Lugano 41'231: 154 vs 250
		/// footprint failures; Bern 95'811: 432 vs 1'019 against 1'019 without cracking).
		/// It matches the radius ArcObjects itself clusters with, which is what the
		/// reference footprint is produced by.
		/// </remarks>
		public CrackAndClusterToleranceStrategy ToleranceStrategy { get; set; } =
			CrackAndClusterToleranceStrategy.Aggressive;

		/// <summary>
		/// The maximum number of cluster/crack iterations. If the fixpoint is not reached
		/// within this many iterations the input is returned UNCHANGED (see
		/// <see cref="SimplificationUtils.CrackAndCluster(Polyhedron,double,CrackAndClusterOptions,out int)"/>);
		/// a half-snapped geometry would be worse than the original.
		/// </summary>
		public int MaxIterations { get; set; } = 12;

		public double GetClusterTolerance(double tolerance)
		{
			switch (ToleranceStrategy)
			{
				case CrackAndClusterToleranceStrategy.Uniform:
					return tolerance;

				case CrackAndClusterToleranceStrategy.Aggressive:
					return 2.0 * Math.Sqrt(2.0) * tolerance;

				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported tolerance strategy: {ToleranceStrategy}");
			}
		}

		public double GetCrackTolerance(double tolerance)
		{
			switch (ToleranceStrategy)
			{
				case CrackAndClusterToleranceStrategy.Uniform:
					return tolerance / Math.Sqrt(2.0);

				case CrackAndClusterToleranceStrategy.Aggressive:
					return Math.Sqrt(2.0) * tolerance;

				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported tolerance strategy: {ToleranceStrategy}");
			}
		}
	}
}
