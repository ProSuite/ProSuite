using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public class AdvancedReshapeFeedback
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull] private readonly ISymbolizedSketchTool _tool;
		private IDisposable _openJawReplacedEndPointOverlay;

		private IDisposable _polygonPreviewOverlayAdd;
		private IDisposable _polygonPreviewOverlayRemove;

		private readonly CIMPointSymbol _openJawReplaceEndSymbol;
		private readonly CIMPolygonSymbol _addAreaSymbol;
		private readonly CIMPolygonSymbol _removeAreaSymbol;

		public AdvancedReshapeFeedback()
		{
			_openJawReplaceEndSymbol = CreateHollowCircle(0, 200, 255);

			_addAreaSymbol = SymbolUtils.CreateHatchFillSymbol(0, 255, 0, 90);
			_removeAreaSymbol = SymbolUtils.CreateHatchFillSymbol(255, 0, 0);
		}

		public AdvancedReshapeFeedback(ISymbolizedSketchTool tool) : this()
		{
			_tool = tool;
		}

		public void UpdateOpenJawReplacedEndPoint([CanBeNull] MapPoint point)
		{
			_openJawReplacedEndPointOverlay?.Dispose();

			if (point != null)
			{
				_openJawReplacedEndPointOverlay =
					MapView.Active.AddOverlay(
						point, _openJawReplaceEndSymbol.MakeSymbolReference());
			}
		}

		public Task<bool> UpdatePreview([CanBeNull] IList<ResultFeature> resultFeatures)
		{
			_polygonPreviewOverlayAdd?.Dispose();
			_polygonPreviewOverlayRemove?.Dispose();

			if (resultFeatures == null || resultFeatures.Count == 0)
			{
				return Task.FromResult(false);
			}

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

			return Task.FromResult(true);
		}

		public void Clear(bool clearSketchSymbol = true)
		{
			if (clearSketchSymbol)
			{
				ClearSketchSymbol();
			}

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

		public bool CanSetSketchSymbol(FeatureLayer layer)
		{
			return GeometryUtils.TranslateEsriGeometryType(layer.ShapeType) ==
			       GeometryType.Polyline;
		}

		public void SetSketchSymbol([NotNull] FeatureLayer layer, [CanBeNull] Feature feature)
		{
			Assert.NotNull(_tool, $"Ensure {nameof(ISymbolizedSketchTool)} is not null.");

			if (! CanSetSketchSymbol(layer))
			{
				return;
			}

			if (feature == null)
			{
				ClearSketchSymbol();
				return;
			}

			CIMSymbol symbol = layer.LookupSymbol(feature.GetObjectID(), _tool.ActiveMapView);

			if (symbol == null)
			{
				_msg.Debug(
					$"Cannot set sketch symbol: no symbol found for {GdbObjectUtils.GetDisplayValue(feature)}");
			}
			else
			{
				_tool.SetSketchSymbol(symbol.MakeSymbolReference());
			}
		}

		public void ClearSketchSymbol()
		{
			_tool?.SetSketchSymbol(null);
		}
	}
}
