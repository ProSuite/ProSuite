﻿using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Geom
{
	/// <summary>
	/// An exterior ring with its associated interior rings, i.e. a Polygon in OGC lingo.
	/// </summary>
	public class RingGroup : MultiLinestring
	{
		public static RingGroup CreateProperlyOriented(
			[NotNull] Linestring exteriorRing,
			[CanBeNull] ICollection<Linestring> interiorRings = null)
		{
			Assert.True(exteriorRing.IsClosed, "Exterior ring is not closed");

			exteriorRing.TryOrientClockwise();

			if (interiorRings == null || interiorRings.Count == 0)
			{
				return new RingGroup(exteriorRing);
			}

			foreach (Linestring interiorRing in interiorRings)
			{
				Assert.True(interiorRing.IsClosed, "Interior ring is not closed");
				interiorRing.TryOrientAnticlockwise();
			}

			return new RingGroup(exteriorRing, interiorRings);
		}

		public RingGroup([NotNull] Linestring exteriorRing)
			: base(new[] {exteriorRing}) { }

		public RingGroup([NotNull] Linestring exteriorRing,
		                 IEnumerable<Linestring> interiorRings)
			: base(new[] {exteriorRing}.Concat(interiorRings)) { }

		public Linestring ExteriorRing => Count > 0 ? Linestrings[0] : null;

		public IEnumerable<Linestring> InteriorRings
		{
			get
			{
				for (int i = 1; i < Count; i++)
				{
					yield return Linestrings[i];
				}
			}
		}

		public bool IsVertical(double tolerance) =>
			Linestrings.All(l => l.IsVerticalRing(tolerance));

		public int InteriorRingCount => Count - 1;

		public bool? ClockwiseOriented => ExteriorRing.ClockwiseOriented;

		/// <summary>
		/// Optional id which could be used to remember the ring index or point IDs for multipatch
		/// geometries generated by StereoAnalyst.
		/// </summary>
		public int? Id { get; set; }

		public override MultiLinestring Clone()
		{
			var result = new RingGroup(ExteriorRing.Clone(),
			                           InteriorRings.Select(i => i.Clone()))
			             {
				             Id = Id
			             };

			return result;
		}

		public void AddInteriorRing(Linestring interiorRing)
		{
			Assert.False(IsEmpty, "Cannot add interior ring to empty RingGroup.");

			AddLinestring(interiorRing);
		}

		public void TryOrientProperly()
		{
			ExteriorRing.TryOrientClockwise();

			foreach (Linestring interiorRing in InteriorRings)
			{
				interiorRing.TryOrientAnticlockwise();
			}
		}
	}
}