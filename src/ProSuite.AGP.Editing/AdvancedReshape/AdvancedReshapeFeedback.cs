using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public class AdvancedReshapeFeedback
	{
		private IDisposable _polygonPreviewOverlayAdd;
		private IDisposable _polygonPreviewOverlayRemove;

		private readonly CIMPolygonSymbol _addAreaSymbol;
		private readonly CIMPolygonSymbol _removeAreaSymbol;

		public AdvancedReshapeFeedback()
		{
			_addAreaSymbol = SymbolUtils.CreateHatchFillSymbol(0, 255, 0, 90);
			_removeAreaSymbol = SymbolUtils.CreateHatchFillSymbol(255, 0, 0);
		}

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public virtual Task<bool> UpdatePreview([CanBeNull] IList<ResultFeature> resultFeatures)
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

		public void Clear()
		{
			_polygonPreviewOverlayAdd?.Dispose();
			_polygonPreviewOverlayAdd = null;

			_polygonPreviewOverlayRemove?.Dispose();
			_polygonPreviewOverlayRemove = null;
		}

		[CanBeNull]
		protected static IDisposable AddOverlay([CanBeNull] Geometry geometry,
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
	}
}
