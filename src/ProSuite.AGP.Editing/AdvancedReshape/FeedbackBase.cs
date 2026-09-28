using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArcGIS.Core.Geometry;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public abstract class FeedbackBase
	{
		public static readonly List<IDisposable> _overlays = new List<IDisposable>();

		public IDisposable _openJawReplacedEndPointOverlay;

		public IDisposable _polygonPreviewOverlayAdd;
		public IDisposable _polygonPreviewOverlayRemove;

		public MapPoint _lastDrawnOpenJawPoint;

		public CIMPolygonSymbol _addAreaSymbol;
		public CIMPolygonSymbol _removeAreaSymbol;

		public CIMPolygonSymbol _polygonSymbol;
		public CIMLineSymbol _lineSymbol;
		public CIMPointSymbol _startPointSymbol;
		public CIMPointSymbol _endPointSymbol;
		public CIMPointSymbol _vertexMarkerSymbol;
		public CIMPointSymbol _controlPointMarkerSymbol;

		/// <summary>
		/// Initializes the symbols for the feedback. Must be called on the QueuedTask before any
		/// feedback is drawn.
		/// </summary>
		/// <param name="useProStyle"></param>
		public void InitializeSymbolsQueued(bool useProStyle = false)
		{
			const SymbolUtils.FillStyle noFill = SymbolUtils.FillStyle.Null;

			var blue = ColorFactory.Instance.CreateRGBColor(0, 0, 200);
			var red = ColorFactory.Instance.CreateRGBColor(255, 0, 0);
			var cyan = ColorFactory.Instance.CreateRGBColor(0, 255, 255);
			var green = ColorFactory.Instance.CreateRGBColor(0, 128, 0);
			var magenta = ColorUtils.CreateRGB(240, 0, 248);
			var yellow = ColorUtils.CreateRGB(255, 224, 0);

			_addAreaSymbol = SymbolUtils.CreateHatchFillSymbol(0, 255, 0, 90);
			_removeAreaSymbol = SymbolUtils.CreateHatchFillSymbol(255, 0, 0);

			//selection symbols
			if (useProStyle)
			{
				var stroke = SymbolFactory.Instance.ConstructStroke(blue, 1, SimpleLineStyle.Dash);
				stroke.CapStyle = LineCapStyle.Square;
				stroke.JoinStyle = LineJoinStyle.Miter;
				stroke.MiterLimit = 4;

				_lineSymbol = SymbolUtils.CreateLineSymbol(stroke);

				_startPointSymbol =
					CreateHollowPointSymbol(green, 6, SimpleMarkerStyle.Circle, 1.5,
											SimpleLineStyle.Solid);

				_endPointSymbol =
					CreateHollowPointSymbol(red, 5, SimpleMarkerStyle.Square, 1.5,
											SimpleLineStyle.Solid);

				_vertexMarkerSymbol =
					CreateHollowPointSymbol(green, 5, SimpleMarkerStyle.Square, 1.5,
											SimpleLineStyle.Solid);

				_polygonSymbol = SymbolUtils.CreatePolygonSymbol(null, noFill, stroke);

				_controlPointMarkerSymbol = CreateControlPointSymbol(5, yellow, ColorUtils.BlackRGB, 1.5);
			}
			else
			{
				_lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(magenta, 0.8);

				_startPointSymbol =
					SymbolFactory.Instance.ConstructPointSymbol(
						green, 6.5, SimpleMarkerStyle.Circle);
				_endPointSymbol =
					SymbolFactory.Instance.ConstructPointSymbol(red, 6.5, SimpleMarkerStyle.Square);
				_vertexMarkerSymbol =
					SymbolFactory.Instance.ConstructPointSymbol(
						cyan, 6.5, SimpleMarkerStyle.Square);

				var stroke = SymbolUtils.CreateSolidStroke(magenta, 0.8);
				_polygonSymbol = SymbolUtils.CreatePolygonSymbol(null, noFill, stroke);

				_controlPointMarkerSymbol =
					CreateControlPointSymbol(6.5, yellow, ColorUtils.BlackRGB, 1.5);
			}
		}

		public void Clear()
		{
			_openJawReplacedEndPointOverlay?.Dispose();
			_openJawReplacedEndPointOverlay = null;
			_lastDrawnOpenJawPoint = null;

			_polygonPreviewOverlayAdd?.Dispose();
			_polygonPreviewOverlayAdd = null;

			_polygonPreviewOverlayRemove?.Dispose();
			_polygonPreviewOverlayRemove = null;
		}

		[CanBeNull]
		public static IDisposable AddOverlay([CanBeNull] Geometry geometry,
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

		public static CIMPointSymbol CreateHollowPointSymbol(
			int red, int green, int blue, double size, SimpleMarkerStyle markerStyle,
			double outlineWidth, SimpleLineStyle outLineStyle)
		{
			CIMColor color = ColorFactory.Instance.CreateRGBColor(red, green, blue);
			return CreateHollowPointSymbol(color, size, markerStyle, outlineWidth, outLineStyle);
		}

		public static CIMPointSymbol CreateHollowPointSymbol(
			CIMColor color, double size, SimpleMarkerStyle markerStyle, double outlineWidth,
			SimpleLineStyle outLineStyle)
		{
			CIMColor transparent = ColorFactory.Instance.CreateRGBColor(0d, 0d, 0d, 0d);

			CIMPointSymbol hollowSymbol =
				SymbolFactory.Instance.ConstructPointSymbol(transparent, size, markerStyle);

			var marker = hollowSymbol.SymbolLayers[0] as CIMVectorMarker;
			Assert.NotNull(marker).DominantSizeAxis3D = DominantSizeAxis.Z;
			marker.ScaleSymbolsProportionally = false;
			var polySymbol = marker.MarkerGraphics[0].Symbol as CIMPolygonSymbol;

			// Fill:
			Assert.NotNull(polySymbol).SymbolLayers[0] =
				SymbolFactory.Instance.ConstructSolidFill(transparent);

			//Outline:
			CIMStroke cimStroke =
				SymbolFactory.Instance.ConstructStroke(color, outlineWidth, outLineStyle);
			cimStroke.CapStyle = LineCapStyle.Square;
			cimStroke.JoinStyle = LineJoinStyle.Miter;
			cimStroke.MiterLimit = 4;
			polySymbol.SymbolLayers[1] = cimStroke;

			hollowSymbol.HaloSize = 0;

			return hollowSymbol;
		}

		public CIMPointSymbol CreateControlPointSymbol(double size, CIMColor fillColor,
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

		public static void CreateVertexMultipoint(Geometry geometry,
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
					(Multipoint)GeometryEngine.Instance.SimplifyAsFeature(multipoint, true);
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

		protected static void DisposeOverlays()
		{
			foreach (IDisposable overlay in _overlays)
			{
				overlay?.Dispose();
			}

			_overlays.Clear();
		}

	}
}
