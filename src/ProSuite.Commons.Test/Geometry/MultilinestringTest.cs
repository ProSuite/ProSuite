using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.Test.Geometry
{
	[TestFixture]
	public class MultilinestringTest
	{
		[Test]
		public void CanUseBasicProperties()
		{
			var multilinestring = new MultiPolycurve(
				new[]
				{
					new Linestring(new[]
					               {
						               new Pnt3D(-5, -15, -25),
						               new Pnt3D(0, 0, 0)
					               }),
					new Linestring(new[]
					               {
						               new Pnt3D(44, 45, 123),
						               new Pnt3D(33, 33, 123),
						               new Pnt3D(22, 22, 12)
					               })
				});

			Assert.AreEqual(2, multilinestring.Count);
			Assert.AreEqual(multilinestring.Count, multilinestring.GetLinestrings().Count());

			Assert.AreEqual(3, multilinestring.SegmentCount);
			Assert.AreEqual(multilinestring.SegmentCount, multilinestring.Count());

			Assert.AreEqual(5, multilinestring.PointCount);
			Assert.AreEqual(multilinestring.PointCount, multilinestring.GetPoints().Count());

			Assert.False(multilinestring.IsClosed);
			Assert.False(multilinestring.IsEmpty);

			int partIndex;
			Assert.True(multilinestring.IsFirstPointInPart(0, out partIndex));
			Assert.AreEqual(0, partIndex);
			Assert.True(multilinestring.IsFirstPointInPart(2, out partIndex));
			Assert.AreEqual(1, partIndex);

			Assert.True(multilinestring.IsFirstSegmentInPart(0));
			Assert.True(multilinestring.IsFirstSegmentInPart(1));
			Assert.True(multilinestring.IsLastPointInPart(1, out partIndex));
			Assert.AreEqual(0, partIndex);
			Assert.True(multilinestring.IsLastPointInPart(4, out partIndex));
			Assert.AreEqual(1, partIndex);

			Assert.True(multilinestring.IsLastSegmentInPart(0));
			Assert.True(multilinestring.IsLastSegmentInPart(2));

			const double xMin = -5;
			Assert.AreEqual(xMin, multilinestring.GetLinestring(0).XMin);
			Assert.AreEqual(xMin, multilinestring.XMin);

			const double xMax = 44;
			Assert.AreEqual(xMax, multilinestring.GetLinestring(1).XMax);
			Assert.AreEqual(xMax, multilinestring.XMax);

			const double yMin = -15;
			Assert.AreEqual(yMin, multilinestring.GetLinestring(0).YMin);
			Assert.AreEqual(yMin, multilinestring.YMin);

			const double yMax = 45;
			Assert.AreEqual(yMax, multilinestring.GetLinestring(1).YMax);
			Assert.AreEqual(yMax, multilinestring.YMax);

			int partIdx;
			int localSegmentIndex = multilinestring.GetLocalSegmentIndex(2, out partIdx);

			Assert.AreEqual(1, partIdx);
			Assert.AreEqual(1, localSegmentIndex);
			Assert.AreEqual(2, multilinestring.GetGlobalSegmentIndex(partIdx, localSegmentIndex));

			localSegmentIndex = multilinestring.GetLocalSegmentIndex(0, out partIdx);
			Assert.AreEqual(0, partIdx);
			Assert.AreEqual(0, localSegmentIndex);
			Assert.AreEqual(0, multilinestring.GetGlobalSegmentIndex(partIdx, localSegmentIndex));

			Assert.AreEqual(multilinestring.GetLinestrings().Sum(l => l.GetLength2D()),
			                multilinestring.GetLength2D());
		}

		[Test]
		public void CanNavigateRings()
		{
			var multilinestring = new MultiPolycurve(
				new[]
				{
					new Linestring(new[]
					               {
						               new Pnt3D(0, 0, 9),
						               new Pnt3D(0, 100, 9),
						               new Pnt3D(100, 100, 9),
						               new Pnt3D(100, 0, 9),
						               new Pnt3D(0, 0, 9)
					               }),
					new Linestring(new[]
					               {
						               // interior ring
						               new Pnt3D(40, 40, 123),
						               new Pnt3D(60, 40, 123),
						               new Pnt3D(60, 60, 12),
						               new Pnt3D(40, 60, 12),
						               new Pnt3D(40, 40, 123)
					               })
				});

			Assert.True(multilinestring.IsClosed);

			int partIdx;
			int localSegmentIdx = multilinestring.GetLocalSegmentIndex(5, out partIdx);
			Assert.AreEqual(6, multilinestring.GetSegmentStartPointIndex(5));

			Assert.AreEqual(1, partIdx);
			Assert.AreEqual(1, localSegmentIdx);

			Line3D previousSegment = multilinestring.PreviousSegment(5);
			Assert.AreEqual(multilinestring.GetSegment(4), previousSegment);
			Assert.AreEqual(multilinestring.GetSegment(1, 0), previousSegment);

			int? previousSegmentIdx = multilinestring.PreviousSegmentIndex(5);
			Assert.NotNull(previousSegmentIdx);
			Assert.AreEqual(4, previousSegmentIdx);

			previousSegmentIdx = multilinestring.PreviousSegmentIndex(previousSegmentIdx.Value);
			Assert.NotNull(previousSegmentIdx);
			Assert.AreEqual(7, previousSegmentIdx);

			int? nextSegmentIndex = multilinestring.NextSegmentIndex(previousSegmentIdx.Value);
			Assert.NotNull(nextSegmentIndex);
			Assert.AreEqual(4, nextSegmentIndex);

			Line3D nextSegment = multilinestring.NextSegment(previousSegmentIdx.Value);
			Assert.AreEqual(multilinestring.GetSegment(4), nextSegment);
			Assert.AreEqual(multilinestring.GetSegment(1, 0), nextSegment);
			Assert.AreEqual(5, multilinestring.GetSegmentStartPointIndex(4));

			// With the exterior ring:
			localSegmentIdx = multilinestring.GetLocalSegmentIndex(1, out partIdx);

			Assert.AreEqual(0, partIdx);
			Assert.AreEqual(1, localSegmentIdx);

			previousSegment = multilinestring.PreviousSegment(1);
			Assert.AreEqual(multilinestring.GetSegment(0), previousSegment);
			Assert.AreEqual(multilinestring.GetSegment(0, 0), previousSegment);

			previousSegmentIdx = multilinestring.PreviousSegmentIndex(1);
			Assert.NotNull(previousSegmentIdx);
			Assert.AreEqual(0, previousSegmentIdx);

			previousSegmentIdx = multilinestring.PreviousSegmentIndex(previousSegmentIdx.Value);
			Assert.NotNull(previousSegmentIdx);
			Assert.AreEqual(3, previousSegmentIdx);

			nextSegmentIndex = multilinestring.NextSegmentIndex(previousSegmentIdx.Value);
			Assert.NotNull(nextSegmentIndex);
			Assert.AreEqual(0, nextSegmentIndex);

			nextSegment = multilinestring.NextSegment(previousSegmentIdx.Value);

			Assert.AreEqual(multilinestring.GetSegment(0), nextSegment);
			Assert.AreEqual(multilinestring.GetSegment(0, 0), nextSegment);
		}

		[Test]
		public void CanSnapToResolution()
		{
			var ring = new List<Pnt3D>();

			ring.Add(new Pnt3D(2600000.0001234, 1200000.0004321, 500.0004321));
			ring.Add(new Pnt3D(2600080.0001234, 1200060.0004321, 500.0004321));
			ring.Add(new Pnt3D(2600080.0001234, 1200030.0004321, 540.0004321));
			ring.Add(new Pnt3D(2600080.0001234, 1200000.0004321, 530.0004321));

			Linestring l = new Linestring(ring);

			var multiPolycurve = new MultiPolycurve(new[] {l});

			MultiLinestring snapped = multiPolycurve.Clone();

			snapped.SnapToResolution(0.001, 2000000, 1000000);

			Assert.IsTrue(GeomRelationUtils.AreBoundsEqual(multiPolycurve, snapped, 0.001));

			EnvelopeXY bbAfterSnap = new EnvelopeXY(2600000, 1200000, 2600080, 1200060);
			Assert.IsTrue(GeomRelationUtils.AreBoundsEqual(bbAfterSnap, snapped, 0));
		}
	}
}
