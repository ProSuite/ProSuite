using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// A collection of ring groups that typically build a (closed or un-closed) surface.
	/// By (OGC-)definition the ring groups should be connected (i.e. touch in 3D). However, this
	/// is not currently enforced. Therefore multi-surfaces (or multipatches) can also be modeled
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

		public MultiLinestring GetXYFootprint(double tolerance,
		                                      double verticalRingDetectionTolerance,
		                                      out List<Linestring> verticalRings)
		{
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
				IEnumerable<RingGroup> cleanedGroups = CleanupDuplicateSegments(
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

			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(ringGroupsToUnionize, tolerance);

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
		private static IEnumerable<RingGroup> CleanupDuplicateSegments(
			[NotNull] RingGroup input,
			double verticalRingDetectionTolerance,
			[NotNull] out List<Linestring> verticalRings)
		{
			verticalRings = new List<Linestring>();

			List<Linestring> cleanedExteriors = CleanRing(
				input.ExteriorRing, verticalRingDetectionTolerance, verticalRings);

			if (cleanedExteriors.Count == 0)
			{
				return Enumerable.Empty<RingGroup>();
			}

			var cleanedInteriors = new List<Linestring>();
			foreach (Linestring interior in input.InteriorRings)
			{
				cleanedInteriors.AddRange(
					CleanRing(interior, verticalRingDetectionTolerance, verticalRings));
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

		private static List<Linestring> CleanRing(
			[NotNull] Linestring ring,
			double verticalRingDetectionTolerance,
			[NotNull] List<Linestring> verticalRings)
		{
			var candidates = new List<Linestring>();
			var spaghetti = new List<Linestring>();

			if (! GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
				    ring, verticalRingDetectionTolerance, spaghetti))
			{
				// No self-intersection was removed: pass the original ring through.
				candidates.Add(ring);
				return candidates;
			}

			if (spaghetti.Count == 0)
			{
				// All segments cancelled out: the original ring was entirely vertical.
				verticalRings.Add(ring);
				return candidates;
			}

			foreach (Linestring fragment in spaghetti)
			{
				if (fragment.IsClosed &&
				    ! fragment.IsVerticalRing(verticalRingDetectionTolerance))
				{
					candidates.Add(fragment);
				}
				else
				{
					verticalRings.Add(fragment);
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
