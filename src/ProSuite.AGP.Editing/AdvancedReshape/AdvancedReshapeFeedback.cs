using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedReshape;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public class AdvancedReshapeFeedback
	{
		private IDisposable _openJawReplacedEndPointOverlay;

		private IDisposable _polygonPreviewOverlayAdd;
		private IDisposable _polygonPreviewOverlayRemove;

		private readonly CIMPointSymbol _openJawReplaceEndSymbol;
		private readonly CIMPolygonSymbol _addAreaSymbol;
		private readonly CIMPolygonSymbol _removeAreaSymbol;

		public AdvancedReshapeFeedback()
		{
			_openJawReplaceEndSymbol = CreateHollowCircle(0, 0, 200);

			_addAreaSymbol = CreateHatchedAreaSymbol(0, 255, 0, 90);
			_removeAreaSymbol = CreateHatchedAreaSymbol(255, 0, 0);
		}

		public void UpdateOpenJawReplacedEndPoint(MapPoint point)
		{
			_openJawReplacedEndPointOverlay?.Dispose();

			if (point != null)
			{
				_openJawReplacedEndPointOverlay =
					MapView.Active.AddOverlay(
						point, _openJawReplaceEndSymbol.MakeSymbolReference());
			}
		}

		public Task<bool> UpdatePreview([NotNull] IList<ReshapeResultFeature> resultFeatures)
		{
			_polygonPreviewOverlayAdd?.Dispose();
			_polygonPreviewOverlayRemove?.Dispose();

			if (resultFeatures.Count == 0)
			{
				return Task.FromResult(false);
			}

			var addGeometries = new List<Geometry>(resultFeatures.Count);
			var removeGeometries = new List<Geometry>(resultFeatures.Count);

			foreach (ReshapeResultFeature resultFeature in resultFeatures)
			{
				var sourcePoly = resultFeature.Feature.GetShape() as Polygon;

				if (sourcePoly == null || sourcePoly.IsEmpty)
				{
					continue;
				}

				var reshapedPoly = (Polygon) resultFeature.UpdatedGeometry;

				addGeometries.Add(GeometryEngine.Instance.Difference(reshapedPoly, sourcePoly));
				removeGeometries.Add(GeometryEngine.Instance.Difference(sourcePoly, reshapedPoly));
			}

			Polygon polygonAddArea = GeometryEngine.Instance.Union(addGeometries) as Polygon;
			Polygon polygonRemoveArea = GeometryEngine.Instance.Union(removeGeometries) as Polygon;

			_polygonPreviewOverlayAdd = AddOverlay(polygonAddArea, _addAreaSymbol);
			_polygonPreviewOverlayRemove = AddOverlay(polygonRemoveArea, _removeAreaSymbol);

			return Task.FromResult(true);
		}

		public void DisposeOverlays()
		{
			_openJawReplacedEndPointOverlay?.Dispose();
			_openJawReplacedEndPointOverlay = null;

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

		private static CIMPolygonSymbol CreateHatchedAreaSymbol(double r, double g, double b,
		                                                        double rotation = 0)
		{
			CIMColor color = ColorFactory.Instance.CreateRGBColor(r, g, b);

			var hatchLineSymbol = SymbolFactory.Instance.ConstructLineSymbol(color, 2);

			var hatchFill = new CIMHatchFill
			                {
				                Enable = true,
				                Rotation = rotation,
				                Separation = 5,
				                LineSymbol = hatchLineSymbol
			                };

			var symbolLayers = new List<CIMSymbolLayer>();

			symbolLayers.AddRange(hatchLineSymbol.SymbolLayers);
			symbolLayers.Add(hatchFill);

			return new CIMPolygonSymbol {SymbolLayers = symbolLayers.ToArray()};
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
