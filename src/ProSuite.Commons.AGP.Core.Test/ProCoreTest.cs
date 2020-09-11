using System;
using System.Threading;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;

namespace ProSuite.Commons.AGP.Test
{
	/// <summary>
	/// Testing assumptions about ArcGIS.Core from the ArcGIS Pro SDK/API.
	/// Testing ArcGIS.Core related utilities (TODO split into separate classes)
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ProCoreTest
	{
		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			Hosting.CoreHostProxy.Initialize();
		}

		[Test]
		public void CanSpatialReferenceProperties()
		{
			var wgs84 = SpatialReferences.WGS84;
			Assert.IsTrue(wgs84.IsGeographic);
			Assert.IsFalse(wgs84.IsProjected);
			Assert.IsFalse(wgs84.IsUnknown);
		}

		[Test]
		public void CanCreateBezierCircle()
		{
			const double radius = 5.0;
			var center = MapPointBuilder.CreateMapPoint(1, 2);
			const double delta = 0.000001;

			var circle = GeometryUtils.CreateBezierCircle(radius, center);

			Assert.AreEqual(1, circle.PartCount);
			Assert.AreEqual(5, circle.PointCount);
			Assert.AreEqual(GeometryType.Polygon, circle.GeometryType);
			Assert.AreEqual(10.0, circle.Extent.Width, delta);
			Assert.AreEqual(10.0, circle.Extent.Height, delta);
			Assert.AreEqual(center.X, circle.Extent.Center.X, delta);
			Assert.AreEqual(center.Y, circle.Extent.Center.Y, delta);

			const double circumference = 2.0 * radius * Math.PI;
			Assert.AreEqual(circumference, circle.Length, 0.01);
		}

		[Test]
		public void CanCreateSimpleStrokeSymbol()
		{
			// Create a simple line symbol: black stroke

			var symbol = SymbolUtils.CreateLineSymbol(ColorUtils.BlackRGB, 1.0);
			var xml = symbol.ToXml(); // may copy-paste into CIM Viewer (fix root elem name!)
			Assert.IsTrue(xml.Length > 0);
		}

		[Test]
		public void CanCreateNonTrivialLineSymbol()
		{
			// Create line symbol: black stroke, with alternating
			// black circles and blue squares along the line

			CIMColor black = ColorUtils.CreateRGB(0, 0, 0);
			CIMColor blue = ColorUtils.CreateRGB(0, 0, 255);

			var circleMarker = SymbolUtils.CreateMarker(black, 5, SymbolUtils.MarkerStyle.Circle)
			                              .SetMarkerPlacementAlongLine(60);
			var squareMarker = SymbolUtils.CreateMarker(blue, 5, SymbolUtils.MarkerStyle.Square)
			                              .SetMarkerPlacementAlongLine(60);

			var dashPattern = SymbolUtils.CreateDashPattern(20, 10, 20, 10);
			var blackStroke = SymbolUtils.CreateSolidStroke(black, 1)
			                             .AddDashes(dashPattern, LineDashEnding.HalfPattern)
			                             .AddOffset(0.0);

			var symbol = new CIMLineSymbol
			             {
				             SymbolLayers = new[] {blackStroke, circleMarker, squareMarker},
				             Effects = null
			             };

			var xml = symbol.ToXml(); // may copy-paste into CIM Viewer (fix root elem name!)
			Assert.IsTrue(xml.Length > 0);
		}
	}
}
