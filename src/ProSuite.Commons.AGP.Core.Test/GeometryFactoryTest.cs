using System;
using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class GeometryFactoryTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		// Helps core host apps (like unit tests) find dependencies like
		// CoreInterop.dll, freetype.dll, etc. in the proper place and version
		var installDir = ProRuntimeUtils.GetProInstallDir();
		ProRuntimeUtils.AddBinDirectoryToPath(installDir);

		CoreHostProxy.Initialize();
	}

	[Test]
	public void CanCreateBezierCircle()
	{
		const double radius = 5.0;
		var center = MapPointBuilderEx.CreateMapPoint(1, 2);
		const double delta = 0.000001;

		var circle = GeometryFactory.CreateBezierCircle(radius, center);

		Assert.AreEqual(1, circle.PartCount);
		Assert.AreEqual(5, circle.PointCount);
		Assert.AreEqual(GeometryType.Polygon, circle.GeometryType);
		Assert.AreEqual(10.0, circle.Extent.Width, delta);
		Assert.AreEqual(10.0, circle.Extent.Height, delta);
		Assert.AreEqual(center.X, circle.Extent.Center.X, delta);
		Assert.AreEqual(center.Y, circle.Extent.Center.Y, delta);

		const double circumference = 2.0 * radius * Math.PI;
		Assert.AreEqual(circumference, circle.Length, 0.01);

		const double area = Math.PI * radius * radius;
		Assert.AreEqual(area, circle.Area, 0.1);
	}

	[Test]
	public void CanCreateMultipointXY()
	{
		Multipoint multipoint = GeometryFactory.CreateMultipointXY();
		Assert.True(multipoint.IsEmpty);
		Assert.AreEqual(0, multipoint.PointCount);

		multipoint = GeometryFactory.CreateMultipointXY(1, 1, 2, 2, 3, 3);
		Assert.False(multipoint.IsEmpty);
		Assert.AreEqual(3, multipoint.PointCount);

		multipoint = GeometryFactory.CreateMultipointXY(1, 1, 2, 2, 99);
		Assert.AreEqual(2, multipoint.PointCount);
	}

	[Test]
	public void CanCreatePolylineXY()
	{
		const double delta = 1e-6;
		double sqrt2 = Math.Sqrt(2);

		Polyline polyline = GeometryFactory.CreatePolylineXY();
		Assert.True(polyline.IsEmpty);
		Assert.AreEqual(0, polyline.PartCount);
		Assert.AreEqual(0, polyline.PointCount);

		polyline = GeometryFactory.CreatePolylineXY(0, 0, 1, 0, 1, 1);
		Assert.False(polyline.IsEmpty);
		Assert.AreEqual(1, polyline.PartCount);
		Assert.AreEqual(3, polyline.PointCount);
		Assert.AreEqual(2.0, polyline.Length);

		polyline = GeometryFactory.CreatePolylineXY(0, 0, 1, 1, double.NaN, 10, 10, 11, 11);
		Assert.False(polyline.IsEmpty);
		Assert.AreEqual(2, polyline.PartCount);
		Assert.AreEqual(4, polyline.PointCount);
		Assert.AreEqual(sqrt2 + sqrt2, polyline.Length, delta);

		// Need at least 4 coords for a part (one segment),
		// NaN can occur repeatedly, odd last coord is ignored
		polyline = GeometryFactory.CreatePolylineXY(
			0, 0, 1, double.NaN, double.NaN, 10, 10, 20, 20, 99);
		Assert.False(polyline.IsEmpty);
		Assert.AreEqual(1, polyline.PartCount);
		Assert.AreEqual(2, polyline.PointCount);
		Assert.AreEqual(10*sqrt2, polyline.Length, delta);
	}

	[Test]
	public void CanCreatePolygonXY()
	{
		const double delta = 1e-6;
		double sqrt2 = Math.Sqrt(2);

		Polygon polygon = GeometryFactory.CreatePolygonXY();
		Assert.True(polygon.IsEmpty);
		Assert.AreEqual(0, polygon.PartCount);
		Assert.AreEqual(0, polygon.PointCount);

		// unit square
		polygon = GeometryFactory.CreatePolygonXY(0, 0, 0, 1, 1, 1, 1, 0, 0, 0);
		Assert.False(polygon.IsEmpty);
		Assert.AreEqual(1, polygon.PartCount);
		Assert.AreEqual(5, polygon.PointCount);
		Assert.AreEqual(1.0, polygon.Area, delta);
		Assert.AreEqual(4.0, polygon.Length, delta);

		// 3 *--------*
		// 2 |  *--*  |  *--*
		//   |  |  |  |  |  |
		// 1 |  *--*  |  *--*
		// 0 *--------+
		//   0  1  2  3  4  5
		polygon = GeometryFactory.CreatePolygonXY(
			0, 0, 0, 3, 3, 3, 3, 0, 0, 0, double.NaN,
			1, 1, 2, 1, 2, 2, 1, 2, 1, 1, double.NaN,
			4, 1, 4, 2, 5, 2, 5, 1, 4, 1);
		Assert.AreEqual(3, polygon.PartCount);
		Assert.AreEqual(15, polygon.PointCount);
		Assert.AreEqual(9.0, polygon.Area, delta);
		Assert.AreEqual(20.0, polygon.Length, delta);

		// Need at least 4 coords for a (degenerate) part,
		// NaN can occur repeatedly, odd last coord is ignored
		polygon = GeometryFactory.CreatePolygonXY(
			double.NaN, double.NaN, double.NaN,
			0, 0, 1, 1, 2, 0, 99, double.NaN, 99, 99, 99);
		Assert.AreEqual(1, polygon.PartCount);
		Assert.AreEqual(4, polygon.PointCount);
		Assert.AreEqual(sqrt2 + sqrt2 + 2.0, polygon.Length, delta);

		// Two vertices give one segment, which the builder's ToGeometry()
		// closes to a degenerate zero area, 2 seg, 3 vertex polygon
		polygon = GeometryFactory.CreatePolygonXY(0, 0, 1, 1);
		Assert.AreEqual(1, polygon.PartCount);
		Assert.AreEqual(3, polygon.PointCount);
		Assert.AreEqual(sqrt2 + sqrt2, polygon.Length);
		Assert.AreEqual(0.0, polygon.Area, delta);
	}
}
