using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

public class SketchRecorder
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly Stack<Geometry> _sketches = new();
	private readonly SketchLatch _latch = new();
	[CanBeNull] private List<IDisposable> _overlays;
	private bool _recording;
	private bool _suspended;

	private CIMLineSymbol _lineSymbol;
	private CIMPolygonSymbol _polygonSymbol;
	private CIMPointSymbol _regularUnselectedSymbol;
	private CIMPointSymbol _currentUnselectedSymbol;

	public SketchRecorder()
	{
		WireEvents();
	}

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

	public async Task RecordAsync()
	{
		Assert.False(_recording, "Already recording");
		_recording = true;

		Geometry sketch = await MapView.Active.GetCurrentSketchAsync();

		TryPush(sketch);
	}

	public async Task SuspendAsync()
	{
		Assert.False(_suspended, "Already suspended");
		Assert.True(_recording, "Not recording");
		_suspended = true;

		var mapView = MapView.Active;
		if (mapView is null)
		{
			return;
		}

		Geometry geometry = await mapView.GetCurrentSketchAsync();

		if (geometry is { IsEmpty: true })
		{
			return;
		}

		// add two extra for start (or end) point and sketch
		int capacity = GeometryUtils.GetPointCount(geometry) + 2;
		_overlays = new List<IDisposable>(capacity);

		CIMSymbolReference regularUnselectedSymbRef = RegularUnselectedSymbol.MakeSymbolReference();
		CIMSymbolReference currentUnselectedSymbRef = CurrentUnselectedSymbol.MakeSymbolReference();

		if (geometry is Multipart multipart)
		{
			var builder = new MultipointBuilderEx(mapView.Map.SpatialReference);

			foreach (MapPoint point in multipart.Points)
			{
				builder.AddPoint(point);
			}

			var multipoint = builder.ToGeometry();
			_overlays.Add(await mapView.AddOverlayAsync(multipoint, regularUnselectedSymbRef));
		}

		if (geometry is Polyline polyline)
		{
			CIMSymbolReference lineSymbRef = LineSymbol.MakeSymbolReference();
			_overlays.Add(await mapView.AddOverlayAsync(polyline, lineSymbRef));

			var endPoint = GeometryUtils.GetEndPoint(polyline);
			if (endPoint != null)
			{
				_overlays.Add(await mapView.AddOverlayAsync(endPoint, currentUnselectedSymbRef));
			}
		}
		else if (geometry is Polygon polygon)
		{
			CIMSymbolReference polySymbRef = PolygonSymbol.MakeSymbolReference();
			_overlays.Add(await mapView.AddOverlayAsync(polygon, polySymbRef));

			// start and end point of a polygon are geometrically equal
			var points = polygon.Points;
			int count = points.Count;
			if (count == 0) return;

			MapPoint endPoint = count > 2 ? points[count - 2] : points[0];

			if (endPoint != null)
			{
				_overlays.Add(await mapView.AddOverlayAsync(endPoint, currentUnselectedSymbRef));
			}
		}
	}

	public async Task SetSketchesAsync()
	{
		try
		{
			Assert.True(_suspended, "Not suspended");
			Assert.True(_recording, "Not recording");

			_msg.VerboseDebug(() => $"Replay: {_sketches.Count} sketches");

			foreach (Geometry sketch in _sketches.Reverse())
			{
				_latch.Increment();
				await MapView.Active.SetCurrentSketchAsync(sketch);
			}
		}
		catch (Exception e)
		{
			_msg.Error($"Error setting current sketch: {e.Message}", e);
		}
	}

	public void Resume()
	{
		Assert.True(_suspended, "Not suspended");
		_suspended = false;

		if (_overlays == null) return;

		foreach (IDisposable overlay in _overlays)
		{
			overlay.Dispose();
		}

		_overlays.Clear();
	}

	public void Reset()
	{
		Assert.True(_recording, "Not recording");

		UnwireEvents();

		_recording = false;
		_suspended = false;
		_sketches.Clear();
		_latch.Reset();

		if (_overlays == null) return;

		foreach (IDisposable overlay in _overlays)
		{
			overlay.Dispose();
		}

		_overlays.Clear();
	}

	private void TryPush(Geometry sketch, [CallerMemberName] string caller = null)
	{
		if (sketch is not { IsEmpty: false })
		{
			return;
		}

		_sketches.Push(sketch);
		_msg.VerboseDebug(() => $"{caller}: {_sketches.Count} sketches");
	}

	private void OnSketchModified(SketchModifiedEventArgs args)
	{
		try
		{
			_msg.VerboseDebug(() => $"{args.SketchOperationType}");

			Assert.True(_recording, "not recording");

			if (_latch.IsLatched)
			{
				_latch.Decrement();
				Assert.True(_latch.Count >= 0, "Sketch stack isn't in sync with latch");
				return;
			}
			
			if (args.IsUndo)
			{
				Assert.NotNull(_sketches.Pop());
				_msg.VerboseDebug(() => $"{nameof(OnSketchModified)} pop: {_sketches.Count} sketches");

				if (_sketches.Count == 1)
				{
					_sketches.Clear();
					_msg.VerboseDebug(() => "clear sketches");
				}

				return;
			}

			TryPush(args.CurrentSketch);
		}
		catch (Exception e)
		{
			_msg.Error($"Error in {nameof(OnSketchModified)}: {e.Message}", e);
		}
	}

	private void OnSketchCompleted(SketchCompletedEventArgs args)
	{
		Assert.True(_recording, "not recording");

		if (_suspended)
		{
			return;
		}

		_msg.VerboseDebug(() => $"{nameof(OnSketchCompleted)}: {_sketches.Count} sketches");

		_sketches.Clear();
		_latch.Reset();
	}

	private void WireEvents()
	{
		SketchModifiedEvent.Subscribe(OnSketchModified);
		SketchCompletedEvent.Subscribe(OnSketchCompleted);
	}

	private void UnwireEvents()
	{
		SketchModifiedEvent.Unsubscribe(OnSketchModified);
		SketchCompletedEvent.Unsubscribe(OnSketchCompleted);
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
		return [blackStroke, whiteStroke];
	}

	private static CIMPointSymbol CreateSketchVertexSymbol(CIMRGBColor color)
	{
		var outline = SymbolUtils.CreateSolidStroke(color, 1.5);
		CIMPolygonSymbol polygonSymbol = SymbolUtils.CreatePolygonSymbol(outline);
		Geometry geometry = SymbolUtils.CreateMarkerGeometry(SymbolUtils.MarkerStyle.Square);
		CIMVectorMarker marker = SymbolUtils.CreateMarker(geometry, polygonSymbol, 5);
		return SymbolUtils.CreatePointSymbol(marker);
	}

	private class SketchLatch
	{
		public int Count { get; private set; }

		public bool IsLatched => Count > 0;

		public void Increment()
		{
			Count++;
		}

		public void Decrement()
		{
			Count--;
		}

		public void Reset()
		{
			Count = 0;
		}
	}
}
