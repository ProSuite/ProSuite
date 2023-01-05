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
		                                      out List<Linestring> verticalRings)
		{
			verticalRings = new List<Linestring>();

			if (RingGroups.Count == 0)
			{
				return MultiPolycurve.CreateEmpty();
			}

			if (RingGroups.Count == 1)
			{
				RingGroup ringGroup = RingGroups[0];
				return AsProperlyOriented(ringGroup);
			}

			var ringGroupsToUnionize = new List<RingGroup>();

			foreach (RingGroup ringGroup in RingGroups)
			{
				MultiLinestring orientedGroup = AsProperlyOriented(ringGroup);

				if (orientedGroup.IsEmpty)
				{
					verticalRings.AddRange(ringGroup.GetLinestrings());
					continue;
				}

				// Remove vertical rings and parts of rings
				Linestring exteriorRing = ((RingGroup) orientedGroup).ExteriorRing;

				List<Linestring> spaghetti = new List<Linestring>();
				if (GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(
					    exteriorRing, tolerance, spaghetti))
				{
					// TODO: Deal with interior rings in case the outer ring is not simple

					if (spaghetti.Count == 0)
					{
						verticalRings.Add(exteriorRing);
					}
					else
					{
						foreach (Linestring spaghetto in spaghetti)
						{
							if (spaghetto.IsClosed && spaghetto.ClockwiseOriented == true &&
							    ! spaghetto.IsVerticalRing(tolerance))
							{
								ringGroupsToUnionize.Add(new RingGroup(spaghetto));
							}
							else
							{
								verticalRings.Add(spaghetto);
							}
						}
					}
				}
				else
				{
					ringGroupsToUnionize.Add(ringGroup);
				}
			}

			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(ringGroupsToUnionize, tolerance);

			return result;
		}

		private static MultiLinestring AsProperlyOriented(RingGroup ringGroup)
		{
			RingGroup resultRingGroup = (RingGroup) ringGroup.Clone();

			bool? clockwiseOriented = resultRingGroup.ClockwiseOriented;

			if (clockwiseOriented == null)
			{
				return MultiPolycurve.CreateEmpty();
			}
			else
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
