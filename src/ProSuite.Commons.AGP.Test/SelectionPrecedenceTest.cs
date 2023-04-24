using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.AGP.Selection;

namespace ProSuite.Commons.AGP.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class SelectionPrecedenceTest
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void Can_use_DistanceToStartEndpointComparer()
		{
			Polyline longPolyline =
				GeometryFactory.CreatePolyline(
				MapPointBuilder.CreateMapPoint(0, 0),
				MapPointBuilder.CreateMapPoint(0, 100));

			Polyline shortPolyline =
				GeometryFactory.CreatePolyline(
					MapPointBuilder.CreateMapPoint(0, 0),
					MapPointBuilder.CreateMapPoint(0, 20));

			Polyline somePolyline =
				GeometryFactory.CreatePolyline(
					MapPointBuilder.CreateMapPoint(0, 0),
					MapPointBuilder.CreateMapPoint(-100, 42));

			MapPoint referenceGeometry = MapPointBuilder.CreateMapPoint(0, 21);

			var polylines = new List<Polyline>{somePolyline, somePolyline, longPolyline, somePolyline, shortPolyline};

			List<SelectionScore> result =
				polylines.Select(geom => new SelectionScore() { Geometry = geom })
				         .OrderBy(score => score, new DistanceToStartEndpointComparer(referenceGeometry))
				         .ToList();

			Assert.AreEqual(shortPolyline, result[0].Geometry);
			Assert.AreEqual(longPolyline, result[1].Geometry);
		}

		[Test]
		public void Can_use_SimpleSelectionScoreComparer()
		{
			Polyline longPolyline =
				GeometryFactory.CreatePolyline(
					MapPointBuilder.CreateMapPoint(0, 0),
					MapPointBuilder.CreateMapPoint(0, 100));

			Polyline shortPolyline =
				GeometryFactory.CreatePolyline(
					MapPointBuilder.CreateMapPoint(0, 0),
					MapPointBuilder.CreateMapPoint(0, 20));

			Polyline somePolyline =
				GeometryFactory.CreatePolyline(
					MapPointBuilder.CreateMapPoint(0, 0),
					MapPointBuilder.CreateMapPoint(-100, 42));

			MapPoint referenceGeometry = MapPointBuilder.CreateMapPoint(0, 21);

			var polylines = new List<Polyline> { somePolyline, somePolyline, longPolyline, somePolyline, shortPolyline };

			List<SelectionScore> result =
				Create(polylines, referenceGeometry)
					.OrderBy(score => score, new SimpleSelectionScoreComparer())
					.ToList();

			Assert.AreEqual(shortPolyline, result[0].Geometry);
			Assert.AreEqual(longPolyline, result[1].Geometry);
		}

		private static IEnumerable<SelectionScore> Create(IEnumerable<Polyline> polylines, Geometry referenceGeometry)
		{
			foreach (Polyline polyline in polylines)
			{
				double distance =
					DistanceToStartEndpointComparer.SumDistancesStartEndPoint(polyline, referenceGeometry);

				yield return new SelectionScore { Score = (int) distance, Geometry = polyline };
			}
		}
	}
}
