using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

public class SketchDrawer
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[NotNull] private readonly List<IDisposable> _overlays = new List<IDisposable>(3);

	// The line width, in points
	private const double _lineWidth = 0.5;
	private const double _stereoLineWidth = 1;

	// The vertex squares are drawn as outlines, while ArcGIS Pro fills its sketch vertices,
	// so the drawn sketch can be told apart from the sketch that is being edited.
	private const double _vertexSize = 5;

	// The map kind and the colors the symbols were created for
	[CanBeNull] private string _symbolsKey;

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

		// Typically this is called from SketchModified, which runs on the CIM thread
		if (QueuedTask.OnWorker)
		{
			EnsureSymbolReferences(isStereo);
		}
		else
		{
			await QueuedTask.Run(() => EnsureSymbolReferences(isStereo));
		}

		// NOTE: When the sketch is NOT drawn if there are NaNs, the memory-crash () does not happen.
		// TODO: Test thoroughly
		//// the latch and only draw non-NaN points. Or compare with previous sketch?
		//// Also, consider comparing with previously drawn sketch -> only draw if non-equal
		//// Also, update the existing sketch instead of replacing it.

		if (sketchGeometry is Multipart multiPart && multiPart.Points.Any(p => double.IsNaN(p.Z)))
		{
			_msg.VerboseDebug(() => "Sketch is not drawn because it contains one or more NaN Zs");
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
			_overlays.Add(await inMapView.AddOverlayAsync(polygon, _polygonSymbolRef));

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

	/// <summary>
	/// Creates the symbols in the colors of the edit sketch that are set in the editing
	/// options of ArcGIS Pro, unless the symbols were created for the same colors and the
	/// same kind of map. The vertices are drawn as outlines. In stereo maps the line is
	/// drawn in the primary color only.
	/// </summary>
	private void EnsureSymbolReferences(bool isStereo)
	{
		EditingOptions options = ApplicationOptions.EditingOptions;

		SegmentSymbolOptions segmentOptions = options?.GetSegmentSymbolOptions();

		CIMColor primaryColor = segmentOptions?.PrimaryColor ?? ColorUtils.BlackRGB;
		CIMColor secondaryColor = segmentOptions is { HasSecondaryColor: true }
			                          ? segmentOptions.SecondaryColor
			                          : null;

		CIMColor regularVertexColor =
			GetVertexColor(options, VertexSymbolType.RegularUnselected,
			               ColorUtils.CreateRGB(0, 128, 0));
		CIMColor currentVertexColor =
			GetVertexColor(options, VertexSymbolType.CurrentUnselected,
			               ColorUtils.CreateRGB(255, 0, 0));

		string symbolsKey = string.Join(
			"|", isStereo, ColorUtils.FormatColor(primaryColor),
			ColorUtils.FormatColor(secondaryColor), ColorUtils.FormatColor(regularVertexColor),
			ColorUtils.FormatColor(currentVertexColor));

		if (symbolsKey == _symbolsKey)
		{
			return;
		}

		CIMSymbolLayer[] strokes = isStereo
			                           ? CreateStereoSketchStrokes(primaryColor)
			                           : CreateSketchStrokes(primaryColor, secondaryColor);

		_lineSymbolRef = SymbolUtils.CreateLineSymbol(strokes).MakeSymbolReference();
		_polygonSymbolRef = SymbolUtils.CreatePolygonSymbol(strokes).MakeSymbolReference();
		_regularUnselectedSymbolRef =
			CreateSketchVertexSymbol(regularVertexColor).MakeSymbolReference();
		_currentUnselectedSymbolRef =
			CreateSketchVertexSymbol(currentVertexColor).MakeSymbolReference();

		_symbolsKey = symbolsKey;
	}

	[NotNull]
	private static CIMColor GetVertexColor([CanBeNull] EditingOptions options,
	                                       VertexSymbolType vertexSymbolType,
	                                       [NotNull] CIMColor defaultColor)
	{
		return options?.GetVertexSymbolOptions(vertexSymbolType)?.Color ?? defaultColor;
	}

	/// <summary>
	/// A solid line in the primary color. A dashed line uses so much memory in stereo maps
	/// that ArcGIS Pro stops.
	/// </summary>
	private static CIMSymbolLayer[] CreateStereoSketchStrokes([NotNull] CIMColor primaryColor)
	{
		return new CIMSymbolLayer[]
		       {
			       SymbolUtils.CreateSolidStroke(primaryColor, _stereoLineWidth)
		       };
	}

	/// <summary>
	/// A line of primary colored dashes over a solid line in the secondary color, if the
	/// secondary color is used, else only the dashes.
	/// </summary>
	private static CIMSymbolLayer[] CreateSketchStrokes([NotNull] CIMColor primaryColor,
	                                                    [CanBeNull] CIMColor secondaryColor)
	{
		var dashPattern = SymbolUtils.CreateDashPattern(3, 3, 3, 3);
		var primaryStroke = SymbolUtils.CreateSolidStroke(primaryColor, _lineWidth)
		                               .AddDashes(dashPattern, LineDashEnding.HalfPattern);

		if (secondaryColor == null)
		{
			return new CIMSymbolLayer[] { primaryStroke };
		}

		var secondaryStroke = SymbolUtils.CreateSolidStroke(secondaryColor, _lineWidth);

		return new CIMSymbolLayer[] { primaryStroke, secondaryStroke };
	}

	private static CIMPointSymbol CreateSketchVertexSymbol(CIMColor color)
	{
		var outline = SymbolUtils.CreateSolidStroke(color, 1.5);
		CIMPolygonSymbol polygonSymbol = SymbolUtils.CreatePolygonSymbol(outline);
		Geometry geometry = SymbolUtils.CreateMarkerGeometry(SymbolUtils.MarkerStyle.Square);
		CIMVectorMarker marker = SymbolUtils.CreateMarker(geometry, polygonSymbol, _vertexSize);
		return SymbolUtils.CreatePointSymbol(marker);
	}
}
