using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;

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

			foreach (RingGroup ringGroup in RingGroups)
			{
				// Split all the rings at linear self-intersections and remove spikes, i.e.
				// duplicate segments sticking out. This makes ClockwiseOriented correct
				// for each cleaned ring before AsProperlyOriented runs. This operation
				// can change the ring's orientation!
				IEnumerable<RingGroup> cleanedGroups = CleanupDuplicateSegments(
					ringGroup, verticalRingDetectionTolerance,
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
