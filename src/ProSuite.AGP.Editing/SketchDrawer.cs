using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing;

public class SketchDrawer
{
	[CanBeNull] private List<IDisposable> _overlays;

	private CIMLineSymbol _lineSymbol;
	private CIMPolygonSymbol _polygonSymbol;
	private CIMPointSymbol _regularUnselectedSymbol;
	private CIMPointSymbol _currentUnselectedSymbol;

	public CIMLineSymbol LineSymbol
	{
		get => _lineSymbol ??= CreateLineSymbol();
		set => _lineSymbol = value;
	}

	public CIMPolygonSymbol PolygonSymbol
	{
		get => _polygonSymbol ??= CreatePolygonSymbol();
		set => _polygonSymbol = value;
	}

	public CIMPointSymbol RegularUnselectedSymbol
	{
		get => _regularUnselectedSymbol ??= CreateRegularUnselectedSymbol();
		set => _regularUnselectedSymbol = value;
	}

	public CIMPointSymbol CurrentUnselectedSymbol
	{
		get => _currentUnselectedSymbol ??= CreateCurrentUnselectedSymbol();
		set => _currentUnselectedSymbol = value;
	}

	public async Task ShowSketch([CanBeNull] Geometry sketchGeometry,
	                             [NotNull] MapView inMapView)
	{
		if (sketchGeometry == null || sketchGeometry.IsEmpty)
		{
			ClearSketch();
			return;
		}

		// add two extra for start (or end) point and sketch
		int capacity = GeometryUtils.GetPointCount(sketchGeometry) + 2;
		_overlays = new List<IDisposable>(capacity);

		CIMSymbolReference regularUnselectedSymbRef =
			RegularUnselectedSymbol.MakeSymbolReference();
		CIMSymbolReference currentUnselectedSymbRef =
			CurrentUnselectedSymbol.MakeSymbolReference();

		if (sketchGeometry is Multipart multipart)
		{
			var builder = new MultipointBuilderEx(inMapView.Map.SpatialReference);

			foreach (MapPoint point in multipart.Points)
			{
				builder.AddPoint(point);
			}

			var multipoint = builder.ToGeometry();
			_overlays.Add(
				await inMapView.AddOverlayAsync(multipoint, regularUnselectedSymbRef));
		}

		if (sketchGeometry is Polyline polyline)
		{
			CIMSymbolReference lineSymbRef = LineSymbol.MakeSymbolReference();
			_overlays.Add(await inMapView.AddOverlayAsync(polyline, lineSymbRef));

			var endPoint = GeometryUtils.GetEndPoint(polyline);
			if (endPoint != null)
			{
				_overlays.Add(
					await inMapView.AddOverlayAsync(endPoint, currentUnselectedSymbRef));
			}
		}
		else if (sketchGeometry is Polygon polygon)
		{
			CIMSymbolReference polySymbRef = PolygonSymbol.MakeSymbolReference();
			_overlays.Add(await inMapView.AddOverlayAsync(polygon, polySymbRef));

			// start and end point of a polygon are geometrically equal
			var points = polygon.Points;
			int count = points.Count;
			if (count == 0) return;

			MapPoint endPoint = count > 2 ? points[count - 2] : points[0];

			if (endPoint != null)
			{
				_overlays.Add(
					await inMapView.AddOverlayAsync(endPoint, currentUnselectedSymbRef));
			}
		}
	}

	public void ClearSketch()
	{
		if (_overlays == null) return;

		foreach (IDisposable overlay in _overlays)
		{
			overlay.Dispose();
		}

		_overlays.Clear();
	}

	private static CIMLineSymbol CreateLineSymbol()
	{
		return SymbolUtils.CreateLineSymbol(CreateSketchStrokes());
	}

	private static CIMPolygonSymbol CreatePolygonSymbol()
	{
		return SymbolUtils.CreatePolygonSymbol(CreateSketchStrokes());
	}

	private static CIMPointSymbol CreateCurrentUnselectedSymbol()
	{
		return CreateSketchVertexSymbol(ColorUtils.CreateRGB(255, 0, 0));
	}

	private static CIMPointSymbol CreateRegularUnselectedSymbol()
	{
		return CreateSketchVertexSymbol(ColorUtils.CreateRGB(0, 128, 0));
	}

	private static CIMSymbolLayer[] CreateSketchStrokes()
	{
		double width = 0.5;
		var dashPattern = SymbolUtils.CreateDashPattern(3, 3, 3, 3);
		var whiteStroke = SymbolUtils.CreateSolidStroke(ColorUtils.WhiteRGB, width);
		var blackStroke = SymbolUtils.CreateSolidStroke(ColorUtils.BlackRGB, width)
		                             .AddDashes(dashPattern, LineDashEnding.HalfPattern);
		return new[] { blackStroke, whiteStroke };
	}

	private static CIMPointSymbol CreateSketchVertexSymbol(CIMRGBColor color)
	{
		var outline = SymbolUtils.CreateSolidStroke(color, 1.5);
		CIMPolygonSymbol polygonSymbol = SymbolUtils.CreatePolygonSymbol(outline);
		Geometry geometry = SymbolUtils.CreateMarkerGeometry(SymbolUtils.MarkerStyle.Square);
		CIMVectorMarker marker = SymbolUtils.CreateMarker(geometry, polygonSymbol, 5);
		return SymbolUtils.CreatePointSymbol(marker);
	}
}
