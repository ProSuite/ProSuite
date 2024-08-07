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
}
