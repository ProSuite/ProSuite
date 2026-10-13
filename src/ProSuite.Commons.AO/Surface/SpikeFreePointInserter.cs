using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Surface
{
	/// <summary>
	/// The spike-free point insertion algorithm: points are globally sorted by Z descending and
	/// only inserted if they do not represent a spike relative to the already-inserted triangles.
	/// </summary>
	/// <remarks>
	/// This class owns the insertion loop; a subclass decides what counts as a spike and how a
	/// frozen triangle is remembered. Use <see cref="Create"/> to pick one by
	/// <see cref="SpikeFreeFreezeCarrier"/>.
	/// </remarks>
	public abstract class SpikeFreePointInserter
	{
		private const int TagValue = 0;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected SpikeFreePointInserter(double freezeDistance, double insertionBuffer)
		{
			FreezeDistance = freezeDistance;
			InsertionBuffer = insertionBuffer;
		}

		/// <param name="freezeDistance">Maximum triangle edge length (map units) for a triangle to
		/// be considered a spike candidate.</param>
		/// <param name="insertionBuffer">Z-value offset added to a candidate point's Z when testing
		/// whether existing triangle vertices form a spike.</param>
		/// <param name="freezeCarrier">How a frozen triangle is remembered. The default reproduces
		/// the long-standing behaviour.</param>
		[NotNull]
		public static SpikeFreePointInserter Create(
			double freezeDistance, double insertionBuffer,
			SpikeFreeFreezeCarrier freezeCarrier = SpikeFreeFreezeCarrier.HardEdges)
		{
			switch (freezeCarrier)
			{
				case SpikeFreeFreezeCarrier.HardEdges:
					return new HardEdgeSpikeFreePointInserter(freezeDistance, insertionBuffer);

				case SpikeFreeFreezeCarrier.NeighbourCircumcircle:
					return new NeighbourCircumcircleSpikeFreePointInserter(
						freezeDistance, insertionBuffer);

				case SpikeFreeFreezeCarrier.FrozenTriangleIndex:
					return new FrozenTriangleIndexSpikeFreePointInserter(
						freezeDistance, insertionBuffer);

				default:
					throw new ArgumentOutOfRangeException(nameof(freezeCarrier), freezeCarrier,
					                                      "Unknown spike-free freeze carrier.");
			}
		}

		protected double FreezeDistance { get; }

		protected double InsertionBuffer { get; }

		protected abstract SpikeFreeFreezeCarrier FreezeCarrier { get; }

		/// <param name="tin">The TIN to insert points into.</param>
		/// <param name="points">All candidate points (source SR, already transformed if needed).</param>
		/// <param name="detectedSpikes">Collection to store the coordinates of detected spike points.</param>
		public void AddPointsToTin(
			[NotNull] ITinEdit tin,
			[NotNull] IEnumerable<(double x, double y, double z)> points,
			[CanBeNull] ICollection<ICoordinates> detectedSpikes = null)
		{
			var advancedTin = tin as ITinAdvanced;
			Assert.ArgumentNotNull(advancedTin, nameof(advancedTin));

			Prepare(advancedTin);

			var point = new PointClass();

			int addedPoints = 0;
			int ignoredPoints = 0;

			foreach ((double x, double y, double z) in points.OrderByDescending(p => p.z))
			{
				point.PutCoords(x, y);
				point.Z = z;

				if (IsSpike(tin, advancedTin, point))
				{
					detectedSpikes?.Add(new Coordinates3D(x, y, z));
					ignoredPoints++;
					continue;
				}

				tin.AddPointZ(point, TagValue);
				addedPoints++;
			}

			_msg.InfoFormat(
				"Added {0} points to the TIN. {1} points were identified as spikes and ignored. Freeze carrier {2}.",
				addedPoints, ignoredPoints, FreezeCarrier);
		}

		/// <summary>Called once per run, before the first point is looked at.</summary>
		protected virtual void Prepare([NotNull] ITinAdvanced advancedTin) { }

		/// <summary>
		/// Is this point a spike and therefore to be discarded? Implementations may freeze
		/// triangles as a side effect.
		/// </summary>
		protected abstract bool IsSpike([NotNull] ITinEdit tin,
		                                [NotNull] ITinAdvanced advancedTin,
		                                [NotNull] IPoint point);

		/// <summary>
		/// Is the triangle small enough and high enough above the point to freeze it?
		/// </summary>
		protected bool SatisfiesFreezeCondition([NotNull] ITinTriangle triangle,
		                                        [NotNull] IPoint point)
		{
			if (triangle.IsEmpty)
			{
				return false;
			}

			double freezeSq = FreezeDistance * FreezeDistance;
			double thresholdZ = point.Z + InsertionBuffer;

			triangle.QueryVertices(out WKSPointZ v0, out WKSPointZ v1, out WKSPointZ v2);

			// The longest edge decides. The shortest would be the other reading and freezes far
			// more.
			if (SquaredDist2D(v0, v1) >= freezeSq) return false;
			if (SquaredDist2D(v1, v2) >= freezeSq) return false;
			if (SquaredDist2D(v2, v0) >= freezeSq) return false;

			if (v0.Z < thresholdZ) return false;
			if (v1.Z < thresholdZ) return false;
			if (v2.Z < thresholdZ) return false;

			return true;
		}

		protected static bool IsInCircumcircle(WKSPointZ a, WKSPointZ b, WKSPointZ c,
		                                       double px, double py)
		{
			double ax = a.X - px, ay = a.Y - py;
			double bx = b.X - px, by = b.Y - py;
			double cx = c.X - px, cy = c.Y - py;

			double aSq = ax * ax + ay * ay;
			double bSq = bx * bx + by * by;
			double cSq = cx * cx + cy * cy;

			double det = ax * (by * cSq - bSq * cy)
			             - ay * (bx * cSq - bSq * cx)
			             + aSq * (bx * cy - by * cx);

			// The sign depends on the triangle's orientation.
			double orientation = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

			return orientation > 0 ? det > 0 : det < 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static double SquaredDist2D(WKSPointZ a, WKSPointZ b)
		{
			double dx = a.X - b.X;
			double dy = a.Y - b.Y;
			return dx * dx + dy * dy;
		}
	}
}
