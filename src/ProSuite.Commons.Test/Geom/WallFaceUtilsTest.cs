using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geom
{
	/// <summary>
	/// Tests for <see cref="WallFaceUtils"/>. The hard requirement everything is measured
	/// against is <see cref="WallAssert.AssertGapFree"/>: the faces must form a surface that is
	/// closed in 3D - adjacent faces share complete edges, no face boundary continues into
	/// another face, and every face is linearly (not just point-wise) connected to its
	/// neighbours.
	/// </summary>
	[TestFixture]
	public class WallFaceUtilsTest
	{
		private const double _tolerance = 1e-8;

		private static readonly WallCornerJoin[] _allJoins =
		{
			WallCornerJoin.Miter, WallCornerJoin.Bevel, WallCornerJoin.Round
		};

		#region The reported case

		[Test]
		public void CanBuildCornerWithBendAndSlopeChangeRight(
			[ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			// The reported case: the sketch bends in XY (by 90°) and changes slope at the same
			// vertex, so the two faces meet at different heights and would leave a Z gap.
			Polyhedron wall = GetWall(ReportedCase(), 10, BufferSide.Right, join);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
		}

		[Test]
		public void CanBuildCornerWithBendAndSlopeChangeLeft(
			[ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			Polyhedron wall = GetWall(ReportedCase(), 10, BufferSide.Left, join);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
		}

		[Test]
		public void CanBuildCornerWithBendAndSlopeChangeBothSides(
			[ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			Polyhedron wall = GetWall(ReportedCase(), 10, BufferSide.Both, join);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
			WallAssert.AssertFacesAreClockwise(wall);
		}

		[Test]
		public void MiteredCornerHasTheExpectedSegmentFaces()
		{
			// The mitered faces are exactly the ones from the problem report: the corner point
			// (10/90) is shared by both faces, at 9 on the first face's plane and at 14 on the
			// second face's plane.
			Polyhedron wall = GetWall(ReportedCase(), 10, BufferSide.Right, WallCornerJoin.Miter);

			WallAssert.AssertGapFree(wall);

			WallAssert.AssertHasFace(wall, "0/0/0 0/100/10 10/90/9 10/0/0");
			WallAssert.AssertHasFace(wall, "0/100/10 100/100/50 100/90/50 10/90/14");

			// ... plus the filler that closes the 9 -> 14 step at the corner point.
			WallAssert.AssertHasFace(wall, "0/100/10 10/90/14 10/90/9");

			Assert.AreEqual(3, wall.RingGroups.Count);
		}

		[Test]
		public void BevelledCornerHasTheExpectedSegmentFaces()
		{
			// Bevelling cuts the outside of the bend back to the two perpendicular offset points
			// and closes the wedge with a sloped triangle. Here the outside of the bend is the
			// un-offset (left) side, so the bevel has no effect and the result is the mitered one.
			Polyhedron wall = GetWall(ReportedCase(), 10, BufferSide.Right, WallCornerJoin.Bevel);

			WallAssert.AssertGapFree(wall);
			Assert.AreEqual(3, wall.RingGroups.Count);

			// Offsetting to the other side puts the bend's outside on the buffered side: the
			// faces end perpendicularly and the wedge between them becomes a sloped triangle
			// (no vertical filler at all, as the un-offset side has no Z step).
			Polyhedron left = GetWall(ReportedCase(), 10, BufferSide.Left, WallCornerJoin.Bevel);

			WallAssert.AssertGapFree(left);

			WallAssert.AssertHasFace(left, "0/0/0 -10/0/0 -10/100/10 0/100/10");
			WallAssert.AssertHasFace(left, "0/100/10 0/110/10 100/110/50 100/100/50");
			WallAssert.AssertHasFace(left, "0/100/10 -10/100/10 0/110/10");

			Assert.AreEqual(3, left.RingGroups.Count);
			WallAssert.AssertFacesArePlanar(left);
		}

		[Test]
		public void MiteredCornerFillerIsVerticalAndBevelledCornerFillerIsNot()
		{
			// The essence of the difference between the two joins: mitering must close the Z
			// step with a vertical ring (which is exactly what must not be dropped), bevelling
			// absorbs it in a sloped ring.
			Polyhedron mitered =
				GetWall(ReportedCase(), 10, BufferSide.Left, WallCornerJoin.Miter);
			Polyhedron bevelled =
				GetWall(ReportedCase(), 10, BufferSide.Left, WallCornerJoin.Bevel);

			Assert.AreEqual(1, WallAssert.GetVerticalFaceCount(mitered));
			Assert.AreEqual(0, WallAssert.GetVerticalFaceCount(bevelled));

			WallAssert.AssertGapFree(mitered);
			WallAssert.AssertGapFree(bevelled);
		}

		#endregion

		#region Coplanar runs are not sub-divided

		[Test]
		public void FlatCornerIsASingleFace(
			[ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			// A horizontal L is one plane, so the whole wall must be a single face - and the
			// bevel/round joins must not cut the corner back, which would open an XY gap.
			var path = Path("0/0/0 100/0/0 100/100/0");

			Polyhedron wall = GetWall(path, 5, BufferSide.Both, join);

			Assert.AreEqual(1, wall.RingGroups.Count);
			Assert.AreEqual(2000, wall.GetArea2D(), 0.001);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
		}

		[Test]
		public void ConstantSlopeRunIsASingleFace()
		{
			var path = Path("0/0/0 100/0/10 200/0/20 300/0/30");

			Polyhedron wall = GetWall(path, 5, BufferSide.Both, WallCornerJoin.Miter);

			Assert.AreEqual(1, wall.RingGroups.Count);
			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
		}

		[Test]
		public void OneSidedFlatCornerIsASingleFace()
		{
			var path = Path("0/0/0 100/0/0 100/100/0");

			Polyhedron wall = GetWall(path, 5, BufferSide.Left, WallCornerJoin.Miter);

			Assert.AreEqual(1, wall.RingGroups.Count);
			Assert.AreEqual(975, wall.GetArea2D(), 0.001);
			WallAssert.AssertGapFree(wall);
		}

		[Test]
		public void SlopeBreakWithoutBendNeedsNoFiller(
			[ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			// Straight in XY but with a slope break: the two faces meet flush along the full
			// cross-section, there is no Z step and therefore no filler.
			var path = Path("0/0/0 100/0/20 200/0/0");

			Polyhedron wall = GetWall(path, 5, BufferSide.Both, join);

			Assert.AreEqual(2, wall.RingGroups.Count);
			Assert.AreEqual(2000, wall.GetArea2D(), 0.001);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
			WallAssert.AssertNoOverlap(wall);
		}

		#endregion

		#region Corner shapes

		[Test]
		public void CanBuildAcuteCornerWithSlopeChange(
			[ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			// A 30° corner (a 150° turn): the miter runs far out, the bevel cuts one long chord
			// and the round join replaces it with a fan of triangles.
			var path = Path("0/0/0 100/0/5 " + PointAt(100, 0, 180 - 30, 100, 40));

			Polyhedron wall = GetWall(path, 5, BufferSide.Both, join);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
		}

		[Test]
		public void TheDefaultJoinIsRound()
		{
			var path = Path("0/0/0 100/0/5 " + PointAt(100, 0, 180 - 30, 100, 40));

			Polyhedron expected = GetWall(path, 5, BufferSide.Both, WallCornerJoin.Round);

			Polyhedron actual = WallFaceUtils.GetWallFaces(
				new List<Linestring> { path }, new List<double> { 5 }, BufferSide.Both,
				tolerance: _tolerance);

			Assert.NotNull(actual);
			Assert.AreEqual(expected.RingGroups.Count, actual.RingGroups.Count);
			Assert.AreEqual(expected.GetArea2D(), actual.GetArea2D(), 0.0001);
		}

		[Test]
		public void RoundJoinAddsMoreThanOneTriangleAtAcuteCorners()
		{
			var path = Path("0/0/0 100/0/5 " + PointAt(100, 0, 180 - 30, 100, 40));

			Polyhedron bevelled = GetWall(path, 5, BufferSide.Both, WallCornerJoin.Bevel);
			Polyhedron rounded = GetWall(path, 5, BufferSide.Both, WallCornerJoin.Round);

			// Two faces plus, on the outside of the bend, one chord triangle (bevel) resp. four
			// fan triangles for the 150° turn (round); plus the vertical filler on the inside.
			Assert.AreEqual(4, bevelled.RingGroups.Count);
			Assert.AreEqual(7, rounded.RingGroups.Count);

			WallAssert.AssertGapFree(bevelled);
			WallAssert.AssertGapFree(rounded);
		}

		[Test]
		public void CanBuildZigZagWithSlopeChanges(
			[ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			var path = Path("0/0/0 50/40/10 100/0/5 150/40/30 200/0/0 250/60/40");

			Polyhedron wall = GetWall(path, 4, BufferSide.Both, join);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
		}

		[Test]
		public void CanBuildVeryTightCornerWithoutGaps(
			[ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			// A hairpin whose miter point would run far beyond both segments: the corner point
			// is clamped, which lets the faces overlap slightly - but they must still be joined
			// without a gap.
			var path = Path("0/0/0 30/0/6 " + PointAt(30, 0, 180 - 8, 30, 20));

			Polyhedron wall = GetWall(path, 5, BufferSide.Both, join);

			WallAssert.AssertGapFree(wall, requireNoOverlap: false);
			WallAssert.AssertFacesArePlanar(wall);
		}

		[Test]
		public void CanBuildCornerWithOppositeSlopes(
			[ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			// The second segment falls where the first one rises, so the Z step at the corner
			// point is large.
			var path = Path("0/0/0 100/0/50 100/100/-50");

			Polyhedron wall = GetWall(path, 8, BufferSide.Both, join);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
		}

		#endregion

		#region Closed and multi-part sketches

		[Test]
		public void CanBuildClosedLoopWithSlopeChanges(
			[ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			var path = ClosedPath("0/0/0 100/0/10 100/100/25 0/100/5 0/0/0");

			Polyhedron wall = GetWall(path, 5, BufferSide.Both, join);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
			WallAssert.AssertFacesAreClockwise(wall);
		}

		[Test]
		public void CanBuildFlatClosedLoop()
		{
			// A completely coplanar loop must not become a single (annular) face.
			var path = ClosedPath("0/0/0 100/0/0 100/100/0 0/100/0 0/0/0");

			Polyhedron wall = GetWall(path, 5, BufferSide.Both, WallCornerJoin.Miter);

			Assert.AreEqual(2, wall.RingGroups.Count);
			Assert.AreEqual(4 * 100 * 10, wall.GetArea2D(), 0.001);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
		}

		[Test]
		public void CanBuildOneSidedClosedLoop()
		{
			var path = ClosedPath("0/0/0 100/0/10 100/100/25 0/100/5 0/0/0");

			Polyhedron wall = GetWall(path, 5, BufferSide.Right, WallCornerJoin.Miter);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
		}

		[Test]
		public void CanBuildMultipleParts()
		{
			var paths = new List<Linestring>
			            {
				            Path("0/0/0 0/100/10 100/100/50"),
				            Path("500/0/0 600/0/20 600/100/5")
			            };

			Polyhedron wall = WallFaceUtils.GetWallFaces(
				paths, new List<double> { 10, 8 }, BufferSide.Both, WallCornerJoin.Miter,
				tolerance: _tolerance);

			Assert.NotNull(wall);
			WallAssert.AssertGapFree(wall, partCount: 2);
			WallAssert.AssertFacesArePlanar(wall);
		}

		#endregion

		#region Degenerate input

		[Test]
		public void SingleSegmentIsASingleFace()
		{
			Polyhedron wall = GetWall(Path("0/0/0 100/50/20"), 5, BufferSide.Both,
			                          WallCornerJoin.Miter);

			Assert.AreEqual(1, wall.RingGroups.Count);
			WallAssert.AssertFacesArePlanar(wall);
		}

		[Test]
		public void DuplicateVerticesAreIgnored()
		{
			Polyhedron wall = GetWall(Path("0/0/0 0/100/10 0/100/10 100/100/50"), 10,
			                          BufferSide.Both, WallCornerJoin.Miter);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);
		}

		[Test]
		public void PurelyVerticalSegmentsAreIgnored()
		{
			// A sketch segment without a horizontal extent has neither a plane nor an offset
			// direction, so it cannot carry a face: it is dropped, and the wall is the one of
			// the remaining, horizontally extended segments.
			Polyhedron wall = GetWall(Path("0/0/0 0/100/10 0/100/30 100/100/50"), 10,
			                          BufferSide.Both, WallCornerJoin.Miter);

			WallAssert.AssertGapFree(wall);
			WallAssert.AssertFacesArePlanar(wall);

			Polyhedron withoutTheStep = GetWall(ReportedCase(), 10, BufferSide.Both,
			                                    WallCornerJoin.Miter);

			Assert.AreEqual(withoutTheStep.RingGroups.Count, wall.RingGroups.Count);
			Assert.AreEqual(withoutTheStep.GetArea2D(), wall.GetArea2D(), 0.001);
		}

		[Test]
		public void ZeroOffsetDistanceYieldsNoWall()
		{
			Polyhedron wall = WallFaceUtils.GetWallFaces(
				new List<Linestring> { Path("0/0/0 100/0/0") }, new List<double> { 0 },
				BufferSide.Both);

			Assert.IsNull(wall);
		}

		#endregion

		#region The footprint

		[Test]
		public void MiteredWallCoversTheMiteredBufferPolygon(
			[Values(BufferSide.Both, BufferSide.Left, BufferSide.Right)] BufferSide side)
		{
			// Mitering keeps the footprint of the buffer the tool draws: the faces tile exactly
			// the mitered, flat-ended buffer polygon.
			var path = Path("0/0/0 0/100/10 100/100/50 160/40/30");

			Polyhedron wall = GetWall(path, 10, side, WallCornerJoin.Miter);

			string message;
			MultiLinestring buffer = GeomTopoOpUtils.GetBufferedLine(
				new List<Linestring> { path }, new List<double> { 10 }, side, 0.001,
				out message, miteredCorners: true, flatEndCaps: true);

			Assert.NotNull(buffer, message);

			// The vertical fillers add no area, so the summed face area is the footprint area.
			Assert.AreEqual(buffer.GetArea2D(), wall.GetArea2D(), 0.01);

			WallAssert.AssertGapFree(wall);
		}

		[Test]
		public void BevelledWallStaysInsideTheMiteredBufferPolygon()
		{
			var path = Path("0/0/0 0/100/10 100/100/50 160/40/30");

			Polyhedron mitered = GetWall(path, 10, BufferSide.Both, WallCornerJoin.Miter);
			Polyhedron bevelled = GetWall(path, 10, BufferSide.Both, WallCornerJoin.Bevel);
			Polyhedron rounded = GetWall(path, 10, BufferSide.Both, WallCornerJoin.Round);

			// Cutting the outside of the bends back can only take area away, and the rounded
			// join keeps more of it than the single bevel chord.
			Assert.Less(bevelled.GetArea2D(), mitered.GetArea2D());
			Assert.Less(rounded.GetArea2D(), mitered.GetArea2D());
			Assert.Greater(rounded.GetArea2D(), bevelled.GetArea2D());
		}

		#endregion

		#region The gap check itself

		[Test]
		public void GapCheckDetectsAMissingFiller(
			[ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			// The check must have teeth: dropping the corner fillers - which is exactly the
			// defect this is all about - has to be reported.
			Polyhedron wall = GetWall(ReportedCase(), 10, BufferSide.Both, join);

			Assert.Greater(wall.RingGroups.Count, 2, "There is nothing to drop");

			var withoutFillers = new Polyhedron(
				wall.RingGroups.Where(g => g.ExteriorRing.SegmentCount > 3).ToList());

			Assert.AreEqual(2, withoutFillers.RingGroups.Count);

			Assert.Throws<AssertionException>(
				() => WallAssert.AssertGapFree(withoutFillers),
				"A wall without its corner fillers was not reported as having a gap");
		}

		[Test]
		public void GapCheckDetectsANonPlanarSingleFace()
		{
			// The alternative that was rejected - averaging the Z at the corner into one warped
			// face - is not planar, which must be reported too.
			var warped = new Polyhedron(
				new List<RingGroup>
				{
					new RingGroup(new Linestring(WallAssert.ParsePoints(
						                             "0/0/0 0/100/10 10/90/11.5 10/0/0 0/0/0")))
				});

			Assert.Throws<AssertionException>(() => WallAssert.AssertFacesArePlanar(warped));
		}

		#endregion

		#region Systematic sweep

		[Test]
		public void RandomWallsAreGapFree([ValueSource(nameof(_allJoins))] WallCornerJoin join)
		{
			// Segment lengths and turn angles are kept moderate enough that the mitered corner
			// point is never clamped, so the faces must tile the footprint exactly.
			var random = new Random(20260902);

			for (var run = 0; run < 300; run++)
			{
				BufferSide side = run % 3 == 0
					                  ? BufferSide.Both
					                  : run % 3 == 1
						                  ? BufferSide.Left
						                  : BufferSide.Right;

				Linestring path = GetRandomPath(random, 2 + run % 6);

				Polyhedron wall = GetWall(path, 5, side, join);

				Assert.NotNull(wall, "No wall for {0}", path);

				try
				{
					WallAssert.AssertGapFree(wall);
					WallAssert.AssertFacesArePlanar(wall);
					WallAssert.AssertFacesAreClockwise(wall);
				}
				catch (AssertionException e)
				{
					throw new AssertionException(
						string.Format("Run {0} ({1}, {2}), path {3}: {4}", run, side, join,
						              WallAssert.Format(path), e.Message), e);
				}
			}
		}

		[NotNull]
		private static Linestring GetRandomPath([NotNull] Random random, int segmentCount)
		{
			var points = new List<Pnt3D> { new Pnt3D(0, 0, 0) };

			double direction = random.NextDouble() * 2 * Math.PI;

			for (var i = 0; i < segmentCount; i++)
			{
				if (i > 0)
				{
					// Up to 150°: beyond that the miter limit (10) would kick in.
					direction += (random.NextDouble() - 0.5) * 2 * (150 * Math.PI / 180);
				}

				double length = 30 + random.NextDouble() * 70;

				Pnt3D previous = points[points.Count - 1];

				points.Add(new Pnt3D(previous.X + length * Math.Cos(direction),
				                     previous.Y + length * Math.Sin(direction),
				                     previous.Z + (random.NextDouble() - 0.5) * 40));
			}

			return new Linestring(points);
		}

		#endregion

		#region Test utils

		[NotNull]
		private static Linestring ReportedCase()
		{
			return Path("0/0/0 0/100/10 100/100/50");
		}

		[NotNull]
		private static Polyhedron GetWall([NotNull] Linestring path, double distance,
		                                  BufferSide side, WallCornerJoin join)
		{
			Polyhedron result = WallFaceUtils.GetWallFaces(
				new List<Linestring> { path }, new List<double> { distance }, side, join,
				tolerance: _tolerance);

			Assert.NotNull(result);

			return result;
		}

		/// <summary>
		/// A linestring from a "x/y/z x/y/z ..." string.
		/// </summary>
		[NotNull]
		private static Linestring Path([NotNull] string points)
		{
			return new Linestring(WallAssert.ParsePoints(points));
		}

		[NotNull]
		private static Linestring ClosedPath([NotNull] string points)
		{
			Linestring result = Path(points);

			Assert.IsTrue(result.IsClosed, "Not a closed path");

			return result;
		}

		/// <summary>
		/// The point at the given bearing (in degrees, counter-clockwise from the positive X
		/// axis) and distance from (x, y), as a "x/y/z" string.
		/// </summary>
		[NotNull]
		private static string PointAt(double x, double y, double bearing, double distance,
		                              double z)
		{
			double radians = bearing * Math.PI / 180;

			return string.Format("{0}/{1}/{2}", x + distance * Math.Cos(radians),
			                     y + distance * Math.Sin(radians), z);
		}

		#endregion
	}
}
