using System;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

/// <summary>
/// Provide display feedback for the white selection by drawing
/// on top of the active map view using Pro SDK “overlays”.
/// </summary>
public class WhiteSelectionFeedback : IDisposable
{
	private CIMColor _selectionColor;
	private double _selectionVertexSize = 6.5;
	private double _selectionLineWidth = 0.8;

	// TODO lock access to overlay disposables?
	private IDisposable _involvedSegmentsOverlay;
	private IDisposable _selectedVertexOverlay;
	private IDisposable _unselectedVertexOverlay;
	private IDisposable _activeVertexOverlay;

	private CIMSymbolReference _involvedSegmentsSymRef;
	private CIMSymbolReference _selectedVertexSymRef;
	private CIMSymbolReference _unselectedVertexSymRef;
	private CIMSymbolReference _activeVertexSymRef;

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public CIMColor SelectionColor
	{
		// Don't use the "official" selection color as the default, so
		// that the white selection stands out from the regular selection
		get => _selectionColor ??= ColorUtils.CreateRGB(240, 0, 248);
		set
		{
			_selectionColor = value;
			InvalidateCachedSymbols();
		}
	}

	public double SelectionVertexSize
	{
		get => _selectionVertexSize;
		set
		{
			if (!(value >= 0 && double.IsFinite(value)))
				throw new ArgumentOutOfRangeException(nameof(value));
			_selectionVertexSize = value;
			InvalidateCachedSymbols();
		}
	}

	public double SelectionLineWidth
	{
		get => _selectionLineWidth;
		set
		{
			if (! (value >= 0 && double.IsFinite(value)))
				throw new ArgumentOutOfRangeException(nameof(value));
			_selectionLineWidth = value;
			InvalidateCachedSymbols();
		}
	}

	/// <param name="ws">The white selection to draw; pass null to remove</param>
	/// <remarks>Must call on MCT</remarks>
	public void DrawWhiteSelection(MapWhiteSelection ws)
	{
		// - wire frame of involved features (polylines and polygons)
		// - unselected vertices (empty square)
		// - selected vertices (filled square)

		if (ws is null)
		{
			_involvedSegmentsOverlay?.Dispose();
			_involvedSegmentsOverlay = null;

			_selectedVertexOverlay?.Dispose();
			_selectedVertexOverlay = null;

			_unselectedVertexOverlay?.Dispose();
			_unselectedVertexOverlay = null;

			return;
		}

		var mapView = MapView.Active;
		if (mapView is null) return;

		var segmentsSymRef = GetInvolvedSegmentsSymRef();
		var segments = CreateSegmentsPolyline(ws);

		if (segments is null || segments.IsEmpty)
		{
			_involvedSegmentsOverlay?.Dispose();
			_involvedSegmentsOverlay = null;
		}
		else
		{
			if (_involvedSegmentsOverlay is null)
			{
				_involvedSegmentsOverlay = mapView.AddOverlay(segments, segmentsSymRef);
			}
			else
			{
				UpdateOverlay(mapView, _involvedSegmentsOverlay, segments, segmentsSymRef);
			}
		}

		var unselectedSymbol = GetUnselectedVertexSymRef();
		var unselectedMultipoint = CreateUnselectedVertexMultipoint(ws);

		if (unselectedMultipoint is null || unselectedMultipoint.IsEmpty)
		{
			_unselectedVertexOverlay?.Dispose();
			_unselectedVertexOverlay = null;
		}
		else
		{
			if (_unselectedVertexOverlay is null)
			{
				_unselectedVertexOverlay = mapView.AddOverlay(unselectedMultipoint, unselectedSymbol);
			}
			else
			{
				UpdateOverlay(mapView, _unselectedVertexOverlay, unselectedMultipoint, unselectedSymbol);
			}
		}

		var selectedSymbol = GetSelectedVertexSymRef();
		var selectedMultipoint = CreateSelectedVertexMultipoint(ws);

		if (selectedMultipoint is null || selectedMultipoint.IsEmpty)
		{
			_selectedVertexOverlay?.Dispose();
			_selectedVertexOverlay = null;
		}
		else
		{
			if (_selectedVertexOverlay is null)
			{
				_selectedVertexOverlay = mapView.AddOverlay(selectedMultipoint, selectedSymbol);
			}
			else
			{
				UpdateOverlay(mapView, _selectedVertexOverlay, selectedMultipoint, selectedSymbol);
			}
		}
	}

	/// <param name="point">The vertex to draw; pass null to remove</param>
	/// <remarks>Must call on MCT</remarks>
	public void DrawActiveVertex(MapPoint point)
	{
		if (point is null || point.IsEmpty)
		{
			ClearActiveVertex();
		}
		else
		{
			var mapView = MapView.Active;
			if (mapView is null) return;

			var symbol = GetCaughtVertexSymRef();

			if (_activeVertexOverlay is null)
			{
				_activeVertexOverlay = mapView.AddOverlay(point, symbol);
			}
			else
			{
				UpdateOverlay(mapView, _activeVertexOverlay, point, symbol);
			}
		}
	}

	public void ClearActiveVertex()
	{
		_activeVertexOverlay?.Dispose();
		_activeVertexOverlay = null;
	}

