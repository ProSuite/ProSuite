using System;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class MarkerPlacementsTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void CanAtExtremities()
	{
		// single and multipart; offset along line; 
		throw new NotImplementedException();
	}

	[Test]
	public void CanOnVertices()
	{
		// PlaceOnEndPoints vs PlacePerPart
		throw new NotImplementedException();
	}

	[Test]
	public void CanOnLine()
	{
		const double delta = 0.01;
		double sin45 = 1.0 / Math.Sqrt(2);
		double cos45 = 1.0 / Math.Sqrt(2);

		var options = new MarkerPlacements.OnLineOptions();

		var marker = PolylineBuilderEx.CreatePolyline(
			new[] { Pt(0, 0), Pt(0, 1) });

		var line = PolylineBuilderEx.CreatePolyline(
			new[] { Pt(0, 0), Pt(2, 2), Pt(6, 2), Pt(8, 0) });

		options.PlacePerPart = true;
		options.AngleToLine = true;
		options.PerpendicularOffset = 1.0;
		options.StartPointOffset = 0.0;

		// at start, angle-to-line, perp offs 1 unit
		options.RelativeTo = MarkerPlacements.OnLinePosition.Start;
		var placed = MarkerPlacements.OnLine(marker, line, options).ToArray();
		Assert.AreEqual(1, placed.Length);
		Assert.AreEqual(2, placed[0].PointCount);
		AssertPoint(placed[0].Points[0], -cos45, sin45, delta);
		AssertPoint(placed[0].Points[1], -cos45 * 2, sin45 * 2, delta);

		// at end, angle-to-line, perp offs 1 unit
		options.RelativeTo = MarkerPlacements.OnLinePosition.End;
		placed = MarkerPlacements.OnLine(marker, line, options).ToArray();
		Assert.AreEqual(1, placed.Length);
		Assert.AreEqual(2, placed[0].PointCount);
		AssertPoint(placed[0].Points[0], 8.0 + cos45, 0.0 + sin45, delta);
		AssertPoint(placed[0].Points[1], 8.0 + cos45 * 2, 0.0 + sin45 * 2, delta);

		// at end, angle-to-line, perp offs 1 unit
		options.RelativeTo = MarkerPlacements.OnLinePosition.Middle;
		placed = MarkerPlacements.OnLine(marker, line, options).ToArray();
		Assert.AreEqual(1, placed.Length);
		Assert.AreEqual(2, placed[0].PointCount);
		AssertPoint(placed[0].Points[0], 4.0, 2.0 + 1.0, delta);
		AssertPoint(placed[0].Points[1], 4.0, 2.0 + 2.0, delta);

		// each segment midpoint, angle-to-line, no perp offs
		options.RelativeTo = MarkerPlacements.OnLinePosition.SegmentMidpoints;
		options.PerpendicularOffset = 0.0;
		placed = MarkerPlacements.OnLine(marker, line, options).ToArray();
		Assert.AreEqual(3, placed.Length);
		Assert.AreEqual(2, placed[2].PointCount);
		AssertPoint(placed[0].Points[0], 1.0, 1.0, delta);
		AssertPoint(placed[1].Points[0], 4.0, 2.0, delta);
		AssertPoint(placed[2].Points[0], 7.0, 1.0, delta);

		// must work with lines and polygons (using the boundary)
		var poly = PolygonBuilderEx.CreatePolygon(
			new[] { Pt(0, 0), Pt(4, 4), Pt(4, 0), Pt(0, 0) });

		options.RelativeTo = MarkerPlacements.OnLinePosition.Middle;
		options.PerpendicularOffset = 0.0;
		options.AngleToLine = false;
		placed = MarkerPlacements.OnLine(marker, poly, options).ToArray();
		Assert.AreEqual(1, placed.Length);
		Assert.AreEqual(2, placed[0].PointCount);
		AssertPoint(placed[0].Points[0], 4.0, 2 * Math.Sqrt(2), delta);
		AssertPoint(placed[0].Points[1], 4.0, 2 * Math.Sqrt(2) + 1.0, delta);
	}

	private static void AssertPoint(MapPoint point, double x, double y, double delta)
	{
		Assert.NotNull(point);
		Assert.AreEqual(x, point.X, delta);
		Assert.AreEqual(y, point.Y, delta);
	}

	[Test]
	public void CanAlongLine()
	{
		var marker = PolylineBuilderEx.CreatePolyline(
			new[] { Pt(0, 0), Pt(0, 1) });

		var options = new MarkerPlacements.AlongLineOptions
		              {
			              AngleToLine = true,
			              PlacePerPart = true,
			              PerpendicularOffset = 0,
			              Pattern = new[] {10.0},
			              OffsetAlongLine = 0,
			              CustomEndingOffset = 0
		              };

		// Test that at least one point is always added (and that the calculation finishes)

		var smallLine = PolylineBuilderEx.CreatePolyline(
			new[] { Pt(0, 0), Pt(5, 0) });
		var endings = Enum.GetValues(typeof(MarkerPlacements.EndingsType))
		                  .Cast<MarkerPlacements.EndingsType>();
		foreach (var ending in endings)
		{
			options.Endings = ending;

			var placements = MarkerPlacements.AlongLine(marker, smallLine, options).ToArray();
			Assert.AreEqual(1, placements.Length);
			Assert.AreEqual(2, placements[0].PointCount);
		}

		// Test a line where multiple markers can be placed

		var line = PolylineBuilderEx.CreatePolyline(
			new[] { Pt(0, 0), Pt(100, 0) });
		MarkerPlacements.EndingsType[] fullEndings =
		{
			MarkerPlacements.EndingsType.Unconstrained,
			MarkerPlacements.EndingsType.Marker,
			MarkerPlacements.EndingsType.Custom
		};

		foreach (var ending in fullEndings)
		{
			options.Endings = ending;

			var placements = MarkerPlacements.AlongLine(marker, line, options).ToArray();
			Assert.AreEqual(11, placements.Length);
			Assert.AreEqual(2, placements[0].PointCount);
		}

		options.Endings = MarkerPlacements.EndingsType.HalfStep;
		var placementsHalfStep = MarkerPlacements.AlongLine(marker, line, options).ToArray();
		Assert.AreEqual(10, placementsHalfStep.Length);
		Assert.AreEqual(2, placementsHalfStep[0].PointCount);

		options.Endings = MarkerPlacements.EndingsType.FullStep;
		var placementsFullStep = MarkerPlacements.AlongLine(marker, line, options).ToArray();
		Assert.AreEqual(9, placementsFullStep.Length);
		Assert.AreEqual(2, placementsFullStep[0].PointCount);
	}

	[Test]
	public void CanPolygonCenter()
	{
		const double delta = 0.000001;
		var options = new MarkerPlacements.PolygonCenterOptions();

		// ArcGIS setting | CenterType
		// ---------------+------------------
		// On polygon     | LabelPoint
		// Center of mass | Centroid
		// Bbox center    | BoundingBoxCenter
		//
		// Test each setting, once per polygon, once per part, on this polygon:
		//
		// 30 ############
		// 25 ####
		// 30 ####  ######
		//    ####  ######
		// 10 ####
		//  5 ############
		//  0    10 15   30

		var builder = new PolygonBuilderEx();
		builder.AddPart(new[]
		                {
			                Pt(0, 0), Pt(0, 30), Pt(30, 30), Pt(30, 25), Pt(10, 25),
			                Pt(10, 5), Pt(30, 5), Pt(30, 0), Pt(0, 0)
		                });
		builder.AddPart(new[] { Pt(15, 10), Pt(15, 20), Pt(30, 20), Pt(30, 10) });
		var polygon = builder.ToGeometry();

		var marker = Pt(0, 0);

		// On polygon:

		options.CenterType = MarkerPlacements.PolygonCenterType.LabelPoint;
		options.PlacePerPart = false;
		var placed = MarkerPlacements.PolygonCenter(marker, polygon, options).ToArray();
		Assert.AreEqual(1, placed.Length);
		// empirical: Pro's "On polygon" placement method uses the LabelPoint operation:
		var lp = GetLabelPoint(polygon);
		Assert.AreEqual(lp.X, placed.Single().X, delta);
		Assert.AreEqual(lp.Y, placed.Single().Y, delta);

		options.CenterType = MarkerPlacements.PolygonCenterType.LabelPoint;
		options.PlacePerPart = true;
		placed = MarkerPlacements.PolygonCenter(marker, polygon, options)
		                         .OrderBy(p => p.X).ToArray();
		Assert.AreEqual(2, placed.Length);
		var lps = GeometryUtils.ConnectedComponents(polygon)
		                       .Select(GetLabelPoint)
		                       .OrderBy(p => p.X).ToArray();
		Assert.AreEqual(lps[0].X, placed[0].X, delta);
		Assert.AreEqual(lps[0].Y, placed[0].Y, delta);
		Assert.AreEqual(lps[1].X, placed[1].X, delta);
		Assert.AreEqual(lps[1].Y, placed[1].Y, delta);

		// Center of mass:

		options.CenterType = MarkerPlacements.PolygonCenterType.Centroid;
		options.PlacePerPart = false;
		placed = MarkerPlacements.PolygonCenter(marker, polygon, options).ToArray();
		Assert.AreEqual(1, placed.Length);
		var ct = GetCentroid(polygon);
		Assert.AreEqual(ct.X, placed.Single().X, delta);
		Assert.AreEqual(ct.Y, placed.Single().Y, delta);

		options.CenterType = MarkerPlacements.PolygonCenterType.Centroid;
		options.PlacePerPart = true;
		placed = MarkerPlacements.PolygonCenter(marker, polygon, options).ToArray();
		Assert.AreEqual(2, placed.Length);
		var cts = GeometryUtils.ConnectedComponents(polygon)
		                       .Select(GetCentroid)
		                       .OrderBy(p => p.X).ToArray();
		Assert.AreEqual(cts[0].X, placed[0].X, delta);
		Assert.AreEqual(cts[0].Y, placed[0].Y, delta);
		Assert.AreEqual(cts[1].X, placed[1].X, delta);
		Assert.AreEqual(cts[1].Y, placed[1].Y, delta);

		// Bounding box center

		options.CenterType = MarkerPlacements.PolygonCenterType.BoundingBoxCenter;
		options.PlacePerPart = false;
		placed = MarkerPlacements.PolygonCenter(marker, polygon, options).ToArray();
		Assert.AreEqual(1, placed.Length);
		Assert.AreEqual(15.0, placed.Single().X, delta);
		Assert.AreEqual(15.0, placed.Single().Y, delta);

		options.CenterType = MarkerPlacements.PolygonCenterType.BoundingBoxCenter;
		options.PlacePerPart = true;
		placed = MarkerPlacements.PolygonCenter(marker, polygon, options)
		                         .OrderBy(p => p.X).ToArray();
		Assert.AreEqual(2, placed.Length);
		Assert.AreEqual(15.0, placed[0].X, delta);
		Assert.AreEqual(15.0, placed[0].Y, delta);
		Assert.AreEqual(22.5, placed[1].X, delta);
		Assert.AreEqual(15.0, placed[1].Y, delta);
	}

	[Test]
	public void CanAroundPolygon()
	{
		throw new NotImplementedException();
	}

	[Test]
	public void CanOnPoint()
	{
		// NB. This is not really a marker placement, but serves as a default

		var marker = PolylineBuilderEx.CreatePolyline(new[] { Pt(0, 0), Pt(10, 10) });
		var point = Pt(100, 100);

		Assert.IsEmpty(MarkerPlacements.OnPoint<MapPoint>(null, point));
		Assert.IsEmpty(MarkerPlacements.OnPoint(marker, null));

		var placed = MarkerPlacements.OnPoint(marker, point).ToArray();
		Assert.NotNull(placed);
		Assert.AreEqual(1, placed.Length);
		Assert.AreEqual(point.X, placed[0].Points[0].X, 0.00001);
		Assert.AreEqual(point.Y, placed[0].Points[0].Y, 0.00001);
	}

	#region Private test utils

	private static MapPoint Pt(double x, double y)
	{
		return MapPointBuilderEx.CreateMapPoint(x, y);
	}

	private static MapPoint GetLabelPoint(Polygon polygon)
	{
		if (polygon is null)
			throw new ArgumentNullException(nameof(polygon));
		return GeometryEngine.Instance.LabelPoint(polygon);
	}

	private static MapPoint GetCentroid(Geometry geometry)
	{
		if (geometry is null)
			throw new ArgumentNullException(nameof(geometry));
		return GeometryEngine.Instance.Centroid(geometry);
	}

	#endregion
}
