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

			var symbol = SymbolUtils.CreateLineSymbol(blackStroke, circleMarker, squareMarker);

			var xml = symbol.ToXml(); // may copy-paste into CIM Viewer (fix root elem name!)
			Assert.IsTrue(xml.Length > 0);
		}

		[Test]
		public void CanCreateLineSymbolWithOverride()
		{
			// Create a solid black 1pt wide stroke symbol
			// with overrides for the color (text field COLOR)
			// and the line width (numeric field WIDTH)

			var color = ColorUtils.BlackRGB;
			const double width = 1.0;
			var stroke = SymbolUtils.CreateSolidStroke(color, width)
			                        .LabelLayer(out Guid strokeLabel);

			var symbol = SymbolUtils.CreateLineSymbol(stroke);

			var reference = symbol.CreateReference()
			                      .AddMapping(strokeLabel, "Width", "[WIDTH]")
			                      .AddMapping(strokeLabel, "Color", "$feature.COLOR");

			var xml = reference.ToXml(); // may copy-paste into CIM Viewer (fix root elem name!)
			Assert.IsTrue(xml.Length > 0);
		}

		[Test]
		public void CanFindPrimitives()
		{
			var black = ColorUtils.BlackRGB;
			var pointSymbol = SymbolUtils.CreatePointSymbol(black, 1.5);

			var layer = SymbolUtils.FindPrimitive<CIMSymbolLayer>(pointSymbol, "layer 0");
			Assert.NotNull(layer);

			layer.LabelLayer(out Guid markerGuid);
			var layer2 = SymbolUtils.FindPrimitive<CIMObject>(pointSymbol, markerGuid);
			Assert.NotNull(layer);
			Assert.AreSame(layer, layer2);

			// Again, but on a more complicated symbol

			var circleMarker = SymbolUtils.CreateMarker(black, 5, SymbolUtils.MarkerStyle.Circle)
			                              .SetMarkerPlacementAlongLine(60);
			var squareMarker = SymbolUtils.CreateMarker(black, 5, SymbolUtils.MarkerStyle.Square)
			                              .SetMarkerPlacementAtExtremities(ExtremityPlacement.Both);
			var blackStroke = SymbolUtils.CreateSolidStroke(black, 2)
			                             .AddDashes(SymbolUtils.CreateDashPattern(20, 10));
			var symbol = SymbolUtils.CreateLineSymbol(blackStroke, circleMarker, squareMarker)
			                        .AddGlobalEffect(SymbolUtils.CreateEffectOffset(10));

			var globalEffect = SymbolUtils.FindPrimitive<CIMGeometricEffect>(symbol, "effect 0");
			globalEffect.LabelEffect(out Guid offsetLabel);
			var localEffect =  SymbolUtils.FindPrimitive<CIMGeometricEffect>(symbol, "layer 0 effect 0");
			localEffect.LabelEffect(out Guid dashesLabel);
			var circleLayer = SymbolUtils.FindPrimitive<CIMSymbolLayer>(symbol, "layer 2");
			circleLayer.LabelLayer(out Guid circleLabel);
			var markerPlacement = SymbolUtils.FindPrimitive<CIMMarkerPlacement>(symbol, "layer 2 placement");
			markerPlacement.LabelPlacement(out Guid placementLabel);
			//var circleGraphic = SymbolUtils.FindPrimitive<CIMMarkerGraphic>(symbol, "layer 2 graphic 0 layer 0");
			//circleGraphic.LabelGraphic(out Guid graphicLabel);
			
			var reference = symbol.CreateReference();
			reference.AddMapping(offsetLabel, "Offset", "[OFFSET]");
			reference.AddMapping(dashesLabel, "DashTemplate", "[DASHPATTERN]");
			reference.AddMapping(circleLabel, "Size", "[SIZE]");
			reference.AddMapping(placementLabel, "AngleToLine", "[ALIGN]");
			//reference.AddMapping(graphicLabel, "Color", "[COLOR]")

			var xml = reference.ToXml();
			Assert.True(xml.Length > 0);
		}
	}
}
