using System.Linq;
using System.Threading;
using ArcGIS.Core.CIM;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class SymbolUtilsTest
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void CanCreateSimpleStrokeSymbol()
		{
			// Create a simple line symbol: black stroke

			var symbol = SymbolUtils.CreateLineSymbol(ColorUtils.BlackRGB, 1.0);
			var json = symbol.ToJson(); // may copy-paste into CIM Viewer (fix root elem name!)
			Assert.IsTrue(json.Length > 0);
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

			var json = symbol.ToJson(); // may copy-paste into CIM Viewer (fix root elem name!)
			Assert.IsTrue(json.Length > 0);
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
			                        .LabelLayer(out string strokeLabel);

			var symbol = SymbolUtils.CreateLineSymbol(stroke);

			var reference = symbol.CreateReference()
			                      .AddMapping(strokeLabel, "Width", "[WIDTH]")
			                      .AddMapping(strokeLabel, "Color", "$feature.COLOR");

			var json = reference.ToJson(); // may copy-paste into CIM Viewer (fix root elem name!)
			Assert.IsTrue(json.Length > 0);
		}

		[Test]
		public void CanFindPrimitives()
		{
			var black = ColorUtils.BlackRGB;
			var pointSymbol = SymbolUtils.CreatePointSymbol(black, 1.5);

			var layer = SymbolUtils.FindPrimitiveByPath<CIMSymbolLayer>(pointSymbol, "layer 0");
			Assert.NotNull(layer);

			layer.LabelLayer(out string markerGuid);
			var layer2 = SymbolUtils.FindPrimitiveByName<CIMObject>(pointSymbol, markerGuid, out var path);
			Assert.NotNull(layer2);
			Assert.AreSame(layer, layer2);
			Assert.AreEqual("layer 0", path);

			// Again, but on a more complicated symbol

			var circleMarker = SymbolUtils.CreateMarker(black, 5, SymbolUtils.MarkerStyle.Circle)
			                              .SetMarkerPlacementAlongLine(60);
			var squareMarker = SymbolUtils.CreateMarker(black, 5, SymbolUtils.MarkerStyle.Square)
			                              .SetMarkerPlacementAtExtremities(ExtremityPlacement.Both);
			var blackStroke = SymbolUtils.CreateSolidStroke(black, 2)
			                             .AddDashes(SymbolUtils.CreateDashPattern(20, 10));
			var symbol = SymbolUtils.CreateLineSymbol(squareMarker, circleMarker, blackStroke)
			                        .AddGlobalEffect(SymbolUtils.CreateEffectOffset(10));

			var globalEffect = SymbolUtils.FindPrimitiveByPath<CIMGeometricEffect>(symbol, "effect 0");
			globalEffect.LabelEffect(out string offsetLabel);
			var localEffect =
				SymbolUtils.FindPrimitiveByPath<CIMGeometricEffect>(symbol, "layer 0 effect 0");
			localEffect.LabelEffect(out string dashesLabel);
			var circleLayer = SymbolUtils.FindPrimitiveByPath<CIMSymbolLayer>(symbol, "layer 2");
			circleLayer.LabelLayer(out string circleLabel);
			var markerPlacement =
				SymbolUtils.FindPrimitiveByPath<CIMMarkerPlacement>(symbol, "layer 2 placement");
			markerPlacement.LabelPlacement(out string placementLabel);
			//var circleGraphic = SymbolUtils.FindPrimitive<CIMMarkerGraphic>(symbol, "layer 2 graphic 0 layer 0");
			//circleGraphic.LabelGraphic(out Guid graphicLabel);

			var effect = SymbolUtils.FindPrimitiveByName<CIMGeometricEffect>(symbol, dashesLabel, out path);
			Assert.NotNull(effect);
			Assert.AreEqual("layer 0 effect 0", path);

			var placement = SymbolUtils.FindPrimitiveByName<CIMMarkerPlacement>(symbol, placementLabel, out path);
			Assert.NotNull(placement);
			Assert.AreEqual("layer 2 placement", path);

			var reference = symbol.CreateReference();
			reference.AddMapping(offsetLabel, "Offset", "[OFFSET]");
			reference.AddMapping(dashesLabel, "DashTemplate", "[DASHPATTERN]");
			reference.AddMapping(circleLabel, "Size", "[SIZE]");
			reference.AddMapping(placementLabel, "AngleToLine", "[ALIGN]");
			//reference.AddMapping(graphicLabel, "Color", "[COLOR]")

			var json = reference.ToJson();
			Assert.True(json.Length > 0);
		}

		[Test]
		public void CanBlend()
		{
			CIMSymbol nullSymbol = null;
			Assert.IsNull(nullSymbol.Blend(null, 0.2f)); // no-op on null symbol
			Assert.IsNull(nullSymbol.Blend(ColorUtils.WhiteRGB, 0.2f));

			var pointSymbol = SymbolUtils.CreatePointSymbol(ColorUtils.BlueRGB);
			Assert.AreSame(pointSymbol, pointSymbol.Blend(ColorUtils.WhiteRGB, 0.2f));
			var graphicSymbol = pointSymbol.SymbolLayers.OfType<CIMVectorMarker>().Single()
			                            .MarkerGraphics.Single().Symbol;
			var pointColor = ((CIMMultiLayerSymbol) graphicSymbol).SymbolLayers
				.OfType<CIMSolidFill>().Single().Color;
			Assert.AreEqual(51.0, pointColor.Values[0]); // red
			Assert.AreEqual(51.0, pointColor.Values[1]); // green
			Assert.AreEqual(255.0, pointColor.Values[2]); // blue
		}

		[Test]
		public void CanSetAlpha()
		{
			CIMSymbol nullSymbol = null;
			Assert.IsNull(nullSymbol.SetAlpha(50f));

			CIMSymbol pointSymbol = SymbolUtils.CreatePointSymbol(ColorUtils.BlueRGB);
			Assert.AreSame(pointSymbol, pointSymbol.SetAlpha(50f));

			var symref = pointSymbol.CreateReference();
			Assert.AreSame(symref, symref.SetAlpha(67f));
		}

		[Test]
		public void CanGetLineWidth()
		{
			var symbol1 = SymbolUtils.CreateLineSymbol(ColorUtils.RedRGB, 2.0);
			Assert.True(SymbolUtils.GetLineWidth(symbol1, out var left1, out var right1));
			Assert.AreEqual(2.0, left1 + right1);

			var symbol2 = SymbolUtils.CreateLineSymbol(ColorUtils.RedRGB, 2.0);
			symbol2.AddGlobalEffect(SymbolUtils.CreateEffectOffset(0.5)); // shift left
			Assert.True(SymbolUtils.GetLineWidth(symbol2, out var left2, out var right2));
			Assert.AreEqual(1.5, left2);
			Assert.AreEqual(0.5, right2);

			var symbol3 = SymbolUtils.CreateLineSymbol(ColorUtils.RedRGB, 2.0);
			symbol3.AddGlobalEffect(SymbolUtils.CreateEffectOffset(-1.5)); // shift right
			Assert.True(SymbolUtils.GetLineWidth(symbol3, out var left3, out var right3));
			Assert.AreEqual(-0.5, left3);
			Assert.AreEqual(2.5, right3);

			// TODO More tests: local effects and multiple stroke layers!
		}
	}
}
