using System;
using System.Collections.Generic;
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
	private IDisposable _selectedControlPointOverlay;
	private IDisposable _unselectedControlPointOverlay;
	private IDisposable _activeVertexOverlay;

	private CIMSymbolReference _involvedSegmentsSymRef;
	private CIMSymbolReference _selectedVertexSymRef;
	private CIMSymbolReference _unselectedVertexSymRef;
	private CIMSymbolReference _selectedControlPointSymRef;
	private CIMSymbolReference _unselectedControlPointSymRef;
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
			if (! (value >= 0 && double.IsFinite(value)))
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

	/// <param name="mws">The white selection to draw; pass null to remove</param>
	/// <remarks>Must call on MCT</remarks>
	public void DrawWhiteSelection(IMapWhiteSelection mws)
	{
		// - wire frame of involved features (polylines and polygons)
		// - unselected vertices (empty square)
		// - selected vertices (filled square)

		if (mws is null)
		{
			_involvedSegmentsOverlay?.Dispose();
			_involvedSegmentsOverlay = null;

			_selectedVertexOverlay?.Dispose();
			_selectedVertexOverlay = null;

			_unselectedVertexOverlay?.Dispose();
			_unselectedVertexOverlay = null;

			_selectedControlPointOverlay?.Dispose();
			_selectedControlPointOverlay = null;

			_unselectedControlPointOverlay?.Dispose();
			_unselectedControlPointOverlay = null;

			return;
		}

		var mapView = MapView.Active;
		if (mapView is null) return;

		var segmentsSymRef = GetInvolvedSegmentsSymRef();
		var segments = CreateSegmentsPolyline(mws);

		if (segments is null || segments.IsEmpty)
		{
			_involvedSegmentsOverlay?.Dispose();
			_involvedSegmentsOverlay = null;
		}
		else if (_involvedSegmentsOverlay is null)
		{
			_involvedSegmentsOverlay = mapView.AddOverlay(segments, segmentsSymRef);
		}
		else
		{
			UpdateOverlay(mapView, _involvedSegmentsOverlay, segments, segmentsSymRef);
		}

		var unselectedVertexSymbol = GetUnselectedVertexSymRef();
		var unselectedVertices = CreateVertexMultipoint(mws, false, false);

		if (unselectedVertices is null || unselectedVertices.IsEmpty)
		{
			_unselectedVertexOverlay?.Dispose();
			_unselectedVertexOverlay = null;
		}
		else if (_unselectedVertexOverlay is null)
		{
			_unselectedVertexOverlay =
				mapView.AddOverlay(unselectedVertices, unselectedVertexSymbol);
		}
		else
		{
			UpdateOverlay(mapView, _unselectedVertexOverlay, unselectedVertices,
			              unselectedVertexSymbol);
		}

		var unselectedControlSymbol = GetUnselectedControlPointSymRef();
		var unselectedControlPoints = CreateVertexMultipoint(mws, false, true);

		if (unselectedControlPoints is null || unselectedControlPoints.IsEmpty)
		{
			_unselectedControlPointOverlay?.Dispose();
			_unselectedControlPointOverlay = null;
		}
		else if (_unselectedControlPointOverlay is null)
		{
			_unselectedControlPointOverlay =
				mapView.AddOverlay(unselectedControlPoints, unselectedControlSymbol);
		}
		else
		{
			UpdateOverlay(mapView, _unselectedControlPointOverlay, unselectedControlPoints,
			              unselectedControlSymbol);
		}

		var selectedVertexSymbol = GetSelectedVertexSymRef();
		var selectedVertices = CreateVertexMultipoint(mws, true, false);

		if (selectedVertices is null || selectedVertices.IsEmpty)
		{
			_selectedVertexOverlay?.Dispose();
			_selectedVertexOverlay = null;
		}
		else if (_selectedVertexOverlay is null)
		{
			_selectedVertexOverlay = mapView.AddOverlay(selectedVertices, selectedVertexSymbol);
		}
		else
		{
			UpdateOverlay(mapView, _selectedVertexOverlay, selectedVertices, selectedVertexSymbol);
		}

		var selectedControlSymbol = GetSelectedControlPointSymRef();
		var selectedControlPoints = CreateVertexMultipoint(mws, true, true);

		if (selectedControlPoints is null || selectedControlPoints.IsEmpty)
		{
			_selectedControlPointOverlay?.Dispose();
			_selectedControlPointOverlay = null;
		}
		else if (_selectedControlPointOverlay is null)
		{
			_selectedControlPointOverlay =
				mapView.AddOverlay(selectedControlPoints, selectedControlSymbol);
		}
		else
		{
			UpdateOverlay(mapView, _selectedControlPointOverlay, selectedControlPoints,
			              selectedControlSymbol);
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

		_selectedControlPointOverlay?.Dispose();
		_selectedControlPointOverlay = null;

		_unselectedControlPointOverlay?.Dispose();
		_unselectedControlPointOverlay = null;

		_activeVertexOverlay?.Dispose();
		_activeVertexOverlay = null;
	}

	public void Dispose()
	{
		Clear();
	}

	private static void UpdateOverlay(MapView mapView, IDisposable overlay, Geometry geometry,
	                                  CIMSymbolReference symbol)
	{
		if (! mapView.UpdateOverlay(overlay, geometry, symbol))
		{
			_msg.Warn("UpdateOverlay() returned false; display feedback may be wrong; see K2#37");
			// ask Redlands when this can happen and what we should do (K2#37)
		}
	}

	private static bool IsControlPoint(MapPoint point)
	{
		if (point is null) return false;
		return point.HasID && point.ID != 0;
	}

	/// <summary>
	/// Create a multipoint with all the vertices of the given white selection.
	/// Use parameters to control which vertices are added to the multipoint:
	/// all, only selected, or only unselected; and, optionally, amongst those
	/// pick only regular vertices or only control points.
	/// </summary>
	private static Multipoint CreateVertexMultipoint(
		IMapWhiteSelection mws, bool? selected = null, bool? controlPoints = null)
	{
		var all = mws.GetLayerSelections();

		var builder = new MultipointBuilderEx(mws.Map.SpatialReference);

		foreach (var ws in all)
		{
			IEnumerable<MapPoint> points;

			if (selected is null)
			{
				// both selected and unselected vertices:
				points = ws.GetSelectedVertices().Concat(ws.GetUnselectedVertices());
			}
			else
			{
				// either selected or unselected vertices:
				points = selected.Value ? ws.GetSelectedVertices() : ws.GetUnselectedVertices();
			}

			if (controlPoints.HasValue)
			{
				bool wantControlPoint = controlPoints.Value; // do not inline!
				points = points.Where(p => IsControlPoint(p) == wantControlPoint);
			}

			builder.AddPoints(points);
		}

		var multipoint = builder.ToGeometry();

		// simplify to remove duplicate vertices:
		var result = GeometryEngine.Instance.SimplifyAsFeature(multipoint, true);

		return (Multipoint) result;
	}

	private static Polyline CreateSegmentsPolyline(IMapWhiteSelection mws)
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
		_selectedControlPointSymRef = null;
		_unselectedControlPointSymRef = null;
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
	private CIMSymbolReference GetSelectedControlPointSymRef()
	{
		if (_selectedControlPointSymRef is null)
		{
			double factor = Math.Sqrt(2);
			var color = SelectionColor;
			var size = SelectionVertexSize * factor; // to compensate diamond vs square (rot 45°)
			var symbol =
				SymbolUtils.CreatePointSymbol(color, size, SymbolUtils.MarkerStyle.Diamond);
			_selectedControlPointSymRef = symbol.MakeSymbolReference();
		}

		return _selectedControlPointSymRef;
	}

	/// <remarks>Must call on MCT</remarks>>
	private CIMSymbolReference GetUnselectedVertexSymRef()
	{
		if (_unselectedVertexSymRef is null)
		{
			var color = SelectionColor;
			var size = SelectionVertexSize;
			var stroke = SymbolUtils.CreateSolidStroke(color, size / 5);
			var polySym =
				SymbolUtils.CreatePolygonSymbol(ColorUtils.WhiteRGB, SymbolUtils.FillStyle.Solid,
				                                stroke);
			var marker = SymbolUtils.CreateMarker(SymbolUtils.MarkerStyle.Square, polySym, size);
			var symbol = SymbolUtils.CreatePointSymbol(marker);
			_unselectedVertexSymRef = symbol.MakeSymbolReference();
		}

		return _unselectedVertexSymRef;
	}

	/// <remarks>Must call on MCT</remarks>>
	private CIMSymbolReference GetUnselectedControlPointSymRef()
	{
		if (_unselectedControlPointSymRef is null)
		{
			double factor = Math.Sqrt(2.0);
			var color = SelectionColor;
			var size = SelectionVertexSize * factor; // to compensate diamond vs square (rot 45°)
			var stroke = SymbolUtils.CreateSolidStroke(color, size / 5);
			var polySym =
				SymbolUtils.CreatePolygonSymbol(ColorUtils.WhiteRGB, SymbolUtils.FillStyle.Solid,
				                                stroke);
			var marker = SymbolUtils.CreateMarker(SymbolUtils.MarkerStyle.Diamond, polySym, size);
			var symbol = SymbolUtils.CreatePointSymbol(marker);
			_unselectedControlPointSymRef = symbol.MakeSymbolReference();
		}

		return _unselectedControlPointSymRef;
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
