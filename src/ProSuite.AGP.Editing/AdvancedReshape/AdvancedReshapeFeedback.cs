using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

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

		public AdvancedReshapeFeedback(ReshapeToolOptions advancedReshapeToolOptions)
		{
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
		}

		public void UpdateOpenJawReplacedEndPoint([CanBeNull] MapPoint point)
		{
			_openJawReplacedEndPointOverlay?.Dispose();

			// Make openJawEndSymbol azure or celest blue, depending  on state of MoveOpenJawEndJunction
			_openJawEndSymbol = _advancedReshapeToolOptions.MoveOpenJawEndJunction
				                    ? CreateHollowCircle(0, 200, 255)
				                    : CreateHollowCircle(0, 0, 200);

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

		private static CIMPointSymbol CreateHollowCircle(int red, int green, int blue)
		{
			CIMColor transparent = ColorFactory.Instance.CreateRGBColor(0d, 0d, 0d, 0d);
			CIMColor color = ColorFactory.Instance.CreateRGBColor(red, green, blue);

			CIMPointSymbol hollowCircle =
				SymbolFactory.Instance.ConstructPointSymbol(transparent, 19,
				                                            SimpleMarkerStyle.Circle);

			var marker = hollowCircle.SymbolLayers[0] as CIMVectorMarker;
			var polySymbol = Assert.NotNull(marker).MarkerGraphics[0].Symbol as CIMPolygonSymbol;

			//Outline:
			Assert.NotNull(polySymbol).SymbolLayers[0] =
				SymbolFactory.Instance.ConstructStroke(color, 2, SimpleLineStyle.Solid);

			// Fill:
			polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(transparent);

			return hollowCircle;
		}
	}
}
