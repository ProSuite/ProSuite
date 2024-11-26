using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;

namespace ProSuite.AGP.Editing.Symbolization;

/// <summary>
/// Display feedback for <see cref="MoveSymbolToolBase"/>
/// </summary>
public class MoveSymbolFeedback : EditSymbolFeedback
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
			if (!(value >= 0 && double.IsFinite(value)))
				throw new ArgumentOutOfRangeException(nameof(value));
			_hintLineWidth = value;
			InvalidateCachedSymbols();
		}
	}

	#endregion

	/// <remarks>Must call on MCT</remarks>
	public void DrawMoveHint(MapPoint startPoint, MapPoint endPoint)
	{
		if (startPoint is null || startPoint.IsEmpty ||
		    endPoint is null || endPoint.IsEmpty)
		{
			_hintOverlay?.Dispose();
			_hintOverlay = null;
			return;
		}

		var mapView = MapView.Active;
		if (mapView is null) return;

		var hintSymRef = GetHintSymRef();
		var polyline = CreateMoveHintShape(startPoint, endPoint);

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

	private static Polyline CreateMoveHintShape(MapPoint startPoint, MapPoint endPoint)
	{
		if (startPoint is null || startPoint.IsEmpty ||
		    endPoint is null || endPoint.IsEmpty)
		{
			return null;
		}

		return PolylineBuilderEx.CreatePolyline(
			new [] { startPoint, endPoint },
		    startPoint.SpatialReference);
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
			// TODO arrow à la *---->
			var color = HintColor;
			var width = HintLineWidth;
			var polySym = SymbolUtils.CreatePolygonSymbol(color);
			var stroke = SymbolUtils.CreateSolidStroke(color, width);
			var startMarker = SymbolUtils
			                  .CreateMarker(SymbolUtils.MarkerStyle.Circle, polySym, width * 3)
			                  .SetMarkerPlacementAtExtremities(ExtremityPlacement.JustBegin);
			var symbol = SymbolUtils.CreateLineSymbol(stroke, startMarker);
			_hintSymRef = symbol.MakeSymbolReference();
		}

		return _hintSymRef;
	}

	#endregion
}
