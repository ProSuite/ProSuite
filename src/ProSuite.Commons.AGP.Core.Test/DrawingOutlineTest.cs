using System;
using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Hosting;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class DrawingOutlineTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void CanGetSimplePointMarkerOutline()
	{
		var sref = SpatialReferences.WGS84;
		var shape = GeometryFactory.CreatePoint(8.5, 47.5, sref);
		var symbol = SymbolUtils.CreatePointSymbol(ColorUtils.RedRGB, 10.0);
		var options = DrawingOutline.GetDefaultOptions(1.0);

		var outline = DrawingOutline.GetOutline(shape, symbol, options);

		Assert.NotNull(outline);
		// outline must have shape's sref:
		Assert.AreEqual(sref, outline.SpatialReference);
		Assert.AreEqual(GeometryType.Polygon, outline.GeometryType);
		Assert.False(outline.IsEmpty);
		Assert.AreEqual(shape.X, outline.Extent.Center.X, 0.01);
		Assert.AreEqual(shape.Y, outline.Extent.Center.Y, 0.01);
	}

	[Test]
	public void CanGetSimpleLineStrokeOutline()
	{
		var shape = CreatePolyline(0.0, 0.0, 10.0, 10.0);
		var symbol = SymbolUtils.CreateLineSymbol(ColorUtils.RedRGB, 4.0);
		var options = DrawingOutline.GetDefaultOptions(1.0);
		options.MaxBufferDeviationPoints = 0.001;

		var outline = DrawingOutline.GetOutline(shape, symbol, options);

		Assert.NotNull(outline);
		Assert.AreEqual(GeometryType.Polygon, outline.GeometryType);
		Assert.False(outline.IsEmpty);
		var area = 4.0 * 10.0 * Math.Sqrt(2) + 2.0 * 2.0 * Math.PI;
		Assert.AreEqual(area, ((Polygon) outline).Area, 0.1);
	}

	[Test]
	public void CanGetSimplePolygonFillOutline()
	{
		var env = GeometryFactory.CreateEnvelope(0, 0, 10, 10);
		var shape = GeometryFactory.CreatePolygon(env);
		var symbol = SymbolUtils.CreatePolygonSymbol(); // solid fill
		var options = DrawingOutline.GetDefaultOptions(1.0);

		var outline = DrawingOutline.GetOutline(shape, symbol, options);

		Assert.NotNull(outline);
		Assert.AreEqual(GeometryType.Polygon, outline.GeometryType);
		Assert.False(outline.IsEmpty);
		Assert.AreEqual(env.Area, ((Polygon) outline).Area, 0.001);
	}

	[Test]
	public void CanGetPolygonFillStrokeOutline()
	{
		var env = GeometryFactory.CreateEnvelope(0, 0, 10, 10);
		var shape = GeometryFactory.CreatePolygon(env);

		// filled with a fat solid outline, line-join is round
		var symbol = SymbolUtils.CreatePolygonSymbol(
			SymbolUtils.CreateSolidFill(),
			SymbolUtils.CreateSolidStroke(ColorUtils.RedRGB, 4.0));

		var options = DrawingOutline.GetDefaultOptions(1.0);
		options.MaxBufferDeviationPoints = 0.01;

		var outline = DrawingOutline.GetOutline(shape, symbol, options);

		Assert.NotNull(outline);
		Assert.AreEqual(GeometryType.Polygon, outline.GeometryType);
		Assert.False(outline.IsEmpty);
		const double area = (2 + 10 + 2) * (2 + 10 + 2) -      // rect plus stroke
		                    (4.0 * 4.0 - 2.0 * 2.0 * Math.PI); // minus round corners
		Assert.AreEqual(area, ((Polygon)outline).Area, 0.1);
	}

	// TODO many more tests

	#region Private test utils

	private static Polyline CreatePolyline(params double[] coords)
	{
		var builder = new PolylineBuilderEx();

		for (int i = 2; i < coords.Length; i += 2)
		{
			var x0 = coords[i - 1];
			var y0 = coords[i - 1];
			var x1 = coords[i];
			var y1 = coords[i + 1];

			var v0 = new Coordinate2D(x0, y0);
			var v1 = new Coordinate2D(x1, y1);

			builder.AddSegment(LineBuilderEx.CreateLineSegment(v0, v1));
		}

		return builder.ToGeometry();
	}

	#endregion
}
