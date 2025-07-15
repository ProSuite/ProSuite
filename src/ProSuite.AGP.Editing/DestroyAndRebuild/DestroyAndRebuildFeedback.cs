using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using GeometryType = ArcGIS.Core.Geometry.GeometryType;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

public class DestroyAndRebuildFeedback
{
	private static readonly List<IDisposable> _overlays = new List<IDisposable>();

	private readonly CIMLineSymbol _lineSymbol;
	private readonly CIMPointSymbol _startPointSymbol;
	private readonly CIMPointSymbol _endPointSymbol;

	private readonly CIMPolygonSymbol _polygonSymbol;

	private readonly CIMPointSymbol _vertexMarkerSymbol;
	private readonly CIMPointSymbol _controlPointMarkerSymbol;

	public DestroyAndRebuildFeedback()
	{
		const SymbolUtils.FillStyle noFill = SymbolUtils.FillStyle.Null;
		var red = ColorFactory.Instance.CreateRGBColor(255, 0, 0);
		var cyan = ColorFactory.Instance.CreateRGBColor(0, 255, 255);
		var green = ColorFactory.Instance.CreateRGBColor(0, 128, 0);
		var magenta = ColorUtils.CreateRGB(240, 0, 248);

		_lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(magenta, 0.8);

		_startPointSymbol = QueuedTask
		                    .Run(() => SymbolFactory.Instance.ConstructPointSymbol(
			                         green, 6.5, SimpleMarkerStyle.Circle)).Result;
		_endPointSymbol = QueuedTask
		                  .Run(() => SymbolFactory.Instance.ConstructPointSymbol(
			                       red, 6.5, SimpleMarkerStyle.Square)).Result; //Diamond

		_vertexMarkerSymbol = QueuedTask
		                      .Run(() => SymbolFactory.Instance.ConstructPointSymbol(
			                           cyan, 6.5, SimpleMarkerStyle.Square)).Result;

		var stroke = SymbolUtils.CreateSolidStroke(magenta, 0.8);
		_polygonSymbol = SymbolUtils.CreatePolygonSymbol(null, noFill, stroke);

		_controlPointMarkerSymbol =
			CreateControlPointSymbol(6.5, cyan, ColorUtils.BlackRGB);
	}

	[CanBeNull]
	private static IDisposable AddOverlay([CanBeNull] Geometry geometry,
	                                      [NotNull] CIMSymbol cimSymbol)
	{
		if (geometry == null || geometry.IsEmpty)
		{
			return null;
		}

		IDisposable result = MapView.Active.AddOverlay(
			geometry, cimSymbol.MakeSymbolReference());

		return result;
	}

	private CIMPointSymbol CreateControlPointSymbol(double size, CIMColor fillColor,
	                                                CIMColor outlineColor)
	{
		double factor = Math.Sqrt(2.0);
		var symbolSize = size * factor; // to compensate diamond vs square (rot 45°)
		var stroke = SymbolUtils.CreateSolidStroke(outlineColor, symbolSize / 5);
		//var polySym = SymbolUtils.CreatePolygonSymbol(ColorUtils.WhiteRGB, SymbolUtils.FillStyle.Solid, stroke);
		var polySym =
			SymbolUtils.CreatePolygonSymbol(fillColor, SymbolUtils.FillStyle.Solid, stroke);
		var marker =
			SymbolUtils.CreateMarker(SymbolUtils.MarkerStyle.Diamond, polySym, symbolSize);
		var symbol = SymbolUtils.CreatePointSymbol(marker);

		return symbol;
	}

	#region Selection

	public bool UpdateSelection([CanBeNull] IList<Feature> selectedFeatures)
	{
		DisposeOverlays();

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
					//_pointOverlay = AddOverlay(geometry, _pointSymbol);
					break;
				case GeometryType.Polyline:
					_overlays.Add(AddOverlay(geometry, _lineSymbol));

					var startPointL = GeometryUtils.GetStartPoint(geometry as Polyline);
					var endPointL = GeometryUtils.GetEndPoint(geometry as Polyline);
					_overlays.Add(AddOverlay(startPointL, _startPointSymbol));

					CreateVertexMultipoint(geometry, out vertexMultipoint,
					                       out controlMultipoint);
					_overlays.Add(AddOverlay(vertexMultipoint, _vertexMarkerSymbol));
					_overlays.Add(AddOverlay(controlMultipoint, _controlPointMarkerSymbol));

					_overlays.Add(AddOverlay(endPointL, _endPointSymbol));
					break;
				case GeometryType.Polygon:
					_overlays.Add(AddOverlay(geometry, _polygonSymbol));

					var startPointP = GeometryUtils.GetStartPoint(geometry as Polygon);
					var endPointP = GeometryUtils.GetEndPoint(geometry as Polygon);
					_overlays.Add(AddOverlay(startPointP, _startPointSymbol));

					CreateVertexMultipoint(geometry, out vertexMultipoint,
					                       out controlMultipoint);
					_overlays.Add(AddOverlay(vertexMultipoint, _vertexMarkerSymbol));
					_overlays.Add(
						AddOverlay(controlMultipoint, _controlPointMarkerSymbol));

					_overlays.Add(AddOverlay(endPointP, _endPointSymbol));
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
