using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

/// <summary>
/// Provides display feedback for moving a point by drawing on top of the active map view
/// using Pro SDK overlays. This is a Pro SDK equivalent of ArcObjects' IMovePointFeedback.
/// </summary>
public class MovePointFeedback : IDisposable
{
	private readonly IMsg _msg = Msg.ForCurrentClass();
	private IDisposable _pointOverlay;
	private CIMSymbolReference _symbolRef;
	private CIMSymbol _symbol;
	private MapPoint _currentPoint;

	/// <summary>
	/// Gets or sets the symbol used to display the feedback point.
	/// </summary>
	public CIMSymbol Symbol
	{
		get => _symbol;
		set
		{
			_symbol = value;
			_symbolRef = null; // Force recreation of symbol reference
		}
	}

	/// <summary>
	/// Moves the feedback point to a new location.
	/// </summary>
	/// <param name="point">The new location to move to</param>
	public void MoveTo(MapPoint point)
	{
		if (point == null || point.IsEmpty)
		{
			Clear();
			return;
		}

		var mapView = MapView.Active;
		if (mapView == null) return;

		_currentPoint = point;
		var symbol = GetSymbolReference();

		if (_pointOverlay == null)
		{
			_pointOverlay = mapView.AddOverlay(point, symbol);
		}
		else
		{
			if (! mapView.UpdateOverlay(_pointOverlay, point, symbol))
			{
				_msg.Warn("UpdateOverlay() returned false; display feedback may be wrong");
			}
		}
	}

	/// <summary>
	/// Clears the feedback display.
	/// </summary>
	public void Clear()
	{
		_pointOverlay?.Dispose();
		_pointOverlay = null;
		_currentPoint = null;
	}

	/// <summary>
	/// Disposes of resources.
	/// </summary>
	public void Dispose()
	{
		Clear();
	}

	private CIMSymbolReference GetSymbolReference()
	{
		if (_symbolRef == null && _symbol != null)
		{
			_symbolRef = _symbol.MakeSymbolReference();
		}

		// If no symbol specified, create a default simple marker symbol
		if (_symbolRef == null)
		{
			var defaultSymbol = SymbolUtils.CreatePointSymbol(
				ColorUtils.CreateRGB(255, 0, 0),
				6);
			_symbolRef = defaultSymbol.MakeSymbolReference();
		}

		return _symbolRef;
	}
}
