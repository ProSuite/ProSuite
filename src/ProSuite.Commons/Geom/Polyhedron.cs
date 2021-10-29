using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;

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
				RingGroups = new List<RingGroup> {newGroup};
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

		public MultiLinestring GetXYFootprint(double tolerance)
		{
			if (RingGroups.Count == 0)
			{
				return MultiPolycurve.CreateEmpty();
			}

			if (RingGroups.Count == 1)
			{
				RingGroup ringGroup = RingGroups[0];

				return AsProperlyOriented(ringGroup);
			}

			MultiLinestring firstGroup = null;
			var otherRings = new List<Linestring>();
			foreach (RingGroup ringGroup in RingGroups)
			{
				if (firstGroup == null)
				{
					firstGroup = AsProperlyOriented(ringGroup);
				}
				else
				{
					otherRings.AddRange(AsProperlyOriented(ringGroup).GetLinestrings());
				}
			}

			var allRings = new MultiPolycurve(otherRings);

			// TODO: First delete duplicate lines to avoid error (Intersections seen twice) in union:
			//GeomTopoOpUtils.TryDeleteLinearSelfIntersectionsXY(allRings, tolerance, out spaghetti);
			MultiLinestring result =
				GeomTopoOpUtils.GetUnionAreasXY(Assert.NotNull(firstGroup), allRings,
				                                tolerance);

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