	public void Clear()
	{
		_involvedSegmentsOverlay?.Dispose();
		_involvedSegmentsOverlay = null;

		_selectedVertexOverlay?.Dispose();
		_selectedVertexOverlay = null;

		_unselectedVertexOverlay?.Dispose();
		_unselectedVertexOverlay = null;

		_activeVertexOverlay?.Dispose();
		_activeVertexOverlay = null;
	}

	public void Dispose()
	{
		Clear();
	}

	private static void UpdateOverlay(MapView mapView, IDisposable overlay, Geometry geometry, CIMSymbolReference symbol)
	{
		if (!mapView.UpdateOverlay(overlay, geometry, symbol))
		{
			_msg.Warn("UpdateOverlay() returned false; display feedback may be wrong; see K2#37");
			// ask Redlands when this can happen and what we should do (K2#37)
		}
	}

	private static Multipoint CreateSelectedVertexMultipoint(MapWhiteSelection mws)
	{
		var all = mws.GetLayerSelections();

		var builder = new MultipointBuilderEx(mws.MapView.Map.SpatialReference);

		foreach (var ws in all)
		{
			var points = ws.GetSelectedVertices();
			builder.AddPoints(points);
		}

		var multipoint = builder.ToGeometry();

		var result = GeometryEngine.Instance.SimplifyAsFeature(multipoint, true);

		return (Multipoint) result;
	}

	private static Multipoint CreateUnselectedVertexMultipoint(MapWhiteSelection mws)
	{
		var all = mws.GetLayerSelections();

		var builder = new MultipointBuilderEx(mws.MapView.Map.SpatialReference);

		foreach (var ws in all)
		{
			var points = ws.GetUnselectedVertices();
			builder.AddPoints(points);
		}

		var multipoint = builder.ToGeometry();

		// simplify to remove duplicate vertices:
		var result = GeometryEngine.Instance.SimplifyAsFeature(multipoint, true);

		return (Multipoint) result;
	}

	private static Polyline CreateSegmentsPolyline(MapWhiteSelection mws)
	{
		var all = mws.GetLayerSelections();

		var builder = new PolylineBuilderEx();

		var paths = all.SelectMany(ws => ws.GetInvolvedShapes())
		               .OfType<Multipart>()
		               .SelectMany(multipart => multipart.Parts);

		foreach (var path in paths)
		{
			const bool startNewPart = true;
			builder.AddSegments(path, startNewPart);
		}

		return builder.ToGeometry();
	}

	private void InvalidateCachedSymbols()
	{
		_selectedVertexSymRef = null;
		_unselectedVertexSymRef = null;
		_involvedSegmentsSymRef = null;
		_activeVertexSymRef = null;
	}

	/// <remarks>Must call on MCT</remarks>>
	private CIMSymbolReference GetCaughtVertexSymRef()
	{
		if (_activeVertexSymRef is null)
		{
			var color = SelectionColor;
			var size = SelectionVertexSize * 3;
			var stroke = SymbolUtils.CreateSolidStroke(color, size / 8);
			var polySym = SymbolUtils.CreatePolygonSymbol(null, SymbolUtils.FillStyle.Null, stroke);
			var marker = SymbolUtils.CreateMarker(SymbolUtils.MarkerStyle.Circle, polySym, size);
			var symbol = SymbolUtils.CreatePointSymbol(marker);
			_activeVertexSymRef = symbol.MakeSymbolReference();
		}

		return _activeVertexSymRef;
	}

	/// <remarks>Must call on MCT</remarks>>
	private CIMSymbolReference GetSelectedVertexSymRef()
	{
		if (_selectedVertexSymRef is null)
		{
			var color = SelectionColor;
			var size = SelectionVertexSize;
			var symbol = SymbolUtils.CreatePointSymbol(color, size, SymbolUtils.MarkerStyle.Square);
			_selectedVertexSymRef = symbol.MakeSymbolReference();
		}

		return _selectedVertexSymRef;
	}

	/// <remarks>Must call on MCT</remarks>>
	private CIMSymbolReference GetUnselectedVertexSymRef()
	{
		if (_unselectedVertexSymRef is null)
		{
			var color = SelectionColor;
			var size = SelectionVertexSize;
			var stroke = SymbolUtils.CreateSolidStroke(color, size / 5);
			var polySym = SymbolUtils.CreatePolygonSymbol(ColorUtils.WhiteRGB, SymbolUtils.FillStyle.Solid, stroke);
			var marker = SymbolUtils.CreateMarker(SymbolUtils.MarkerStyle.Square, polySym, size);
			var symbol = SymbolUtils.CreatePointSymbol(marker);
			_unselectedVertexSymRef = symbol.MakeSymbolReference();
		}

		return _unselectedVertexSymRef;
	}

	/// <remarks>Must call on MCT</remarks>>
	private CIMSymbolReference GetInvolvedSegmentsSymRef()
	{
		if (_involvedSegmentsSymRef is null)
		{
			var color = SelectionColor;
			var symbol = SymbolUtils.CreateLineSymbol(color, SelectionLineWidth);
			_involvedSegmentsSymRef = symbol.MakeSymbolReference();
		}

		return _involvedSegmentsSymRef;
	}
}
