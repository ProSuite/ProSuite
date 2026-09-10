using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	/// <summary>
	/// Buildings from the July 2026 delivery (Kaspar) whose footprint differed from the AGD
	/// reference at every tolerance. <c>kaspar_july_&lt;case&gt;.wkb</c> is the multipatch
	/// of one such building, the expected areas are those of the AGD reference.
	/// </summary>
	[TestFixture]
	public class KasparJulyHardCaseGeomTest
	{
		private const double _tolerance = 0.003125;

		/// <summary>
		/// The courtyard of a warped roof face must survive: this face is 0.75 m off its own
		/// best-fit plane, so measuring its interior ring against that plane with an absolute
		/// tolerance declared the courtyard "not coplanar", promoted it to an exterior ring
		/// and filled it in (147.6 instead of 83.7 m2).
		/// </summary>
		[Test]
		public void CanKeepCourtyardOfNonPlanarRoofFace()
		{
			MultiLinestring footprint = GetFootprint("kaspar_july_hole_147");

			Assert.AreEqual(2, footprint.PartCount, "exterior ring plus the courtyard");
			Assert.AreEqual(1, CountInteriorRings(footprint), "the courtyard");
			Assert.AreEqual(83.746, footprint.GetArea2D(), 0.01);
		}

		private static MultiLinestring GetFootprint(string stem)
		{
			return ReadPolyhedron(stem).GetXYFootprint(_tolerance, _tolerance, out _);
		}

		/// <summary>
		/// A multipatch .wkb comes back as a MultiPolyhedron even when it holds a single
		/// polyhedron; merge all its ring groups into one.
		/// </summary>
		private static Polyhedron ReadPolyhedron(string stem)
		{
			object geometry = GeomUtils.FromWkbFile(
				GeomTestUtils.GetGeometryTestDataPath($"{stem}.wkb"), out _);

			return geometry is Polyhedron single
				       ? single
				       : new Polyhedron(((MultiPolyhedron) geometry)
				                        .Polyhedra.SelectMany(p => p.RingGroups).ToList());
		}

		private static int CountInteriorRings(MultiLinestring rings)
		{
			return rings.GetLinestrings().Count(r => r.GetArea2D() < 0);
		}
	}
}
