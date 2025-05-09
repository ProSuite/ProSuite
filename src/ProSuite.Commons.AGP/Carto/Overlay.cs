using System;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.Commons.AGP.Carto
{
	public class Overlay
	{
		private readonly Geometry _geometry;
		private readonly CIMSymbolReference _symbolReference;

		public Overlay(Geometry geometry, CIMSymbolReference symbolReference)
		{
			_geometry = geometry;
			_symbolReference = symbolReference;
		}

		public Overlay(Geometry geometry, CIMSymbol symbol)
		{
			_geometry = geometry;
			_symbolReference = symbol.MakeSymbolReference();
		}

		public async Task<IDisposable> AddToMapAsync(MapView mapView, bool useReferenceScale = false)
		{
			return useReferenceScale
				       ? await mapView.AddOverlayAsync(_geometry, _symbolReference, mapView.Map.ReferenceScale)
				       : await mapView.AddOverlayAsync(_geometry, _symbolReference);
		}

		public IDisposable AddToMap(MapView mapView, bool useReferenceScale = false)
		{
			return useReferenceScale
				       ? mapView.AddOverlay(_geometry, _symbolReference, mapView.Map.ReferenceScale)
				       : mapView.AddOverlay(_geometry, _symbolReference);
		}
	}
}
