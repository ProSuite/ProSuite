using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using System;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Carto;

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
		// single and multi part; offset along line; 
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
		throw new NotImplementedException();
	}

	[Test]
	public void CanAlongLine()
	{
		throw new NotImplementedException();
	}

	[Test]
	public void CanPolygonCenter()
	{
		// PlacePerPart
		throw new NotImplementedException();
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

	#endregion
}
