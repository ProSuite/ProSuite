using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// A collection of ring groups that typically build a (closed or un-closed) surface.
	/// By (OGC-)definition the ring groups should be connected (i.e. touch in 3D). However, this
	/// is not currently enforced. Therefore, multi-surfaces (or multipatches) can also be modeled
	/// using this class.
	/// </summary>
	public class Polyhedron : MultiLinestring
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static IBoundedXY CreateEmpty()
		{
			var result = new Polyhedron(new List<RingGroup>(0));

			result.SetEmpty();

			return result;
		}

		public IReadOnlyList<RingGroup> RingGroups { get; private set; }

		public Polyhedron(IList<RingGroup> ringGroups)
			: base((ringGroups.SelectMany(r => r.GetLinestrings())))
		{
			RingGroups = ringGroups.ToList();
		}

		protected override void AddLinestringCore(Linestring linestring)
		{
			if (RingGroups.Count == 0)
			{
				var newGroup = new RingGroup(linestring);
				RingGroups = new List<RingGroup> { newGroup };
			}
			else
			{
				RingGroup lastGroup = RingGroups.Last();
				lastGroup.AddLinestring(linestring);
			}
		}

		protected override void InsertLinestringCore(int index, Linestring linestring)
		{
			// TODO: Probably handle the part-insertion on the subclass level altogether
			// to avoid global/local index conversion
			// For polyhedra it might make more sense to add a linestring to a specified group
			int currentStartIdx = 0;
			foreach (RingGroup ringGroup in RingGroups)
			{
				if (index - currentStartIdx < ringGroup.PartCount)
				{
					ringGroup.InsertLinestring(index - currentStartIdx, linestring);
					return;
				}

				currentStartIdx += ringGroup.PartCount;
			}
		}

		protected override void RemoveLinestringCore(Linestring linestring)
		{
			foreach (RingGroup group in RingGroups)
			{
				group.RemoveLinestring(linestring);
			}
		}

		public IEnumerable<RingGroup> FindRingGroups([NotNull] IBoundedXY searchGeometry,
		                                             double tolerance)
		{
			HashSet<RingGroup> found = new HashSet<RingGroup>();

			foreach (int ringIndex in FindParts(searchGeometry, tolerance))
			{
				Linestring foundLinestring = GetPart(ringIndex);

				foreach (RingGroup ringGroup in RingGroups)
				{
					if (ringGroup.GetLinestrings().Any(l => l == foundLinestring))
					{
						found.Add(ringGroup);
					}
				}
			}

			return found;
		}

		/// <param name="tolerance">The calculation tolerance, which can be small (e.g. close to
		/// the resolution in order to be able to properly calculate almost-vertical walls that are
		/// considerably thinner than the XY tolerance)</param>
		/// <param name="verticalRingDetectionTolerance">The tolerance for detecting vertical rings
		/// and vertices very close to other vertices (also between the source and the target) that
		/// ensures that the operation can succeed thanks to clustering. Should be larger than the
		/// XY resolution, ideally similar to the tolerance.</param>
		/// <param name="verticalRings">Output parameter for rings that are too small in XY.</param>
		public MultiLinestring GetXYFootprint(double tolerance,
		                                      double verticalRingDetectionTolerance,
		                                      out List<Linestring> verticalRings)
		{
			// TODO: Explain the rationale for the vertical ring detection tolerance and how it
			// differs from the XY tolerance, if at all. 

			verticalRings = new List<Linestring>();

			if (RingGroups.Count == 0)
			{
				return MultiPolycurve.CreateEmpty();
			}

			var ringGroupsToUnionize = new List<RingGroup>();

			// Remove interior rings that are not coplanar with their exterior ring.
			// By the OGC simple-polyhedron definition an interior ring (a hole) must lie
			// in the same plane as its exterior ring. A ring that is too far off that plane
			// is likely a different face mis-stored as an inner ring; subtracting it would punch
			// a spurious hole into the footprint.
			var ringGroupsToClean = new List<RingGroup>(RingGroups.Count);
			foreach (RingGroup ringGroup in RingGroups)
			{
				ringGroupsToClean.Add(
					RemoveNonCoplanarInteriorRings(
						ringGroup, verticalRingDetectionTolerance,
						out List<Linestring> nonCoplanarRings));

				foreach (Linestring nonCoplanarRing in nonCoplanarRings)
				{
					// For the time being we treat them as exterior rings as they are most likely misclassified as interior.
					ringGroupsToClean.Add(new RingGroup(nonCoplanarRing));
				}
			}

			foreach (RingGroup coplanarGroup in ringGroupsToClean)
			{
				// Split all the rings at linear self-intersections and remove spikes, i.e.
				// duplicate segments sticking out. This makes ClockwiseOriented correct
				// for each cleaned ring before AsProperlyOriented runs. This operation
				// can change the ring's orientation!
				IEnumerable<RingGroup> cleanedGroups = SimplifyRings(
					coplanarGroup, verticalRingDetectionTolerance,
					out List<Linestring> cleanupVerticals);

				verticalRings.AddRange(cleanupVerticals);

				foreach (RingGroup cleanedGroup in cleanedGroups)
				{
					RingGroup orientedGroup = AsProperlyOriented(cleanedGroup);

					if (orientedGroup.IsEmpty)
					{
						verticalRings.AddRange(cleanedGroup.GetLinestrings());
						continue;
					}

					ringGroupsToUnionize.Add(orientedGroup);
				}
			}

			// Pass the resolution as the merge tolerance so the union snaps near-coincident
			// parallel edge runs (shared walls separated only by a sub-resolution offset) into
			// clean linear intersections, making the footprint robust at fine tolerances.
			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(ringGroupsToUnionize, tolerance,
				                                verticalRingDetectionTolerance,
				                                inputRingsMayBeNonSimple: true);

			return result;
		}

		/// <summary>
		/// Splits each ring of the input group at linear self-intersections (which
		/// includes self-tangencies, i.e. rings that visit the same XY point at two
		/// non-adjacent vertices). The resulting rings are simple in XY and therefore
		/// have a deterministic <see cref="Linestring.ClockwiseOriented"/> value, so
		/// that orientation-dependent downstream logic works correctly.
		/// </summary>
		/// <param name="input">The ring group to clean.</param>
		/// <param name="verticalRingDetectionTolerance">Tolerance that determines
		/// whether segments are duplicates and thus rings are vertical.</param>
		/// <param name="verticalRings">Receives fragments that are non-closed or
		/// classified as vertical rings.</param>
		/// <returns>Zero or more cleaned ring groups. Empty if the input was
		/// entirely vertical or degenerate. May contain more than one group if the
		/// exterior ring split into multiple simple rings.</returns>
		private static IEnumerable<RingGroup> SimplifyRings(
			[NotNull] RingGroup input,
			double verticalRingDetectionTolerance,
			[NotNull] out List<Linestring> verticalRings)
		{
			verticalRings = new List<Linestring>();

			List<Linestring> cleanedExteriors = SimplifyRing(
				input.ExteriorRing, verticalRingDetectionTolerance, verticalRings);

			if (cleanedExteriors.Count == 0)
			{
				return Enumerable.Empty<RingGroup>();
			}

			var cleanedInteriors = new List<Linestring>();
			foreach (Linestring interior in input.InteriorRings)
			{
				cleanedInteriors.AddRange(
					SimplifyRing(interior, verticalRingDetectionTolerance, verticalRings));
			}

			// Reassemble. Interior rings are attached to the first exterior fragment
			// only.
			// TODO: When the exterior ring splits, distribute interior rings across

			// the exterior fragments by geometric containment.
			var result = new List<RingGroup>(cleanedExteriors.Count);
			for (int i = 0; i < cleanedExteriors.Count; i++)
			{
				RingGroup group = i == 0 && cleanedInteriors.Count > 0
					                  ? new RingGroup(cleanedExteriors[i], cleanedInteriors)
					                  : new RingGroup(cleanedExteriors[i]);
				result.Add(group);
			}

			return result;
		}

		private static List<Linestring> SimplifyRing(
			[NotNull] Linestring ring,
			double verticalRingDetectionTolerance,
			[NotNull] List<Linestring> verticalRings)
		{
			var candidates = new List<Linestring>();
			var withoutLinearSelfIntersections = new List<Linestring>();

			// TODO: Move both to GeomTopoOpUtils, calculate intersection points only once
			//       and consolidate with polyline simplification.
			if (! GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
				    ring, verticalRingDetectionTolerance, withoutLinearSelfIntersections))
			{
				// No linear self-intersection was removed: keep the original ring, but
				// still explode any 0-dimensional (crossing) self-intersections below.
				withoutLinearSelfIntersections.Add(ring);
			}
			else if (withoutLinearSelfIntersections.Count == 0)
			{
				// All segments cancelled out: the original ring was entirely vertical.
				verticalRings.Add(ring);
				return candidates;
			}

			foreach (Linestring fragment in withoutLinearSelfIntersections)
			{
				if (! fragment.IsClosed)
				{
					verticalRings.Add(fragment);
					continue;
				}

				// Explode figure-8 / self-crossing rings into separate simple rings before any
				// area-based verticality or orientation check!
				var simpleRings = new List<Linestring>();
				if (! GeomTopoOpUtils.TryCrackSelfCrossingRing(
					    fragment, verticalRingDetectionTolerance, simpleRings))
				{
					simpleRings.Add(fragment);
				}

				foreach (Linestring simpleRing in simpleRings)
				{
					if (simpleRing.IsClosed &&
					    ! simpleRing.IsVerticalRing(verticalRingDetectionTolerance))
					{
						candidates.Add(simpleRing);
					}
					else
					{
						verticalRings.Add(simpleRing);
					}
				}
			}

			return candidates;
		}

		/// <summary>
		/// Returns a ring group equivalent to <paramref name="ringGroup"/> but with any
		/// interior rings removed that are not coplanar with the exterior ring (an
		/// OGC-simple-polyhedron violation). The exterior and all coplanar interior
		/// rings are kept unchanged. The removed (non-coplanar) interior rings are
		/// returned via <paramref name="nonCoplanarRings"/>. If the exterior plane
		/// cannot be determined or all interior rings are coplanar, the original ring
		/// group is returned.
		/// </summary>
		[NotNull]
		private static RingGroup RemoveNonCoplanarInteriorRings(
			[NotNull] RingGroup ringGroup,
			double coplanarityTolerance,
			[NotNull] out List<Linestring> nonCoplanarRings)
		{
			nonCoplanarRings = new List<Linestring>();

			if (ringGroup.InteriorRingCount == 0)
			{
				return ringGroup;
			}

			IList<Pnt3D> exteriorPoints = ringGroup.ExteriorRing.GetPoints().ToList();

			Plane3D exteriorPlane = Plane3D.TryFitPlane(exteriorPoints, isRing: true);

			if (exteriorPlane == null || ! exteriorPlane.IsDefined)
			{
				// Cannot determine the exterior plane (e.g. degenerate/collinear ring):
				// keep all interior rings unchanged.
				return ringGroup;
			}

			var coplanarInteriors = new List<Linestring>(ringGroup.InteriorRingCount);
			foreach (Linestring interiorRing in ringGroup.InteriorRings)
			{
				if (IsCoplanar(interiorRing, exteriorPlane, coplanarityTolerance))
				{
					coplanarInteriors.Add(interiorRing);
				}
				else
				{
					_msg.DebugFormat(
						"Detected interior ring that is not coplanar with its exterior. Interior ring has {0} segments (max. distance to exterior plane: {1})",
						interiorRing.SegmentCount,
						interiorRing.GetPoints()
						            .Max(p => exteriorPlane.GetDistanceAbs(p.X, p.Y, p.Z)));

					nonCoplanarRings.Add(interiorRing);
				}
			}

			if (nonCoplanarRings.Count == 0)
			{
				// All interior rings are coplanar: nothing to remove.
				return ringGroup;
			}

			return new RingGroup(ringGroup.ExteriorRing, coplanarInteriors);
		}

		/// <summary>
		/// Returns the rings (exterior or interior) that have a linear or 0-dimensional
		/// (point / crossing) self-intersection in XY. Vertical rings are skipped because
		/// their XY footprint degenerates to a line, which would be reported as a false
		/// self-intersection; for planar, non-vertical faces the XY projection is a
		/// bijection, so an XY self-intersection corresponds to a real in-plane one.
		/// </summary>
		/// <param name="tolerance">The XY tolerance for detecting self-intersections.</param>
		public IEnumerable<Linestring> GetSelfIntersectingRings(double tolerance)
		{
			foreach (RingGroup ringGroup in RingGroups)
			{
				foreach (Linestring ring in ringGroup.GetLinestrings())
				{
					if (ring.IsEmpty || ! ring.IsClosed)
					{
						continue;
					}

					if (ring.IsVerticalRing(tolerance))
					{
						continue;
					}

					// Linear self-intersections (spikes / duplicate segment runs):
					bool hasLinearSelfIntersection =
						GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
							ring, tolerance, new List<Linestring>());

					// 0-dimensional self-crossings (figure-8 / bowtie rings):
					bool hasSelfCrossing =
						GeomTopoOpUtils.TryCrackSelfCrossingRing(
							ring, tolerance, new List<Linestring>());

					if (hasLinearSelfIntersection || hasSelfCrossing)
					{
						yield return ring;
					}
				}
			}
		}

		/// <summary>
		/// Returns the interior rings that are not actually part of their exterior face: an
		/// interior ring (a hole) must lie in the plane of its exterior ring and within its
		/// XY extent (OGC simple-polyhedron definition). A ring that is off that plane or
		/// outside the exterior footprint is most likely a different face mis-stored as an
		/// inner ring.
		/// </summary>
		/// <param name="tolerance">The tolerance for the coplanarity and XY-containment
		/// tests.</param>
		public IEnumerable<Linestring> GetInteriorRingsNotInExterior(double tolerance)
		{
			foreach (RingGroup ringGroup in RingGroups)
			{
				if (ringGroup.InteriorRingCount == 0)
				{
					continue;
				}

				Linestring exteriorRing = ringGroup.ExteriorRing;

				if (exteriorRing == null || exteriorRing.IsEmpty || ! exteriorRing.IsClosed)
				{
					continue;
				}

				Plane3D exteriorPlane =
					Plane3D.TryFitPlane(exteriorRing.GetPoints().ToList(), isRing: true);

				bool planeDefined = exteriorPlane != null && exteriorPlane.IsDefined;

				foreach (Linestring interiorRing in ringGroup.InteriorRings)
				{
					if (interiorRing.IsEmpty)
					{
						continue;
					}

					// Off the exterior ring's plane (a different face mis-stored as a hole):
					bool notCoplanar =
						planeDefined && ! IsCoplanar(interiorRing, exteriorPlane, tolerance);

					// Reaching outside the exterior ring's XY extent: any vertex that is
					// strictly outside (false, i.e. not inside and not on the boundary).
					bool notContainedInXY = interiorRing.GetPoints()
					                                    .Any(p => GeomRelationUtils.AreaContainsXY(
							                                         exteriorRing, p, tolerance,
							                                         disregardingOrientation:
							                                         true) == false);

					if (notCoplanar || notContainedInXY)
					{
						yield return interiorRing;
					}
				}
			}
		}

		private static bool IsCoplanar([NotNull] Linestring ring,
		                               [NotNull] Plane3D plane,
		                               double tolerance)
		{
			foreach (Pnt3D point in ring.GetPoints())
			{
				if (plane.GetDistanceAbs(point.X, point.Y, point.Z) > tolerance)
				{
					return false;
				}
			}

			return true;
		}

		private static RingGroup AsProperlyOriented([NotNull] RingGroup ringGroup)
		{
			RingGroup resultRingGroup = (RingGroup) ringGroup.Clone();

			bool? clockwiseOriented = resultRingGroup.ClockwiseOriented;

			if (clockwiseOriented == null)
			{
				return RingGroup.CreateEmpty();
			}

			if (clockwiseOriented == false)
			{
				resultRingGroup.ReverseOrientation();
			}

			return resultRingGroup;
		}

		public override MultiLinestring Clone()
		{
			List<RingGroup> clonedRingGroups =
				RingGroups.Select(r => (RingGroup) r.Clone()).ToList();

			return new Polyhedron(clonedRingGroups);
		}
	}
}
