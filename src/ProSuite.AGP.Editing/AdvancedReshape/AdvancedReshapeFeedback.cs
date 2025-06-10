using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public class AdvancedReshapeFeedback
	{
		private IDisposable _openJawReplacedEndPointOverlay;

		private IDisposable _polygonPreviewOverlayAdd;
		private IDisposable _polygonPreviewOverlayRemove;

		private CIMPointSymbol _openJawEndSymbol;
		private readonly CIMPolygonSymbol _addAreaSymbol;
		private readonly CIMPolygonSymbol _removeAreaSymbol;
		private readonly ReshapeToolOptions _advancedReshapeToolOptions;

		private MapPoint _lastDrawnOpenJawPoint;

		private static List<IDisposable> _selectedGeometries = new List<IDisposable>();

		private readonly CIMPolygonSymbol _polygonSymbol;
		private readonly CIMLineSymbol _lineSymbol;
		private readonly CIMPointSymbol _startPointSymbol;
		private readonly CIMPointSymbol _endPointSymbol;
		private readonly CIMPointSymbol _vertexMarkerSymbol;


		public AdvancedReshapeFeedback(ReshapeToolOptions advancedReshapeToolOptions)
		{
			const SymbolUtils.FillStyle noFill = SymbolUtils.FillStyle.Null;
			var blue = ColorFactory.Instance.CreateRGBColor(0, 0, 200);
			var red = ColorFactory.Instance.CreateRGBColor(255, 0, 0);
			var cyan = ColorFactory.Instance.CreateRGBColor(0, 255, 255);
			var green = ColorFactory.Instance.CreateRGBColor(0, 128, 0);

			_advancedReshapeToolOptions = advancedReshapeToolOptions;
			_advancedReshapeToolOptions.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == nameof(ReshapeToolOptions.MoveOpenJawEndJunction))
				{
					QueuedTask.Run(() => UpdateOpenJawReplacedEndPoint(_lastDrawnOpenJawPoint));
				}

				if (args.PropertyName == nameof(ReshapeToolOptions.ShowPreview))
				{
					QueuedTask.Run(() => UpdatePreview(null));
				}
			};

			_addAreaSymbol = SymbolUtils.CreateHatchFillSymbol(0, 255, 0, 90);
			_removeAreaSymbol = SymbolUtils.CreateHatchFillSymbol(255, 0, 0);

			//selection symbols
			_lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(blue, 2.0);
			_startPointSymbol = QueuedTask
			                    .Run(() => SymbolFactory.Instance.ConstructPointSymbol(
				                         green, 5, SimpleMarkerStyle.Circle)).Result;
			_endPointSymbol = QueuedTask
			                  .Run(() => SymbolFactory.Instance.ConstructPointSymbol(
				                       red, 5, SimpleMarkerStyle.Square)).Result;//Diamond

			_vertexMarkerSymbol = QueuedTask
			                      .Run(() => SymbolFactory.Instance.ConstructPointSymbol(
				                           cyan, 5, SimpleMarkerStyle.Square)).Result;

			var stroke = SymbolUtils.CreateSolidStroke(blue, 2.0);
			_polygonSymbol = SymbolUtils.CreatePolygonSymbol(null, noFill, stroke);

		}

		public void UpdateOpenJawReplacedEndPoint([CanBeNull] MapPoint point)
		{
			_openJawReplacedEndPointOverlay?.Dispose();

			// Make openJawEndSymbol azure or celest blue, depending  on state of MoveOpenJawEndJunction
			_openJawEndSymbol = _advancedReshapeToolOptions.MoveOpenJawEndJunction
				                    ? CreateHollowPointSymbol(0, 200, 255, SimpleMarkerStyle.Circle, 19)
				                    : CreateHollowPointSymbol(0, 0, 200, SimpleMarkerStyle.Circle, 19);

			if (point != null)
			{
				_openJawReplacedEndPointOverlay =
					MapView.Active.AddOverlay(
						point, _openJawEndSymbol.MakeSymbolReference());
			}

			_lastDrawnOpenJawPoint = point;
		}

		public Task<bool> UpdatePreview([CanBeNull] IList<ResultFeature> resultFeatures)
		{
			
			_polygonPreviewOverlayAdd?.Dispose();
			_polygonPreviewOverlayRemove?.Dispose();

			if (resultFeatures == null || resultFeatures.Count == 0)
			{
				return Task.FromResult(false);
			}

			if (_advancedReshapeToolOptions.ShowPreview)
			{
				var addGeometries = new List<Geometry>(resultFeatures.Count);
				var removeGeometries = new List<Geometry>(resultFeatures.Count);

				foreach (ResultFeature resultFeature in resultFeatures)
				{
					var sourcePoly = resultFeature.OriginalFeature.GetShape() as Polygon;

					if (sourcePoly == null || sourcePoly.IsEmpty)
					{
						continue;
					}

					var reshapedPoly = (Polygon) resultFeature.NewGeometry;

					addGeometries.Add(GeometryEngine.Instance.Difference(reshapedPoly, sourcePoly));
					removeGeometries.Add(GeometryEngine.Instance.Difference(sourcePoly, reshapedPoly));
				}

				Polygon polygonAddArea = GeometryEngine.Instance.Union(addGeometries) as Polygon;
				Polygon polygonRemoveArea = GeometryEngine.Instance.Union(removeGeometries) as Polygon;

				_polygonPreviewOverlayAdd = AddOverlay(polygonAddArea, _addAreaSymbol);
				_polygonPreviewOverlayRemove = AddOverlay(polygonRemoveArea, _removeAreaSymbol);
			}

			return Task.FromResult(true);
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

		private static CIMPointSymbol CreateHollowPointSymbol(int red, int green, int blue, SimpleMarkerStyle markerStyle, double size)
		{
			CIMColor transparent = ColorFactory.Instance.CreateRGBColor(0d, 0d, 0d, 0d);
			CIMColor color = ColorFactory.Instance.CreateRGBColor(red, green, blue);

			CIMPointSymbol hollowSymbol =
				SymbolFactory.Instance.ConstructPointSymbol(transparent, size, markerStyle);

			var marker = hollowSymbol.SymbolLayers[0] as CIMVectorMarker;
			var polySymbol = Assert.NotNull(marker).MarkerGraphics[0].Symbol as CIMPolygonSymbol;

			//Outline:
			Assert.NotNull(polySymbol).SymbolLayers[0] =
				SymbolFactory.Instance.ConstructStroke(color, 2, SimpleLineStyle.Solid);

			// Fill:
			polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(transparent);

			return hollowSymbol;
		}

		#region Selection

		public Task<bool> UpdateSelection([CanBeNull] IList<Feature> selectedFeatures)
		{
			DisposeSelectedGeometriesOverlays();

			if (selectedFeatures == null || selectedFeatures.Count == 0)
			{
				return Task.FromResult(false);
			}

			foreach (Feature selectedFeature in selectedFeatures)
			{
				Geometry geometry = selectedFeature.GetShape();
				GeometryType geometryType = geometry.GeometryType;

				switch (geometryType)
				{
					case GeometryType.Point:
						//_pointOverlay = AddOverlay(geometry, _pointSymbol);
						break;
					case GeometryType.Polyline:
						_selectedGeometries.Add(AddOverlay(geometry, _lineSymbol));

						var startPoint = GeometryUtils.GetStartPoint(geometry as Polyline);
						var endPoint = GeometryUtils.GetEndPoint(geometry as Polyline);
						_selectedGeometries.Add(AddOverlay(startPoint, _startPointSymbol));

						//ReadOnlyPointCollection points = ((Multipart)geometry).Points;

						IList<MapPoint> controlPoints = GetControlPoints(geometry as Multipart);
						foreach (MapPoint controlPoint in controlPoints)
						{
							_selectedGeometries.Add(AddOverlay(controlPoint, _vertexMarkerSymbol));
						}
						_selectedGeometries.Add(AddOverlay(endPoint, _endPointSymbol));
						break;
					case GeometryType.Polygon:
						_selectedGeometries.Add(AddOverlay(geometry, _polygonSymbol));

						var startPoint2 = GeometryUtils.GetStartPoint(geometry as Polygon);
						var endPoint2 = GeometryUtils.GetEndPoint(geometry as Polygon);
						_selectedGeometries.Add(AddOverlay(startPoint2, _startPointSymbol));

						IList<MapPoint> controlPoints2 = GetControlPoints(geometry as Multipart);
						foreach (MapPoint controlPoint in controlPoints2)
						{
							_selectedGeometries.Add(AddOverlay(controlPoint, _vertexMarkerSymbol));
						}
						_selectedGeometries.Add(AddOverlay(endPoint2, _endPointSymbol));
						break;

					default:
						throw new ArgumentOutOfRangeException(
							nameof(geometryType), geometryType, null);
				}

			}

			return Task.FromResult(true);
		}

		public void ClearSelection()
		{
			DisposeSelectedGeometriesOverlays();
		}

		public static void DisposeSelectedGeometriesOverlays()
		{
			foreach (IDisposable overlay in _selectedGeometries)
			{
				overlay.Dispose();
			}
			_selectedGeometries.Clear();
		}


		public static IList<MapPoint> GetControlPoints(
			Multipart geometry, int partIndex = -1)
		{
			IList<MapPoint> controlPoints = new List<MapPoint>();
			if (geometry != null)
			{
				var builder = geometry.ToBuilder() as MultipartBuilderEx;

				if (builder is null)
					throw new ArgumentNullException(nameof(builder));

				var startPoint = GeometryUtils.GetStartPoint(geometry);
				var endPoint = GeometryUtils.GetEndPoint(geometry);

				int count = 0;

				int partCount = builder.PartCount;
				for (int k = 0; k < partCount; k++)
				{
					if (partIndex >= 0 && k != partIndex) continue;

					int pointCount = builder.GetPointCount(k);
					for (int j = 0; j < pointCount; j++)
					{
						var point = builder.GetPoint(k, j);
						bool isSameStartPointXY = IsSamePointXY(point, startPoint, Double.NaN);
						bool isSameEndPointXY = IsSamePointXY(point, endPoint, Double.NaN);
						if (! isSameStartPointXY && ! isSameEndPointXY)
						{
							controlPoints.Add(point);
						}
					}
				}
			}

			return controlPoints;
		}

		private static bool IsSamePointXY(MapPoint a, MapPoint b, double tolerance)
		{
			//ProcessingUtils.IsSamePointXY(a, b, tolerance);

			if (ReferenceEquals(a, b))
			{
				return true;
			}

			if (double.IsNaN(tolerance))
			{
				double xyToleranceA = GeometryUtils.GetXyTolerance(a);
				double xyToleranceB = GeometryUtils.GetXyTolerance(b);
				tolerance = Math.Max(xyToleranceA, xyToleranceB);
				if (double.IsNaN(tolerance)) tolerance = 0.0;
			}

			double dx = a.X - b.X;
			double dy = a.Y - b.Y;
			double dd = dx * dx + dy * dy;

			double toleranceSquared = tolerance * tolerance;
			return dd <= toleranceSquared;
		}

		#endregion Selection

	}
}
