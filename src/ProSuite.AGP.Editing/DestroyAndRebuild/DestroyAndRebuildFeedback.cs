using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using GeometryType = ArcGIS.Core.Geometry.GeometryType;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

public class DestroyAndRebuildFeedback
{
	private static readonly List<IDisposable> _overlays = new List<IDisposable>();

	private CIMLineSymbol _lineSymbol;
	private CIMPointSymbol _startPointSymbol;
	private CIMPointSymbol _endPointSymbol;
	private CIMPolygonSymbol _polygonSymbol;
	private CIMPointSymbol _vertexMarkerSymbol;
	private CIMPointSymbol _controlPointMarkerSymbol;
	private readonly bool _useOldSymbolization;

	// The map view the overlays are drawn on. Defaults to MapView.Active but can be set
	// explicitly (via UpdateSelection) so the feedback also shows in a stereo map view.
	[CanBeNull] private MapView _mapView;

	public DestroyAndRebuildFeedback(bool useOldSymbolization = false)
	{
		_useOldSymbolization = useOldSymbolization;
	}

	/// <summary>
	///	Initializes the symbols used for rendering lines, points, and polygons. This method must
	/// be called on a queued task before using the feedback.
	/// </summary>
	public void InitializeSymbolsQueued()
	{
		const SymbolUtils.FillStyle noFill = SymbolUtils.FillStyle.Null;

		var blue = ColorFactory.Instance.CreateRGBColor(0, 0, 200);
		var red = ColorFactory.Instance.CreateRGBColor(255, 0, 0);
		var cyan = ColorFactory.Instance.CreateRGBColor(0, 255, 255);
		var green = ColorFactory.Instance.CreateRGBColor(0, 128, 0);
		var magenta = ColorUtils.CreateRGB(240, 0, 248);
		var yellow = ColorUtils.CreateRGB(255, 224, 0);

		if (_useOldSymbolization)
		{
			_lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(blue, 2.0);

			_startPointSymbol = SymbolFactory.Instance.ConstructPointSymbol(
				blue, 8, SimpleMarkerStyle.Circle);

			_endPointSymbol =
				SymbolFactory.Instance.ConstructPointSymbol(
					red, 12, SimpleMarkerStyle.Diamond);

			var stroke = SymbolUtils.CreateSolidStroke(blue, 2.0);
			_polygonSymbol = SymbolUtils.CreatePolygonSymbol(null, noFill, stroke);

			_vertexMarkerSymbol = null;
			_controlPointMarkerSymbol = null;
		}
		else
		{
			_lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(magenta, 0.8);

			_startPointSymbol = SymbolFactory.Instance.ConstructPointSymbol(
				green, 6.5, SimpleMarkerStyle.Circle);

			_endPointSymbol = SymbolFactory.Instance.ConstructPointSymbol(
				red, 6.5, SimpleMarkerStyle.Square); //Diamond

			_vertexMarkerSymbol = SymbolFactory.Instance.ConstructPointSymbol(
				cyan, 6.5, SimpleMarkerStyle.Square);

			var stroke = SymbolUtils.CreateSolidStroke(magenta, 0.8);
			_polygonSymbol = SymbolUtils.CreatePolygonSymbol(null, noFill, stroke);

			_controlPointMarkerSymbol =
				CreateControlPointSymbol(6.5, yellow, ColorUtils.BlackRGB, 1.5);
		}
	}

	[ItemCanBeNull]
	private async Task<IDisposable> AddOverlayAsync([CanBeNull] Geometry geometry,
	                                                [NotNull] CIMSymbol cimSymbol)
	{
		if (geometry == null || geometry.IsEmpty)
		{
			return null;
		}

		MapView mapView = _mapView ?? MapView.Active;

		if (mapView == null)
		{
			return null;
		}

		// A stereo map view does not render an overlay whose geometry contains NaN Z values
		// (and it can even crash), so skip those. In a 2D view they would simply be ignored.
		if (MapUtils.IsStereoMapView(mapView) && HasNaNZ(geometry))
		{
			return null;
		}

		// Use the async overlay API: unlike the synchronous AddOverlay, it also renders in a
		// stereo map view.
		IDisposable result = await mapView.AddOverlayAsync(
			                     geometry, cimSymbol.MakeSymbolReference());

		return result;
	}

	private static bool HasNaNZ([NotNull] Geometry geometry)
	{
		return geometry is Multipart multipart &&
		       multipart.Points.Any(point => double.IsNaN(point.Z));
	}

	private CIMPointSymbol CreateControlPointSymbol(double size, CIMColor fillColor,
	                                                CIMColor outlineColor, double outlineWidth)
	{
		double factor = Math.Sqrt(2.0);
		var symbolSize = size * factor; // to compensate diamond vs square (rot 45°)
		var stroke = SymbolUtils.CreateSolidStroke(outlineColor, outlineWidth); //symbolSize / 5);
		//var polySym = SymbolUtils.CreatePolygonSymbol(ColorUtils.WhiteRGB, SymbolUtils.FillStyle.Solid, stroke);
		var polySym =
			SymbolUtils.CreatePolygonSymbol(fillColor, SymbolUtils.FillStyle.Solid, stroke);
		var marker =
			SymbolUtils.CreateMarker(SymbolUtils.MarkerStyle.Diamond, polySym, symbolSize);
		var symbol = SymbolUtils.CreatePointSymbol(marker);

		return symbol;
	}

	#region Selection

