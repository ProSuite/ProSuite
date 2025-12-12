using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing;

public class SketchDrawer
{
	[NotNull] private readonly List<IDisposable> _overlays = new List<IDisposable>(3);

	private static readonly CIMRGBColor _sketchGreen = ColorUtils.CreateRGB(0, 125, 0);

	private CIMLineSymbol _lineSymbol;
	private CIMPolygonSymbol _polygonSymbol;
	private CIMPointSymbol _regularUnselectedSymbol;
	private CIMPointSymbol _currentUnselectedSymbol;

	public CIMLineSymbol LineSymbol
	{
		// NOTE: This line symbol crashes the application by grabbing all the memory
		//       -> use other symbol in stereo!
		get => _lineSymbol ??= CreateLineSymbol();
		set => _lineSymbol = value;
	}

	public CIMLineSymbol LineSymbolStereo
	{
		get => _lineSymbol ??= CreateStereoLineSymbol();
		set => _lineSymbol = value;
	}

	public CIMPolygonSymbol PolygonSymbol
	{
		get => _polygonSymbol ??= CreatePolygonSymbol();
		set => _polygonSymbol = value;
	}

	public CIMPolygonSymbol PolygonSymbolStereo
	{
		get => _polygonSymbol ??= CreateStereoPolygonSymbol();
		set => _polygonSymbol = value;
	}

	public CIMPointSymbol RegularUnselectedSymbol
	{
		get => _regularUnselectedSymbol ??= CreateRegularUnselectedSymbol();
		set => _regularUnselectedSymbol = value;
	}

	public CIMPointSymbol RegularUnselectedSymbolStereo
	{
		get => _regularUnselectedSymbol ??= CreateStereoRegularUnselectedSymbol();
		set => _regularUnselectedSymbol = value;
	}

	public CIMPointSymbol CurrentUnselectedSymbol
	{
		get => _currentUnselectedSymbol ??= CreateCurrentUnselectedSymbol();
		set => _currentUnselectedSymbol = value;
	}

	public CIMPointSymbol CurrentUnselectedSymbolStereo
	{
		get => _currentUnselectedSymbol ??= CreateStereoCurrentUnselectedSymbol();
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

		// Consider updating existing geometries instead clearing
		ClearSketch();

		bool isStereo = MapUtils.IsStereoMapView(inMapView);

		CIMSymbolReference regularUnselectedSymbRef =
			isStereo
				? RegularUnselectedSymbolStereo.MakeSymbolReference()
				: RegularUnselectedSymbol.MakeSymbolReference();

		CIMSymbolReference currentUnselectedSymbRef =
			isStereo
				? CurrentUnselectedSymbolStereo.MakeSymbolReference()
				: CurrentUnselectedSymbol.MakeSymbolReference();

		if (sketchGeometry is Multipart multipart)
		{
			var builder = new MultipointBuilderEx(inMapView.Map.SpatialReference);

			foreach (MapPoint point in multipart.Points)
			{
				builder.AddPoint(point);
			}

			// NOTE: The multipoints are not shown in stereo! TODO: repro & report
			var multipoint = builder.ToGeometry();
			_overlays.Add(
				await inMapView.AddOverlayAsync(multipoint, regularUnselectedSymbRef));
		}

		if (sketchGeometry is Polyline polyline)
		{
			CIMSymbolReference lineSymbRef =
				isStereo
					? LineSymbolStereo.MakeSymbolReference()
					: LineSymbol.MakeSymbolReference();

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

	private static CIMLineSymbol CreateStereoLineSymbol()
	{
		return SymbolFactory.Instance.ConstructLineSymbol(_sketchGreen, 1.6);
	}

	private static CIMPolygonSymbol CreatePolygonSymbol()
	{
		return SymbolUtils.CreatePolygonSymbol(CreateSketchStrokes());
	}

	private static CIMPolygonSymbol CreateStereoPolygonSymbol()
	{
		var solidStroke = new CIMSolidStroke
		                  {
			                  Color = _sketchGreen,
			                  Width = 1.6
		                  };

		return SymbolFactory.Instance.ConstructPolygonSymbol(
			_sketchGreen, SimpleFillStyle.Null, solidStroke);
	}

	private static CIMPointSymbol CreateCurrentUnselectedSymbol()
	{
		return CreateSketchVertexSymbol(ColorUtils.CreateRGB(255, 0, 0));
	}

	private static CIMPointSymbol CreateStereoCurrentUnselectedSymbol()
	{
		return SymbolFactory.Instance.ConstructPointSymbol(
			ColorUtils.RedRGB, 4, SimpleMarkerStyle.Square);
	}

	private static CIMPointSymbol CreateRegularUnselectedSymbol()
	{
		return CreateSketchVertexSymbol(ColorUtils.CreateRGB(0, 128, 0));
	}

	private static CIMPointSymbol CreateStereoRegularUnselectedSymbol()
	{
		var result = SymbolFactory.Instance.ConstructPointSymbol(
			ColorUtils.GreenRGB, 4, SimpleMarkerStyle.Square);

		result.HaloSize = 1;

		return result;
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
