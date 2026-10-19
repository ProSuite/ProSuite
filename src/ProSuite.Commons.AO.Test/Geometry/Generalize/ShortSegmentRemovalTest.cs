using System;
using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.Geometry.Generalize
{
	/// <summary>
	/// Tests for removing short segments from a polycurve. While the segments are removed one
	/// after the other, the removal keeps a list of the segments that are still short instead of
	/// measuring every segment of the geometry again after each single removal. These tests
	/// check that the two agree, and that the removal is fast enough on a geometry with many
	/// parts and many short segments.
	/// </summary>
	[TestFixture]
	public class ShortSegmentRemovalTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			Environment.SetEnvironmentVariable(
				SegmentReplacementUtils.VerifyShortSegmentRemovalEnvironmentVariable, null);

			TestUtils.ReleaseLicense();
		}

		[Test]
		public void CanRemoveShortSegmentsFromHugePolygon()
		{
			// 3m leaves about 1600 short segments in this polygon, spread over many parts and
			// with many of them next to each other. That is the situation which used to take
			// minutes, because the geometry was measured again after every single removal.
			const double minimumSegmentLength = 3;

			IPolygon polygon = ReadHugePolygon();

			int vertexCount = ((IPointCollection) polygon).PointCount;
			int partCount = ((IGeometryCollection) polygon).GeometryCount;

			int shortSegmentCountBefore = GetShortSegments(polygon, minimumSegmentLength).Count;

			Assert.Greater(shortSegmentCountBefore, 1000,
			               "The test geometry must contain many short segments");

			Stopwatch watch = Stopwatch.StartNew();

			RemoveShortSegments(polygon, minimumSegmentLength);

			watch.Stop();

			int shortSegmentCountAfter = GetShortSegments(polygon, minimumSegmentLength).Count;

			Console.WriteLine(
				$"Removed {shortSegmentCountBefore - shortSegmentCountAfter} of " +
				$"{shortSegmentCountBefore} short segments from a polygon with {vertexCount} " +
				$"vertices in {partCount} parts in {watch.ElapsedMilliseconds}ms");

			// Some short segments legitimately remain: a part that would become invalid by
			// further removal is protected. But the great majority must be gone.
			Assert.Less(shortSegmentCountAfter, shortSegmentCountBefore / 10,
			            "Most of the short segments should have been removed");
		}

		/// <summary>
		/// Removes the short segments of the large test polygon with the comparison against a
		/// full measurement of the geometry switched on. Every single update of the list of
		/// remaining short segments is then checked against measuring all segments again, which
		/// is what the removal did before that list was introduced. Any difference fails the
		/// test.
		/// </summary>
		[Test]
		public void HugePolygonUpdatesMatchFullMeasurement()
		{
			// The comparison measures every segment of the geometry again after every single
			// removal, so use a part of the polygon rather than all of it to keep the test time
			// reasonable. 3m leaves many short segments next to each other, which is what makes
			// the second removal phase run.
			const double minimumSegmentLength = 3;
			const int partCount = 300;

			Environment.SetEnvironmentVariable(
				SegmentReplacementUtils.VerifyShortSegmentRemovalEnvironmentVariable, "true");

			try
			{
				IPolygon polygon = KeepFirstParts(ReadHugePolygon(), partCount);

				IList<esriSegmentInfo> shortSegments =
					GetShortSegments(polygon, minimumSegmentLength);

				int shortSegmentCountBefore = shortSegments.Count;

				Assert.Greater(shortSegmentCountBefore, 100,
				               "The test geometry must contain many short segments");

				Assert.Greater(CountNeighbouringShortSegments(shortSegments), 10,
				               "The test geometry must contain short segments next to each " +
				               "other, otherwise the second removal phase, which is the one " +
				               "that keeps the list of remaining short segments, does not run");

				Stopwatch watch = Stopwatch.StartNew();

				RemoveShortSegments(polygon, minimumSegmentLength);

				watch.Stop();

				Console.WriteLine(
					$"Removed {shortSegmentCountBefore} short segments with the comparison " +
					$"against a full measurement switched on in {watch.ElapsedMilliseconds}ms. " +
					$"That is roughly what the removal cost before the list of remaining short " +
					$"segments was introduced.");
			}
			finally
			{
				Environment.SetEnvironmentVariable(
					SegmentReplacementUtils.VerifyShortSegmentRemovalEnvironmentVariable, null);
			}
		}

		/// <summary>
		/// The same comparison on rings that consist almost entirely of short segments. This is
		/// the case the removal has to get right: removing one segment merges its two
		/// neighbours, and the merged segment may or may not be short itself.
		/// </summary>
		[Test]
		public void AdjacentShortSegmentUpdatesMatchFullMeasurement()
		{
			const double minimumSegmentLength = 2.5;

			Environment.SetEnvironmentVariable(
				SegmentReplacementUtils.VerifyShortSegmentRemovalEnvironmentVariable, "true");

			try
			{
				var random = new Random(1234);

				for (var run = 0; run < 20; run++)
				{
					IPolygon polygon = CreateRandomRings(random, 4, 80);

					IList<esriSegmentInfo> shortSegments =
						GetShortSegments(polygon, minimumSegmentLength);

					Assert.Greater(shortSegments.Count, 20,
					               "The rings must contain many short segments");

					Assert.Greater(CountNeighbouringShortSegments(shortSegments), 20,
					               "The rings must contain short segments next to each other");

					RemoveShortSegments(polygon, minimumSegmentLength);
				}
			}
			finally
			{
				Environment.SetEnvironmentVariable(
					SegmentReplacementUtils.VerifyShortSegmentRemovalEnvironmentVariable, null);
			}
		}

		[NotNull]
		private static IPolygon ReadHugePolygon()
		{
			string filePath = TestData.GetHugeLockergesteinPolygonPath();

			return (IPolygon) TestUtils.ReadGeometryFromXml(filePath);
		}

		private static void RemoveShortSegments([NotNull] IPolycurve polycurve,
		                                        double minimumSegmentLength)
		{
			IList<esriSegmentInfo> shortSegments =
				GetShortSegments(polycurve, minimumSegmentLength);

			const bool use2DLengthOnly = true;

			SegmentReplacementUtils.RemoveShortSegments(
				polycurve, shortSegments, minimumSegmentLength, use2DLengthOnly, null, null,
				null);

			((ISegmentCollection) polycurve).SegmentsChanged();
		}

		/// <summary>
		/// A copy of the polygon that keeps only its first parts.
		/// </summary>
		[NotNull]
		private static IPolygon KeepFirstParts([NotNull] IPolygon polygon, int partCount)
		{
			var result = (IPolygon) GeometryFactory.Clone(polygon);

			var parts = (IGeometryCollection) result;

			if (parts.GeometryCount > partCount)
			{
				parts.RemoveGeometries(partCount, parts.GeometryCount - partCount);
				parts.GeometriesChanged();
			}

			return result;
		}

		/// <summary>
		/// The number of short segments that directly follow another short segment. The first
		/// removal phase skips those, which is what makes the second phase run.
		/// </summary>
		private static int CountNeighbouringShortSegments(
			[NotNull] IList<esriSegmentInfo> shortSegments)
		{
			var count = 0;

			for (var i = 1; i < shortSegments.Count; i++)
			{
				if (shortSegments[i].iPart == shortSegments[i - 1].iPart &&
				    shortSegments[i].iAbsSegment == shortSegments[i - 1].iAbsSegment + 1)
				{
					count++;
				}
			}

			return count;
		}

		[NotNull]
		private static IList<esriSegmentInfo> GetShortSegments(
			[NotNull] IPolycurve polycurve, double minimumSegmentLength)
		{
			const bool use3DLength = false;

			return GeometryUtils.GetShortSegments(polycurve, minimumSegmentLength, null,
			                                      use3DLength);
		}

		/// <summary>
		/// Rings whose vertices lie close enough together that many of their segments are
		/// shorter than the minimum segment length.
		/// </summary>
		[NotNull]
		private static IPolygon CreateRandomRings([NotNull] Random random, int ringCount,
		                                          int verticesPerRing)
		{
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			var rings = new List<IRing>();

			for (var ringIndex = 0; ringIndex < ringCount; ringIndex++)
			{
				var points = new List<WKSPointZ>();

				double centerX = 2600000 + ringIndex * 500;
				const double centerY = 1200000;
				const double radius = 20;

				for (var i = 0; i < verticesPerRing; i++)
				{
					double angle = 2 * Math.PI * i / verticesPerRing;

					// A radius that jitters by up to about 1.5m keeps the segment lengths varied
					// while all of them stay below the minimum segment length used here.
					double jitter = random.NextDouble() * 1.5;

					points.Add(new WKSPointZ
					           {
						           X = centerX + (radius + jitter) * Math.Cos(angle),
						           Y = centerY + (radius + jitter) * Math.Sin(angle),
						           Z = 500
					           });
				}

				points.Add(points[0]);

				rings.Add(GeometryFactory.CreateRing(points.ToArray(), lv95));
			}

			return GeometryFactory.CreatePolygon(rings, lv95);
		}
	}
}