	public async Task<bool> UpdateSelectionAsync([CanBeNull] IList<Feature> selectedFeatures,
	                                             [CanBeNull] MapView mapView = null)
	{
		DisposeOverlays();

		// Draw on the given view (e.g. the stereo view) or fall back to the active view.
		_mapView = mapView ?? MapView.Active;

		if (selectedFeatures == null || selectedFeatures.Count == 0)
		{
			return false;
		}

		foreach (Feature selectedFeature in selectedFeatures)
		{
			Geometry geometry = selectedFeature.GetShape();
			GeometryType geometryType = geometry.GeometryType;

			Multipoint vertexMultipoint;
			Multipoint controlMultipoint;

			switch (geometryType)
			{
				case GeometryType.Point:
					// Use start point symbol for points, consistent across both versions
					_overlays.Add(
						await AddOverlayAsync(geometry, Assert.NotNull(_startPointSymbol)));
					break;
				case GeometryType.Polyline:
					_overlays.Add(await AddOverlayAsync(geometry, Assert.NotNull(_lineSymbol)));

					var startPointL = GeometryUtils.GetStartPoint(geometry as Polyline);
					var endPointL = GeometryUtils.GetEndPoint(geometry as Polyline);
					_overlays.Add(
						await AddOverlayAsync(startPointL, Assert.NotNull(_startPointSymbol)));

					if (! _useOldSymbolization)
					{
						CreateVertexMultipoint(geometry, out vertexMultipoint,
						                       out controlMultipoint);
						_overlays.Add(await AddOverlayAsync(vertexMultipoint,
						                                    Assert.NotNull(_vertexMarkerSymbol)));
						_overlays.Add(await AddOverlayAsync(controlMultipoint,
						                                    Assert.NotNull(
							                                    _controlPointMarkerSymbol)));
					}

					_overlays.Add(
						await AddOverlayAsync(endPointL, Assert.NotNull(_endPointSymbol)));
					break;
				case GeometryType.Polygon:
					// Old symbolization: for polygons, show only the outline
					_overlays.Add(await AddOverlayAsync(geometry, Assert.NotNull(_polygonSymbol)));

					if (! _useOldSymbolization)
					{
						var startPointP = GeometryUtils.GetStartPoint(geometry as Polygon);
						var endPointP = GeometryUtils.GetEndPoint(geometry as Polygon);
						_overlays.Add(
							await AddOverlayAsync(startPointP, Assert.NotNull(_startPointSymbol)));

						CreateVertexMultipoint(geometry, out vertexMultipoint,
						                       out controlMultipoint);
						_overlays.Add(await AddOverlayAsync(vertexMultipoint,
						                                    Assert.NotNull(_vertexMarkerSymbol)));
						_overlays.Add(await AddOverlayAsync(controlMultipoint,
						                                    Assert.NotNull(
							                                    _controlPointMarkerSymbol)));

						_overlays.Add(
							await AddOverlayAsync(endPointP, Assert.NotNull(_endPointSymbol)));
					}

					break;

				case GeometryType.Multipatch:
					Polyline multipatchOutline = GetMultipatchOutline((Multipatch) geometry);
					_overlays.Add(await AddOverlayAsync(multipatchOutline,
					                                    Assert.NotNull(_lineSymbol)));
					break;

				default:
					throw new ArgumentOutOfRangeException(
						nameof(geometryType), geometryType, null);
			}
		}

		return true;
	}

	private static void CreateVertexMultipoint(Geometry geometry,
	                                           out Multipoint vertexMultipoint,
	                                           out Multipoint controlMultipoint)
	{
		vertexMultipoint = null;
		controlMultipoint = null;

		var multipart = Assert.NotNull(geometry as Multipart);
		// Group into vertex and control points, but skip the start and end points
		var groups = multipart.Points.Skip(1).SkipLast(1).GroupBy((point) => point.ID < 1);

		foreach (IGrouping<bool, MapPoint> grouping in groups)
		{
			var builder = new MultipointBuilderEx(geometry.SpatialReference);
			builder.AddPoints(grouping);
			var multipoint = builder.ToGeometry();
			// simplify to remove duplicate vertices:
			var simplified =
				(Multipoint) GeometryEngine.Instance.SimplifyAsFeature(multipoint, true);
			if (grouping.Key)
			{
				vertexMultipoint = simplified;
			}
			else
			{
				controlMultipoint = simplified;
			}
		}
	}

	/// <summary>
	/// Builds a (multipart) polyline of the multipatch's patch edges, used to render the
	/// target outline as feedback (no faces, no vertices).
	/// </summary>
	[CanBeNull]
	private static Polyline GetMultipatchOutline([NotNull] Multipatch multipatch)
	{
		if (multipatch.IsEmpty)
		{
			return null;
		}

		var builder = new PolylineBuilderEx(multipatch.SpatialReference);

		var multipatchBuilder = new MultipatchBuilderEx(multipatch);

		foreach (Patch patch in multipatchBuilder.Patches)
		{
			if (patch?.Coords == null || patch.Coords.Count < 2)
			{
				continue;
			}

			builder.AddPart(patch.Coords);
		}

		return builder.ToGeometry();
	}

	public void ClearSelection()
	{
		DisposeOverlays();
	}

	private static void DisposeOverlays()
	{
		foreach (IDisposable overlay in _overlays)
		{
			overlay?.Dispose();
		}

		_overlays.Clear();
	}

	#endregion Selection
}
