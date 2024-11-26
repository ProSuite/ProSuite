using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Geom;

namespace ProSuite.AGP.Editing.Symbolization;

/// <summary>
/// Display feedback for <see cref="RotateSymbolToolBase"/>
/// </summary>
public class RotateSymbolFeedback : EditSymbolFeedback
{
	private CIMColor _hintColor;
	private double _hintLineWidth = 1.5;

	private IDisposable _hintOverlay;

	private CIMSymbolReference _hintSymRef;

	#region Properties

	public CIMColor HintColor
	{
		get => _hintColor ??= ColorUtils.CreateRGB(244, 0, 195);
		set
		{
			_hintColor = value;
			InvalidateCachedSymbols();
		}
	}

	public double HintLineWidth
	{
		get => _hintLineWidth;
		set
		{
			if (! (value >= 0 && double.IsFinite(value)))
				throw new ArgumentOutOfRangeException(nameof(value));
			_hintLineWidth = value;
			InvalidateCachedSymbols();
		}
	}

	#endregion

	/// <summary>
	/// If centerPoint given: draw hint for additive rotation,
	/// otherwise: draw hint for absolute rotation (aka orientation)
	/// </summary>
	/// <remarks>Must call on MCT</remarks>
	public void DrawRotateHint(MapPoint startPoint, MapPoint endPoint, MapPoint centerPoint)
	{
		if (startPoint is null || endPoint is null)
		{
			_hintOverlay?.Dispose();
			_hintOverlay = null;
			return;
		}

		var mapView = MapView.Active;
		if (mapView is null) return;

		var hintSymRef = GetHintSymRef();
		var polyline = CreateRotateHintShape(startPoint, endPoint, centerPoint);

		if (polyline is null || polyline.IsEmpty)
		{
			_hintOverlay?.Dispose();
			_hintOverlay = null;
		}
		else if (_hintOverlay is null)
		{
			_hintOverlay = mapView.AddOverlay(polyline, hintSymRef);
		}
		else
		{
			UpdateOverlay(mapView, _hintOverlay, polyline, hintSymRef);
		}
	}

	public override void Clear()
	{
		_hintOverlay?.Dispose();
		_hintOverlay = null;
	}

	#region Private methods

	private static Polyline CreateRotateHintShape(
		MapPoint startPoint, MapPoint endPoint, MapPoint centerPoint)
	{
		if (startPoint is null)
			throw new ArgumentNullException(nameof(startPoint));
		if (endPoint is null)
			throw new ArgumentNullException(nameof(endPoint));

		if (centerPoint is null)
		{
			// hint shape for orientation (absolute rotation):
			return CreateOrientHintShape(startPoint, endPoint);
		}

		// hint shape for additive rotation:
		// line from cP to sP
		// arc from sP to eP at distance cP,sP from cP

		var sref = startPoint.SpatialReference;

		var line = LineBuilderEx.CreateLineSegment(centerPoint, startPoint, sref);

		var c = centerPoint.ToPair();
		var s = startPoint.ToPair();
		var e = endPoint.ToPair();
		var cs = s - c; // vector center to startPoint
		var ce = e - c; // vector center to endPoint
		var a = c + (e - c) * cs.Length / ce.Length;
		var arcPoint = MapPointBuilderEx.CreateMapPoint(a.X, a.Y, sref);

		var orientation = LeftOf(c, s, a)
			                  ? ArcOrientation.ArcCounterClockwise
			                  : ArcOrientation.ArcClockwise;

		var arc = EllipticArcBuilderEx.CreateCircularArc(
			startPoint, arcPoint, centerPoint.Coordinate2D, orientation, sref);

		var builder = new PolylineBuilderEx(sref);
		builder.AddSegment(line);
		builder.AddSegment(arc);
		return builder.ToGeometry();
	}

	private static Polyline CreateOrientHintShape(MapPoint startPoint, MapPoint endPoint)
	{
		// Shape to create:
		// - arc cw from .8[startPoint,endPoint] to rightPoint with center at startPoint
		// - line from rightPoint to startPoint
		// - line from startPoint to endPoint

		var sref = startPoint.SpatialReference;

		double dx = endPoint.X - startPoint.X;
		double dy = endPoint.Y - startPoint.Y;
		double magnitude = Math.Sqrt(dx * dx + dy * dy);

		if (magnitude < double.Epsilon) // TODO xy tolerance or resolution
		{
			return PolylineBuilderEx.CreatePolyline(startPoint.SpatialReference); // empty
		}

		const double factor = 0.85;

		var rightCoord = new Coordinate2D(startPoint.X + factor * magnitude, startPoint.Y);
		var rightPoint = MapPointBuilderEx.CreateMapPoint(rightCoord);
		var arcCoord = new Coordinate2D(startPoint.X + factor * dx, startPoint.Y + factor * dy);
		var arcPoint = MapPointBuilderEx.CreateMapPoint(arcCoord);

		var orientation = LeftOf(startPoint.ToPair(), endPoint.ToPair(), rightPoint.ToPair())
			                  ? ArcOrientation.ArcCounterClockwise
							  : ArcOrientation.ArcClockwise;

		var arc = EllipticArcBuilderEx.CreateCircularArc(
			arcPoint, rightPoint, startPoint.Coordinate2D, orientation, sref);
		var line1 = LineBuilderEx.CreateLineSegment(rightPoint, startPoint);
		var line2 = LineBuilderEx.CreateLineSegment(startPoint, endPoint);

		var builder = new PolylineBuilderEx(sref);
		builder.AddSegment(arc);
		builder.AddSegment(line1);
		builder.AddSegment(line2);
		return builder.ToGeometry();
	}

	/// <returns>true if p is left of the line a-b, otherwise false</returns>
	private static bool LeftOf(Pair a, Pair b, Pair p)
	{
		return Pair.Area2(a, b, p) > 0;
	}

	private void InvalidateCachedSymbols()
	{
		_hintSymRef = null;
	}

	/// <remarks>Must call on MCT</remarks>
	private CIMSymbolReference GetHintSymRef()
	{
		if (_hintSymRef is null)
		{
			var symbol = SymbolUtils.CreateLineSymbol(HintColor, HintLineWidth);
			_hintSymRef = symbol.MakeSymbolReference();
		}

		return _hintSymRef;
	}

	#endregion
}
