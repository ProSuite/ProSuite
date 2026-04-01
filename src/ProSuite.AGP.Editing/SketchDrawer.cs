using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

public class SketchDrawer
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly List<IDisposable> _overlays = new List<IDisposable>(3);

	private static readonly CIMRGBColor _sketchGreen = ColorUtils.CreateRGB(0, 125, 0);

	private CIMSymbolReference _lineSymbolRef;
	private CIMSymbolReference _polygonSymbolRef;
	private CIMSymbolReference _regularUnselectedSymbolRef;
	private CIMSymbolReference _currentUnselectedSymbolRef;

	public CIMSymbolReference LineSymbol
	{
		// NOTE: This line symbol crashes the application by grabbing all the memory
		//       -> use other symbol in stereo!
		get => _lineSymbolRef;
		set => _lineSymbolRef = value;
	}

	public CIMSymbolReference LineSymbolStereo
	{
		get => _lineSymbolRef;
		set => _lineSymbolRef = value;
	}

	public CIMSymbolReference PolygonSymbol
	{
		get => _polygonSymbolRef;
		set => _polygonSymbolRef = value;
	}

	public CIMSymbolReference PolygonSymbolStereo
	{
		get => _polygonSymbolRef;
		set => _polygonSymbolRef = value;
	}

	public CIMSymbolReference RegularUnselectedSymbol
	{
		get => _regularUnselectedSymbolRef;
		set => _regularUnselectedSymbolRef = value;
	}

	public CIMSymbolReference RegularUnselectedSymbolStereo
	{
		get => _regularUnselectedSymbolRef;
		set => _regularUnselectedSymbolRef = value;
	}

	public CIMSymbolReference CurrentUnselectedSymbol
	{
		get => _currentUnselectedSymbolRef;
		set => _currentUnselectedSymbolRef = value;
	}

	public CIMSymbolReference CurrentUnselectedSymbolStereo
	{
		get => _currentUnselectedSymbolRef;
		set => _currentUnselectedSymbolRef = value;
	}

	public async Task ShowSketch([CanBeNull] Geometry sketchGeometry,
	                             [NotNull] MapView inMapView)
	{
		if (sketchGeometry == null || sketchGeometry.IsEmpty)
		{
			ClearSketch();
			return;
		}

		bool isStereo = MapUtils.IsStereoMapView(inMapView);

		if (! SymbolReferencesInitialized())
		{
			// Typically this is called from SketchModified (which is called from a CIM thread anyway)
			// But just to be sure, use QueuedTask:
			await QueuedTask.Run(() => EnsureSymbolReferences(isStereo));
		}

		// NOTE: When the sketch is NOT drawn if there are NaNs, the memory-crash () does not happen.
		// TODO: Test thoroughly
		//// the latch and only draw non-NaN points. Or compare with previous sketch?
		//// Also, consider comparing with previously drawn sketch -> only draw if non-equal
		//// Also, update the existing sketch instead of replacing it.

		if (sketchGeometry is Multipart multiPart && multiPart.Points.Any(p => double.IsNaN(p.Z)))
		{
			return;
		}

		// Consider updating existing geometries instead clearing
		ClearSketch();

		if (sketchGeometry is Multipoint multipointSketch)
		{
			var builder = new MultipointBuilderEx(inMapView.Map.SpatialReference);

			foreach (MapPoint point in multipointSketch.Points)
			{
				builder.AddPoint(point);
			}

			// NOTE: The multipoints are not shown in stereo! TODO: repro & report
			var multipoint = builder.ToGeometry();
			_overlays.Add(
				await inMapView.AddOverlayAsync(multipoint, _regularUnselectedSymbolRef));
		}

		if (sketchGeometry is Polyline polyline)
		{
			_overlays.Add(await inMapView.AddOverlayAsync(polyline, _lineSymbolRef));

			var endPoint = GeometryUtils.GetEndPoint(polyline);
			if (endPoint != null)
			{
				_overlays.Add(
					await inMapView.AddOverlayAsync(endPoint, _currentUnselectedSymbolRef));
			}
		}
		else if (sketchGeometry is Polygon polygon)
		{
			_overlays.Add(inMapView.AddOverlay(polygon, _polygonSymbolRef));

			// start and end point of a polygon are geometrically equal
			var points = polygon.Points;
			int count = points.Count;
			if (count == 0) return;

			MapPoint endPoint = count > 2 ? points[count - 2] : points[0];

			if (endPoint != null)
			{
				_overlays.Add(
					await inMapView.AddOverlayAsync(endPoint, _currentUnselectedSymbolRef));
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

	private bool SymbolReferencesInitialized()
	{
		return _lineSymbolRef != null &&
		       _polygonSymbolRef != null &&
		       _regularUnselectedSymbolRef != null &&
		       _currentUnselectedSymbolRef != null;
	}

	private void EnsureSymbolReferences(bool isStereo)
	{
		_lineSymbolRef ??= isStereo
			                   ? CreateStereoLineSymbol().MakeSymbolReference()
			                   : CreateLineSymbol().MakeSymbolReference();

		_polygonSymbolRef ??= isStereo
			                      ? CreateStereoPolygonSymbol().MakeSymbolReference()
			                      : CreatePolygonSymbol().MakeSymbolReference();

		_regularUnselectedSymbolRef ??= isStereo
			                                ? CreateStereoRegularUnselectedSymbol()
				                                .MakeSymbolReference()
			                                : CreateRegularUnselectedSymbol().MakeSymbolReference();

		_currentUnselectedSymbolRef ??= isStereo
			                                ? CreateStereoCurrentUnselectedSymbol()
				                                .MakeSymbolReference()
			                                : CreateCurrentUnselectedSymbol().MakeSymbolReference();
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

		//result.HaloSize = 1;

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
