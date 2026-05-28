using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// A boundary loop describes one source or target ring that visits the same XY
	/// point N >= 2 times at a pinch location. Each pinch location is modelled as
	/// a "pinch group" (N >= 2 coinciding intersections at one XY, ordered along
	/// the ring). A single BoundaryLoop owns one pinch group; a ring with multiple
	/// distinct pinch XYs is modelled as multiple BoundaryLoops on the same source
	/// part, consolidated via <see cref="ExtraLoopIntersections"/> (cross-XY pinch
	/// pairs are recorded as "extras" on a main BoundaryLoop so the atomic-loop
	/// emission can recurse through them).
	/// <para>
	/// For N == 2 the legacy properties <see cref="Loop1"/> / <see cref="Loop2"/> /
	/// <see cref="IsLoopingToOutside"/> describe the two atomic sub-rings exactly as
	/// before. For N >= 3 the inner consecutive sub-rings between pinches in
	/// <see cref="Pinches"/> order are the atomic sub-rings; <see cref="Loop1"/> /
	/// <see cref="Loop2"/> only describe the first such pair and are kept for
	/// legacy compatibility.
	/// </para>
	/// </summary>
	public class BoundaryLoop
	{
		private readonly bool _isSourceRing;
		private Linestring _loop1;
		private Linestring _loop2;

		/// <summary>
		/// N-pinch single-group constructor. Pinch points must be ordered along the ring
		/// (by <see cref="IntersectionPoint3D.VirtualSourceVertex"/> for source loops,
		/// <see cref="IntersectionPoint3D.VirtualTargetVertex"/> for target loops).
		/// </summary>
		public BoundaryLoop([NotNull] IList<IntersectionPoint3D> pinchPoints,
		                    [NotNull] Linestring fullRing,
		                    bool isSourceRing,
		                    double tolerance = 0d)
		{
			Assert.ArgumentNotNull(pinchPoints, nameof(pinchPoints));
			Assert.ArgumentCondition(pinchPoints.Count >= 2,
			                         "A boundary loop requires >= 2 coinciding intersections.");
			Assert.ArgumentNotNull(fullRing, nameof(fullRing));

			Pinches = pinchPoints;
			FullRing = fullRing;
			_isSourceRing = isSourceRing;
			Tolerance = tolerance;
		}

		/// <summary>
		/// Legacy 2-pinch constructor preserved for back-compat with callers like
		/// <see cref="GeomTopoOpUtils.ExplodeExteriorBoundaryLoops"/>.
		/// </summary>
		public BoundaryLoop([NotNull] IntersectionPoint3D start,
		                    [NotNull] IntersectionPoint3D end,
		                    [NotNull] Linestring fullRing,
		                    bool isSourceRing)
			: this(new[] { start, end }, fullRing, isSourceRing) { }

		/// <summary>
		/// All N pinch intersections of this group, in ring-vertex order.
		/// </summary>
		[NotNull]
		public IList<IntersectionPoint3D> Pinches { get; }

		public bool HasMultipleAtomicLoops => Pinches.Count > 2;

		public double Tolerance { get; }

		/// <summary>
		/// Cross-XY pinch pairs from OTHER pinch groups on the same source ring.
		/// Set via <see cref="AddExtraLoopIntersections"/> by <see cref="IntersectionClusters"/>
		/// during consolidation; consumed by <see cref="GetLoopSubcurves"/> to emit
		/// the sub-rings between paired extra pinches in addition to this group's
		/// own N atomic sub-rings.
		/// </summary>
		[CanBeNull]
		public IList<Tuple<IntersectionPoint3D, IntersectionPoint3D>> ExtraLoopIntersections
		{
			get;
			set;
		}

		[NotNull]
		public IntersectionPoint3D Start => Pinches[0];

		[NotNull]
		public IntersectionPoint3D End => Pinches[1];

		[NotNull]
		private Linestring FullRing { get; }

		public Linestring Loop1
		{
			get
			{
				if (_loop1 == null)
				{
					_loop1 = GetSubcurve(Start, End, FullRing, true);
				}

				return _loop1;
			}
		}

		public Linestring Loop2
		{
			get
			{
				if (_loop2 == null)
				{
					_loop2 = GetSubcurve(End, Start, FullRing, true);
				}

				return _loop2;
			}
		}

		public bool IsLoopingToOutside
		{
			get
			{
				if (Loop1.ClockwiseOriented == true && Loop2.ClockwiseOriented == true)
				{
					return true;
				}

				if (Loop1.ClockwiseOriented == false && Loop2.ClockwiseOriented == false)
				{
					return false;
				}

				bool? loop1Contains2 =
					GeomRelationUtils.AreaContainsXY(Loop1, Loop2, double.Epsilon);
				if (loop1Contains2 == true)
				{
					return false;
				}

				bool? loop2Contains1 =
					GeomRelationUtils.AreaContainsXY(Loop2, Loop1, double.Epsilon);

				if (loop2Contains1 == true)
				{
					return false;
				}

				return true;
			}
		}

		public bool Loop1ContainsLoop2 =>
			GeomRelationUtils.AreaContainsXY(Loop1, Loop2, double.Epsilon) == true;

		public bool Loop2ContainsLoop1 =>
			GeomRelationUtils.AreaContainsXY(Loop2, Loop1, double.Epsilon) == true;

		public IEnumerable<Linestring> GetBothRings()
		{
			yield return Loop1;
			yield return Loop2;
		}

		/// <summary>
		/// Adds another pinch group's start/end pair from the same source ring as
		/// "extra" intersections. Their sub-rings are emitted recursively by
		/// <see cref="GetLoopSubcurves"/> alongside this group's own atomic sub-rings.
		/// </summary>
		public void AddExtraLoopIntersections([NotNull] IntersectionPoint3D start,
		                                      [NotNull] IntersectionPoint3D end)
		{
			if (ExtraLoopIntersections == null)
			{
				ExtraLoopIntersections =
					new List<Tuple<IntersectionPoint3D, IntersectionPoint3D>>();
			}

			ExtraLoopIntersections.Add(
				new Tuple<IntersectionPoint3D, IntersectionPoint3D>(start, end));
		}

		/// <summary>
		/// Returns each atomic sub-ring as a list of <see cref="IntersectionRun"/>s.
		/// For an N-pinch group: yields N atomic sub-rings between consecutive pinches
		/// in ring-vertex order (with wrap-around). When <see cref="ExtraLoopIntersections"/>
		/// is non-empty, each atomic span is split at ALL extras falling inside it
		/// (yielding one combined main outline plus one recurse per inside extra), and
		/// each extra is consumed exactly once — chained extras are passed down
		/// through recursion via a remaining-extras list.
		/// </summary>
		[NotNull]
		public IEnumerable<IList<IntersectionRun>> GetLoopSubcurves()
		{
			int n = Pinches.Count;
			int loopCount = 0;

			IList<Tuple<IntersectionPoint3D, IntersectionPoint3D>> availableExtras =
				ExtraLoopIntersections;

			for (int i = 0; i < n; i++)
			{
				IntersectionPoint3D from = Pinches[i];
				IntersectionPoint3D to = Pinches[(i + 1) % n];

				if (IsAtomicSpanDegenerate(from, to))
				{
					continue;
				}

				foreach (IList<IntersectionRun> loopSubcurves in
				         GetLoopSubcurves(from, to, availableExtras))
				{
					yield return loopSubcurves;
					loopCount = ValidateLoopCount(loopCount, loopSubcurves.Count);
				}
			}
		}

		private IEnumerable<IList<IntersectionRun>> GetLoopSubcurves(
			[NotNull] IntersectionPoint3D start,
			[NotNull] IntersectionPoint3D end,
			[CanBeNull]
			IList<Tuple<IntersectionPoint3D, IntersectionPoint3D>> availableExtras)
		{
			List<InsideExtra> topLevelExtras =
				CollectTopLevelInsideExtras(start, end, availableExtras);

			if (topLevelExtras.Count == 0)
			{
				yield return new List<IntersectionRun>
				             { GetIntersectionRun(start, end) };
				yield break;
			}

			// Build ONE main outline split at all top-level extras falling inside
			// (start, end): start..first0, second0..first1, ..., secondLast..end.
			// Nested extras stay hidden at this level; they surface inside the
			// recursion on the enclosing top-level extra's inner span.
			var mainRuns = new List<IntersectionRun>(topLevelExtras.Count + 1);
			IntersectionPoint3D cursor = start;
			foreach (InsideExtra extra in topLevelExtras)
			{
				mainRuns.Add(GetIntersectionRun(cursor, extra.First));
				cursor = extra.Second;
			}

			mainRuns.Add(GetIntersectionRun(cursor, end));
			yield return mainRuns;

			// Recurse into each top-level extra's inner span. Strict between-checks
			// in CollectTopLevelInsideExtras keep the recursion finite — at the inner
			// span, the enclosing pair is no longer inside (start/end are its bounds).
			foreach (InsideExtra extra in topLevelExtras)
			{
				foreach (IList<IntersectionRun> innerLoop in
				         GetLoopSubcurves(extra.First, extra.Second, availableExtras))
				{
					yield return innerLoop;
				}
			}
		}

		private List<InsideExtra> CollectTopLevelInsideExtras(
			[NotNull] IntersectionPoint3D start,
			[NotNull] IntersectionPoint3D end,
			[CanBeNull]
			IList<Tuple<IntersectionPoint3D, IntersectionPoint3D>> availableExtras)
		{
			var inside = new List<InsideExtra>();
			if (availableExtras == null || availableExtras.Count == 0)
			{
				return inside;
			}

			double startVtx = start.VirtualSourceVertex;
			double endVtx = end.VirtualSourceVertex;

			foreach (Tuple<IntersectionPoint3D, IntersectionPoint3D> pair in availableExtras)
			{
				double v1 = pair.Item1.VirtualSourceVertex;
				double v2 = pair.Item2.VirtualSourceVertex;

				if (! IsRingVertexIndexBetween(v1, startVtx, endVtx) &&
				    ! IsRingVertexIndexBetween(v2, startVtx, endVtx))
				{
					continue;
				}

				GetSourceIntersectionOrder(start, pair.Item1, pair.Item2,
				                           out IntersectionPoint3D first,
				                           out IntersectionPoint3D second);

				double firstDist =
					SegmentCountBetween(FullRing, startVtx, first.VirtualSourceVertex);
				double secondDist =
					SegmentCountBetween(FullRing, startVtx, second.VirtualSourceVertex);

				inside.Add(new InsideExtra(pair, first, second, firstDist, secondDist));
			}

			// Pick top-level extras only (those not nested inside an earlier extra in
			// the sweep). After sorting by distance-from-start, an extra is top-level
			// iff it starts AFTER the previous top-level extra ended.
			inside.Sort((a, b) => a.FirstDistance.CompareTo(b.FirstDistance));

			var topLevel = new List<InsideExtra>(inside.Count);
			double frontier = -1;
			foreach (InsideExtra extra in inside)
			{
				if (extra.FirstDistance > frontier)
				{
					topLevel.Add(extra);
					frontier = extra.SecondDistance;
				}
				// else: nested inside the previous top-level — handled by recursion.
			}

			return topLevel;
		}

		private readonly struct InsideExtra
		{
			public InsideExtra(
				Tuple<IntersectionPoint3D, IntersectionPoint3D> pair,
				IntersectionPoint3D first,
				IntersectionPoint3D second,
				double firstDistance,
				double secondDistance)
			{
				Pair = pair;
				First = first;
				Second = second;
				FirstDistance = firstDistance;
				SecondDistance = secondDistance;
			}

			public Tuple<IntersectionPoint3D, IntersectionPoint3D> Pair { get; }
			public IntersectionPoint3D First { get; }
			public IntersectionPoint3D Second { get; }
			public double FirstDistance { get; }
			public double SecondDistance { get; }
		}

		private void GetSourceIntersectionOrder([NotNull] IntersectionPoint3D start,
		                                        [NotNull] IntersectionPoint3D intersectionA,
		                                        [NotNull] IntersectionPoint3D intersectionB,
		                                        out IntersectionPoint3D first,
		                                        out IntersectionPoint3D second)
		{
			double startA = SegmentCountBetween(FullRing, start.VirtualSourceVertex,
			                                    intersectionA.VirtualSourceVertex);
			double startB = SegmentCountBetween(FullRing, start.VirtualSourceVertex,
			                                    intersectionB.VirtualSourceVertex);

			if (startA < startB)
			{
				first = intersectionA;
				second = intersectionB;
			}
			else
			{
				first = intersectionB;
				second = intersectionA;
			}
		}

		private static double SegmentCountBetween(
			[NotNull] Linestring ring,
			double firstVirtualVertex,
			double secondVirtualVertex)
		{
			double result = secondVirtualVertex - firstVirtualVertex;

			if (result < 0)
			{
				result += ring.SegmentCount;
			}

			return result;
		}

		private static bool IsRingVertexIndexBetween(double ringVertex, double start, double end)
		{
			return start <= end
				       ? ringVertex > start && ringVertex < end
				       : ringVertex > start || (ringVertex < start && ringVertex < end);
		}

		private bool IsAtomicSpanDegenerate([NotNull] IntersectionPoint3D from,
		                                    [NotNull] IntersectionPoint3D to)
		{
			double fromVtx = _isSourceRing ? from.VirtualSourceVertex : from.VirtualTargetVertex;
			double toVtx = _isSourceRing ? to.VirtualSourceVertex : to.VirtualTargetVertex;

			double segs = toVtx - fromVtx;
			if (segs < 0)
			{
				segs += FullRing.SegmentCount;
			}

			// Mirrors the per-pair segment-count guard from the previous pair-based
			// IntersectionClusters implementation: sub-loops one segment or shorter
			// are not real loops (un-cracked intersection or near-degenerate spike).
			return segs <= 1;
		}

		private static int ValidateLoopCount(int previousCount,
		                                     int addedLoopCount)
		{
			const int maxLoops = 100;

			int result = previousCount + addedLoopCount;

			if (result > maxLoops)
			{
				throw new InvalidOperationException(
					"Maximum number of boundary loops exceeded");
			}

			return result;
		}

		private IntersectionRun GetIntersectionRun(IntersectionPoint3D start,
		                                           IntersectionPoint3D end)
		{
			return new IntersectionRun(start, end, GetSubcurve(start, end, FullRing, false), null);
		}

		private Linestring GetSubcurve([NotNull] IntersectionPoint3D fromIntersection,
		                               [NotNull] IntersectionPoint3D toIntersection,
		                               [NotNull] Linestring fullRing,
		                               bool closed)
		{
			double fromDistanceAlongAsRatio;
			double toDistanceAlongAsRatio;

			int fromIndex;
			int toIndex;
			if (_isSourceRing)
			{
				fromIndex = fromIntersection.GetLocalSourceIntersectionSegmentIdx(
					fullRing, out fromDistanceAlongAsRatio);
				toIndex = toIntersection.GetLocalSourceIntersectionSegmentIdx(
					fullRing, out toDistanceAlongAsRatio);
			}
			else
			{
				fromIndex = fromIntersection.GetLocalTargetIntersectionSegmentIdx(
					fullRing, out fromDistanceAlongAsRatio);
				toIndex = toIntersection.GetLocalTargetIntersectionSegmentIdx(
					fullRing, out toDistanceAlongAsRatio);
			}

			const bool forward = true;
			const bool preferFullRing = true;
			Linestring subcurve = fullRing.GetSubcurve(
				fromIndex, fromDistanceAlongAsRatio,
				toIndex, toDistanceAlongAsRatio,
				false, ! forward, preferFullRing);

			if (closed)
			{
				subcurve.Close();
			}

			return subcurve;
		}

		#region Equality members

		protected bool Equals(BoundaryLoop other)
		{
			return Start.Equals(other.Start) && End.Equals(other.End);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((BoundaryLoop) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Start.GetHashCode() * 397) ^ End.GetHashCode();
			}
		}

		#endregion
	}
}
