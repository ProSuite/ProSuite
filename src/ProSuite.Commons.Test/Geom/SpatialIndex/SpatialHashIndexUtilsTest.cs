using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;
using ProSuite.Commons.Geom.SpatialIndex;

namespace ProSuite.Commons.Test.Geom.SpatialIndex
{
	[TestFixture]
	public class SpatialHashIndexUtilsTest
	{
		[Test]
		public void CanFindIdentifiersByPolycurve()
		{
			SpatialHashIndex<string> index = Index();

			index.Add("inside", 45, 45);
			index.Add("boundary", 22, 22);
			index.Add("outside", 5, 5);

			Dictionary<string, EnvelopeRelation> found = Find(index);

			Assert.AreEqual(EnvelopeRelation.Inside, found["inside"]);
			Assert.AreEqual(EnvelopeRelation.Straddling, found["boundary"]);
			Assert.IsFalse(found.ContainsKey("outside"),
			               "a tile the polycurve misses is not searched at all");
		}

		[Test]
		public void AnIdentifierInSeveralTilesIsReportedOnce()
		{
			SpatialHashIndex<string> index = Index();

			// Reaches [40,50] x [40,50], which the polygon contains, and [20,30] x [20,30],
			// which its corner cuts through.
			index.Add("spanning", 22, 22, 45, 45);

			Dictionary<string, EnvelopeRelation> found = Find(index);

			Assert.AreEqual(1, found.Count);
			Assert.AreEqual(EnvelopeRelation.Straddling, found["spanning"],
			                "one straddling tile is enough to make the item need a test");
		}

		[Test]
		public void AnIdentifierOnlyInContainedTilesNeedsNoTest()
		{
			SpatialHashIndex<string> index = Index();

			index.Add("spanning", 42, 42, 55, 55);

			Assert.AreEqual(EnvelopeRelation.Inside, Find(index)["spanning"]);
		}

		[Test]
		public void AnEmptyIndexFindsNothing()
		{
			Assert.IsEmpty(Find(Index()));
		}

		/// <summary>A polycurve reaching far beyond the index walks the tiles the index holds
		/// rather than the tiles it covers. The answer must not depend on which.</summary>
		[Test]
		public void APolycurveWiderThanTheIndexFindsTheSame()
		{
			SpatialHashIndex<string> index = Index();

			index.Add("inside", 45, 45);
			index.Add("outside", 5, 5);

			ISegmentList wide = Square(10, 10000);

			Dictionary<string, EnvelopeRelation> found =
				SpatialHashIndexUtils.FindIdentifiers(index, wide, _tolerance)
				                     .ToDictionary(entry => entry.Identifier,
				                                   entry => entry.Relation);

			Assert.AreEqual(EnvelopeRelation.Inside, found["inside"]);
			Assert.AreEqual(EnvelopeRelation.Straddling, found["outside"],
			                "the tile at the polygon's own corner straddles it");
		}

		private const double _tolerance = 0.001;

		private static SpatialHashIndex<string> Index()
		{
			return new SpatialHashIndex<string>(new TilingDefinition(0, 0, 10, 10), 16, 1);
		}

		private static Dictionary<string, EnvelopeRelation> Find(SpatialHashIndex<string> index)
		{
			return SpatialHashIndexUtils
			       .FindIdentifiers(index, Square(20, 80), _tolerance)
			       .ToDictionary(entry => entry.Identifier, entry => entry.Relation);
		}

		private static ISegmentList Square(double min, double max)
		{
			return GeomTestUtils.CreatePoly(
				new List<Pnt3D>
				{
					new Pnt3D(min, min, 0),
					new Pnt3D(min, max, 0),
					new Pnt3D(max, max, 0),
					new Pnt3D(max, min, 0)
				});
		}
	}
}
