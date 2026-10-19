using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// Makes rings simple in XY, i.e. removes the self-intersections that the navigation of
	/// the boolean operations cannot deal with (needles, spikes, sub-tolerance boundary
	/// loops, self-crossings, point-touches of an exterior ring).
	/// <para>This is the single place where those repairs live. They used to be spread over
	/// the callers, each of them calculating the self-intersections of the same ring again:
	/// <c>Polyhedron.SimplifyRing</c> before the union (two calculations per ring),
	/// <c>RemoveSubToleranceBoundaryLoops</c> (an O(n^2) vertex scan) and
	/// <c>ExplodeExteriorBoundaryLoops</c> (one more calculation) after every single step of
	/// the incremental union and difference.</para>
	/// <para>Two properties make it cheap enough to be called after every step of an
	/// incremental operation:
	/// <list type="bullet">
	/// <item>The self-intersections of a ring are calculated ONCE and all repairs that need
	/// them are dispatched from that one result.</item>
	/// <item>A ring without self-intersections - by far the common case - costs exactly that
	/// one (spatially indexed) calculation and nothing else. In particular the O(n^2) scan
	/// for a sub-tolerance boundary loop only runs for a ring that is known to touch
	/// itself.</item>
	/// </list>
	/// </para>
	/// </summary>
	public static class RingSimplifier
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// The number of ring replacements after which the fixpoint iteration of a single
		/// ring gives up and keeps what it has. Each replacement removes a self-intersection,
		/// so the bound is never reached by a real geometry; it only guarantees termination
		/// if two repairs were to undo each other.
		/// </summary>
		private const int _maxRepairsPerRing = 32;

		/// <summary>
		/// Applies the repairs selected by <paramref name="flags"/> to every ring of
		/// <paramref name="rings"/>, in place. A ring can be replaced by several rings (a
		/// self-crossing ring, an exploded boundary loop) or by none at all (a ring that
		/// consists of nothing but needles).
		/// </summary>
		/// <param name="rings">The rings to simplify. Modified in place.</param>
		/// <param name="tolerance">The XY tolerance.</param>
		/// <param name="flags">The repairs to perform.</param>
		/// <param name="areaOfInterest">If specified, the rings whose bounds lie outside this
		/// area are left alone. Use it after a step of an incremental operation that could
		/// only have changed the rings within a known area: the rings outside it were made
		/// simple by the step that produced them, and re-checking all of them after every
		/// step makes the operation quadratic in the number of inputs.</param>
		/// <returns>Whether any ring was changed.</returns>
		public static bool SimplifyRingsXY([NotNull] MultiLinestring rings,
		                                   double tolerance,
		                                   RingSimplifyFlags flags =
			                                   RingSimplifyFlags.StepResult,
		                                   [CanBeNull] IBoundedXY areaOfInterest = null)
		{
			if (flags == RingSimplifyFlags.None || rings.IsEmpty)
			{
				return false;
			}

			var changed = false;

			// Reused across the rings: in the common case nothing is added to it.
			var replacement = new List<Linestring>(1);

			// The parts are replaced while iterating, hence the snapshot.
			foreach (Linestring ring in rings.GetLinestrings().ToList())
			{
				if (areaOfInterest != null &&
				    GeomRelationUtils.AreBoundsDisjoint(ring, areaOfInterest, tolerance))
				{
					continue;
				}

				replacement.Clear();

				if (! TrySimplifyRingXY(ring, tolerance, flags, replacement))
				{
					continue;
				}

				changed = true;

				int index = IndexOf(rings, ring);

				rings.RemoveLinestring(ring);

				for (var i = 0; i < replacement.Count; i++)
				{
					rings.InsertLinestring(index + i, replacement[i]);
				}
			}

			return changed;
		}

		/// <summary>
		/// Applies the repairs selected by <paramref name="flags"/> to a single ring until
		/// it has no more self-intersections that they could remove.
		/// </summary>
		/// <param name="ring">The ring to simplify. Not modified.</param>
		/// <param name="tolerance">The XY tolerance.</param>
		/// <param name="flags">The repairs to perform.</param>
		/// <param name="results">Receives the resulting rings. Nothing is added if the ring
		/// is already simple.</param>
		/// <param name="discarded">Optionally receives the fragments that are dropped
		/// because they are not a proper ring any more (the caller may want to classify them,
		/// e.g. as vertical rings).</param>
		/// <returns>Whether the ring was changed, i.e. whether <paramref name="results"/>
		/// was written to.</returns>
		public static bool TrySimplifyRingXY([NotNull] Linestring ring,
		                                     double tolerance,
		                                     RingSimplifyFlags flags,
		                                     [NotNull] List<Linestring> results,
		                                     [CanBeNull] List<Linestring> discarded = null)
		{
			if (! TryRepairOnce(ring, tolerance, flags, out List<Linestring> firstRepair))
			{
				// The common case: nothing to do and nothing allocated.
				return false;
			}

			var repairCount = 1;

			var pending = new Queue<Linestring>();

			Enqueue(firstRepair, pending, discarded);

			var simplified = new List<Linestring>(1);

			while (pending.Count > 0)
			{
				Linestring current = pending.Dequeue();

				if (repairCount >= _maxRepairsPerRing ||
				    ! TryRepairOnce(current, tolerance, flags,
				                    out List<Linestring> replacement))
				{
					simplified.Add(current);
					continue;
				}

				repairCount++;

				Enqueue(replacement, pending, discarded);
			}

			results.AddRange(simplified);

			return true;
		}

		private static void Enqueue([NotNull] IEnumerable<Linestring> fragments,
		                            [NotNull] Queue<Linestring> pending,
		                            [CanBeNull] List<Linestring> discarded)
		{
			foreach (Linestring fragment in fragments)
			{
				if (fragment.IsClosed && fragment.PointCount >= 4)
				{
					pending.Enqueue(fragment);
				}
				else
				{
					// Not a ring any more: an open remainder or a collapsed one.
					discarded?.Add(fragment);
				}
			}
		}

		/// <summary>
		/// Performs the first applicable repair on the ring and returns what replaces it.
		/// </summary>
		/// <returns>False if the ring needs no repair, in which case
		/// <paramref name="replacement"/> is null.</returns>
		private static bool TryRepairOnce([NotNull] Linestring ring,
		                                  double tolerance,
		                                  RingSimplifyFlags flags,
		                                  out List<Linestring> replacement)
		{
			replacement = null;

			if (! ring.IsClosed || ring.PointCount < 4)
			{
				return false;
			}

			// The needles first: the scan is linear and needs no intersection calculation at
			// all, and removing them can make the rest unnecessary.
			if (HasFlag(flags, RingSimplifyFlags.DeleteNeedles) ||
			    HasFlag(flags, RingSimplifyFlags.DeleteLinearSelfIntersections))
			{
				Linestring withoutNeedles = RemoveNeedles(ring, tolerance);

				if (withoutNeedles != null)
				{
					replacement = new List<Linestring>(1);

					if (! withoutNeedles.IsEmpty)
					{
						replacement.Add(withoutNeedles);
					}

					return true;
				}
			}

			IList<IntersectionPoint3D> selfIntersections =
				GeomTopoOpUtils.GetSelfIntersectionPoints(ring, tolerance);

			if (selfIntersections.Count == 0)
			{
				// The fast path: the ring is simple, nothing else has to look at it.
				return false;
			}

			if (HasFlag(flags, RingSimplifyFlags.DeleteLinearSelfIntersections) &&
			    selfIntersections.Any(IsLinear))
			{
				var fragments = new List<Linestring>();

				if (GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
					    ring, tolerance, fragments))
				{
					replacement = fragments;
					return true;
				}
			}

			if (HasFlag(flags, RingSimplifyFlags.CrackSelfCrossings) &&
			    selfIntersections.Any(ip => ip.Type == IntersectionPointType.Crossing))
			{
				var fragments = new List<Linestring>();

				if (GeomTopoOpUtils.TryCrackSelfCrossingRing(ring, tolerance, fragments))
				{
					replacement = fragments;
					return true;
				}
			}

			if (HasFlag(flags, RingSimplifyFlags.RemoveSubToleranceBoundaryLoops))
			{
				// Gated by the self-intersections above: without them there is no vertex pair
				// for the O(n^2) scan to find, so the scan is only paid for a ring that is
				// already known to intersect itself.
				List<Pnt3D> withoutLoop = RemoveSubToleranceBoundaryLoop(ring, tolerance);

				if (withoutLoop != null)
				{
					replacement = new List<Linestring> { new Linestring(withoutLoop) };
					return true;
				}
			}

			if (HasFlag(flags, RingSimplifyFlags.ExplodeExteriorBoundaryLoops) &&
			    ring.ClockwiseOriented == true)
			{
				// In some sliver situations there might be linear self intersections.
				// Let's not judge them already here.
				List<IntersectionPoint3D> touchPoints =
					selfIntersections
						.Where(ip => ip.Type == IntersectionPointType.TouchingInPoint)
						.ToList();

				if (touchPoints.Count == 2)
				{
					// Positive boundary loops are non-simple and lead to problems, especially
					// if there are more than 2 loops (which typically happens in cupolas).
					var boundaryLoop =
						new BoundaryLoop(touchPoints[0], touchPoints[1], ring, true);

					replacement = new List<Linestring>
					              { boundaryLoop.Loop1, boundaryLoop.Loop2 };

					return true;
				}

				if (touchPoints.Count > 2)
				{
					// TODO: Cluster by point, build pairs
					_msg.WarnFormat(
						"Multiple boundary loops or otherwise unexpected self-intersections in {0}",
						ring.Segments);
				}
			}

			return false;
		}

		#region Needles (zero-width spikes)

		/// <summary>
		/// Returns the ring without its zero-width spikes ("needles"), or null if it has
		/// none. A needle is a vertex whose predecessor and successor are the same point:
		/// the ring is left and re-entered along the very same segment, so the two segments
		/// cancel out and can be deleted without changing the area.
		/// <para>Such a spike carries no area but it is a LINEAR self-intersection, which
		/// neither <see cref="RemoveSubToleranceBoundaryLoop"/> (it wants exactly one
		/// self-touch and a measurable width) nor the boundary loop explosion (it declines to
		/// judge linear self-intersections) handles. In the next union step a ring touching
		/// both flanks of the needle sends the walk around a tiny loop and the whole
		/// accumulated result is dropped (TOP-5999, e.g. TLM_GEBAEUDEKOERPER 4679236:
		/// 14.58 -> 0.07 sq m, 3880597, 8362839, 4896396).</para>
		/// </summary>
		/// <returns>The ring without its needles (empty if nothing but needles remained), or
		/// null if there is nothing to remove.</returns>
		[CanBeNull]
		private static Linestring RemoveNeedles([NotNull] Linestring ring, double tolerance)
		{
			if (! HasNeedle(ring, tolerance))
			{
				// The common case, and it costs one linear scan without allocating.
				return null;
			}

			// The open vertex list (without the duplicated closing point).
			List<Pnt3D> points = ring.GetPoints(0, ring.PointCount - 1, true).ToList();

			bool removedInPass;

			do
			{
				removedInPass = false;

				for (var i = 0; i < points.Count && points.Count > 3; i++)
				{
					Pnt3D previous = points[(i - 1 + points.Count) % points.Count];
					Pnt3D next = points[(i + 1) % points.Count];

					if (! previous.EqualsXY(next, tolerance))
					{
						continue;
					}

					// points[i] is the tip of a needle: drop it together with the
					// coincident successor. Long spikes are peeled off one pair at a time.
					int successor = (i + 1) % points.Count;

					points.RemoveAt(Math.Max(i, successor));
					points.RemoveAt(Math.Min(i, successor));

					removedInPass = true;

					// Re-examine at the same position: the removal can expose the next pair.
					i--;
				}
			} while (removedInPass);

			if (points.Count < 3)
			{
				return Linestring.CreateEmpty();
			}

			points.Add(points[0].ClonePnt3D());

			return new Linestring(points);
		}

		/// <summary>
		/// Whether the closed ring has a vertex whose predecessor and successor are the same
		/// point in XY. Linear in the vertex count and free of allocations, which is what
		/// makes it affordable after every step of an incremental operation.
		/// </summary>
		private static bool HasNeedle([NotNull] Linestring ring, double tolerance)
		{
			// The unique vertices, i.e. without the duplicated closing point.
			int vertexCount = ring.PointCount - 1;

			if (vertexCount < 4)
			{
				return false;
			}

			for (var i = 0; i < vertexCount; i++)
			{
				Pnt3D previous = ring.GetPoint3D((i - 1 + vertexCount) % vertexCount);
				Pnt3D next = ring.GetPoint3D((i + 1) % vertexCount);

				if (previous.EqualsXY(next, tolerance))
				{
					return true;
				}
			}

			return false;
		}

		#endregion

		#region Sub-tolerance boundary loops

		/// <summary>
		/// The vertices of <paramref name="linestring"/> without its single sub-tolerance
		/// boundary loop, or null if there is nothing to remove.
		/// <para>Such a loop is a sliver spike whose two flanks are closer to each other than
		/// the tolerance. It carries no area worth keeping, but it makes the ring intersect
		/// itself LINEARLY, and a linear self-intersection is exactly what the boundary loop
		/// explosion declines to judge. The spike therefore survives into the following union
		/// steps, where the intersection calculation reports a zero-extent linear run for it
		/// (start point == end point, target span collapsed to a single location) and the
		/// turning-left walk is derailed by it - it either loses the large ring (TOP-5999,
		/// TLM_GEBAEUDEKOERPER 8706452: 130 sq m down to 1.08) or throws "Intersections seen
		/// twice" (8712317, 8839728).</para>
		/// <para>Only rings with EXACTLY ONE self-touch are treated. Where a ring has several
		/// loops the decomposition is ambiguous: removing one of them changes how the
		/// remaining ones are classified (8778451 loses 5 sq m if its sliver is removed while
		/// a second, legitimate loop is present), so those rings are left to the boundary loop
		/// explosion as before.</para>
		/// <para>Must run BEFORE the boundary loop explosion: once the ring has been split at
		/// the sliver, the spike is a separate part and no longer recognizable as a boundary
		/// loop.</para>
		/// </summary>
		[CanBeNull]
		internal static List<Pnt3D> RemoveSubToleranceBoundaryLoop(
			[NotNull] Linestring linestring, double tolerance)
		{
			// Below 6 points there is no room for two loops of at least 3 vertices each.
			if (! linestring.IsClosed || linestring.PointCount < 6)
			{
				return null;
			}

			// Work on the open vertex list (without the duplicated closing point).
			List<Pnt3D> vertices =
				linestring.GetPoints(0, linestring.PointCount - 1, true).ToList();

			int vertexCount = vertices.Count;

			int loopStart = -1;
			int loopEnd = -1;

			for (var i = 0; i < vertexCount; i++)
			for (int j = i + 3; j < vertexCount; j++)
			{
				// Both sides of the self-touch must be a ring of their own.
				if (vertexCount - (j - i) < 3)
				{
					continue;
				}

				if (GeomUtils.GetDistanceXY(vertices[i], vertices[j]) > tolerance)
				{
					continue;
				}

				if (loopStart >= 0)
				{
					// More than one self-touch: ambiguous, leave the ring alone.
					return null;
				}

				loopStart = i;
				loopEnd = j;
			}

			if (loopStart < 0)
			{
				return null;
			}

			if (IsSubToleranceLoop(vertices, loopStart, loopEnd, false, tolerance))
			{
				vertices.RemoveRange(loopStart + 1, loopEnd - loopStart);
			}
			else if (IsSubToleranceLoop(vertices, loopStart, loopEnd, true, tolerance))
			{
				// Keep the inner loop only. Its last vertex coincides with its first.
				vertices = vertices.GetRange(loopStart, loopEnd - loopStart);
			}
			else
			{
				return null;
			}

			if (vertices.Count < 3)
			{
				return null;
			}

			vertices.Add(vertices[0].ClonePnt3D());

			return vertices;
		}

		/// <summary>
		/// Whether the loop between the coincident vertices <paramref name="loopStart"/> and
		/// <paramref name="loopEnd"/> is a sliver, i.e. its mean width (2 * area / perimeter)
		/// is below the tolerance. With <paramref name="complement"/> the loop that wraps
		/// around the end of the vertex list is evaluated instead.
		/// </summary>
		private static bool IsSubToleranceLoop([NotNull] IList<Pnt3D> vertices, int loopStart,
		                                       int loopEnd, bool complement, double tolerance)
		{
			int vertexCount = vertices.Count;
			var loop = new List<Pnt3D>();

			if (complement)
			{
				for (int i = loopEnd; i <= loopStart + vertexCount; i++)
				{
					loop.Add(vertices[i % vertexCount]);
				}
			}
			else
			{
				for (int i = loopStart; i <= loopEnd; i++)
				{
					loop.Add(vertices[i]);
				}
			}

			double perimeter = 0;
			for (var i = 0; i < loop.Count - 1; i++)
			{
				perimeter += GeomUtils.GetDistanceXY(loop[i], loop[i + 1]);
			}

			if (perimeter <= 0)
			{
				return false;
			}

			loop.Add(loop[0].ClonePnt3D());

			return 2 * Math.Abs(new Linestring(loop).GetArea2D()) / perimeter < tolerance;
		}

		#endregion

		private static bool HasFlag(RingSimplifyFlags flags, RingSimplifyFlags flag)
		{
			return (flags & flag) == flag;
		}

		private static bool IsLinear([NotNull] IntersectionPoint3D intersectionPoint)
		{
			return intersectionPoint.Type == IntersectionPointType.LinearIntersectionStart ||
			       intersectionPoint.Type == IntersectionPointType.LinearIntersectionEnd ||
			       intersectionPoint.Type ==
			       IntersectionPointType.LinearIntersectionIntermediate;
		}

		private static int IndexOf([NotNull] MultiLinestring rings,
		                           [NotNull] Linestring ring)
		{
			for (var i = 0; i < rings.PartCount; i++)
			{
				if (ReferenceEquals(rings.GetPart(i), ring))
				{
					return i;
				}
			}

			return rings.PartCount;
		}
	}
}
