using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class RouteMeasuresTest
	{
		private const int _routeId = 1;

		[Test]
		public void CanGetOverlapFor2Features()
		{
			var ranges = new RouteMeasures(new[] { 0.001 }, new[] { 0.001 });

			ranges.Add(_routeId, new CurveMeasureRange(1, 0, 0, 100));
			ranges.Add(_routeId, new CurveMeasureRange(2, 0, 90, 200));

			var overlaps = new List<OverlappingMeasures>(ranges.GetOverlaps());

			Assert.AreEqual(1, overlaps.Count);
			OverlappingMeasures overlap = overlaps[0];

			Assert.AreEqual(_routeId, overlap.RouteId);

			Assert.AreEqual(90, overlap.MMin);
			Assert.AreEqual(100, overlap.MMax);

			List<TestRowReference> features = overlap.Features.ToList();

			Assert.AreEqual(2, features.Count);
		}

		[Test]
		public void CanGetOverlapFor2FeaturesContainer()
		{
			var ranges = new RouteMeasures(new[] { 0.001 }, new[] { 0.001 });

			ranges.Add(_routeId, new CurveMeasureRange(1, 0, 50, 60));
			ranges.Add(_routeId, new CurveMeasureRange(2, 0, 0, 100));

			var overlaps = new List<OverlappingMeasures>(ranges.GetOverlaps());

			Assert.AreEqual(1, overlaps.Count);
			OverlappingMeasures overlap = overlaps[0];

			Assert.AreEqual(_routeId, overlap.RouteId);

			Assert.AreEqual(50, overlap.MMin);
			Assert.AreEqual(60, overlap.MMax);

			List<TestRowReference> features = overlap.Features.ToList();

			Assert.AreEqual(2, features.Count);
		}

		[Test]
		public void CanGetOverlapFor3Features()
		{
			var ranges = new RouteMeasures(new[] { 0.001 }, new[] { 0.001 });

			ranges.Add(_routeId, new CurveMeasureRange(1, 0, 0, 100));
			ranges.Add(_routeId, new CurveMeasureRange(2, 0, 0, 100));
			ranges.Add(_routeId, new CurveMeasureRange(3, 0, 0, 100));

			var overlaps = new List<OverlappingMeasures>(ranges.GetOverlaps());

			Assert.AreEqual(3, overlaps.Count);
			foreach (OverlappingMeasures overlap in overlaps)
			{
				Assert.AreEqual(0, overlap.MMin);
				Assert.AreEqual(100, overlap.MMax);
				Assert.AreEqual(_routeId, overlap.RouteId);

				List<TestRowReference> features = overlap.Features.ToList();

				Assert.AreEqual(2, features.Count);
			}
		}

		[Test]
		public void CanIgnoreDisjointRanges()
		{
			var ranges = new RouteMeasures(new[] { 0.001 }, new[] { 0.001 });

			ranges.Add(_routeId, new CurveMeasureRange(1, 0, 0, 100));
			ranges.Add(_routeId, new CurveMeasureRange(2, 0, 200, 300));

			var overlaps = new List<OverlappingMeasures>(ranges.GetOverlaps());

			Assert.AreEqual(0, overlaps.Count);
		}

		[Test]
		public void CanGetOverlapForContinuousRangesAtDisjointEndpoints()
		{
			var ranges = new RouteMeasures(new[] { 0.001 }, new[] { 0.001 });

			var range1 = new CurveMeasureRange(1, 0, 0, 100.00001)
			             {
				             MMaxEndPoint = new Location(1000, 1000)
			             };

			var range2 = new CurveMeasureRange(2, 0, 100, 200)
			             {
				             MMinEndPoint = new Location(1000, 2000) // disjoint
			             };

			ranges.Add(_routeId, range1);
			ranges.Add(_routeId, range2);

			var overlaps = new List<OverlappingMeasures>(ranges.GetOverlaps());

			Assert.AreEqual(1, overlaps.Count);
		}

		[Test]
		public void CanIgnoreContinuousRangesAtConnectedEndpoints()
		{
			var ranges = new RouteMeasures(new[] { 0.001 }, new[] { 0.001 });

			var range1 = new CurveMeasureRange(1, 0, 0, 100.00001)
			             {
				             MMaxEndPoint = new Location(1000, 1000)
			             };

			var range2 = new CurveMeasureRange(2, 0, 100, 200)
			             {
				             MMinEndPoint = new Location(1000, 1000.0005) // within tolerance
			             };

			ranges.Add(_routeId, range1);
			ranges.Add(_routeId, range2);

			var overlaps = new List<OverlappingMeasures>(ranges.GetOverlaps());

			Assert.AreEqual(0, overlaps.Count);
		}
	}
}
