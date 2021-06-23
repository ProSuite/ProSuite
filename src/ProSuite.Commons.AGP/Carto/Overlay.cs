using System;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;

namespace ProSuite.Commons.AGP.Carto
{
	public class Overlay
	{
		private readonly ArcGIS.Core.Geometry.Geometry _geometry;
		private readonly CIMSymbolReference _symbolReference;

		public Overlay(ArcGIS.Core.Geometry.Geometry geometry, CIMSymbolReference symbolReference)
		{
			_geometry = geometry;
			_symbolReference = symbolReference;
		}

		public Overlay(ArcGIS.Core.Geometry.Geometry geometry, CIMSymbol symbol)
		{
			_geometry = geometry;
			_symbolReference = symbol.MakeSymbolReference();
		}

		public async Task<IDisposable> AddToMapAsync(MapView mapView)
		{
			return await mapView.AddOverlayAsync(_geometry, _symbolReference);
		}

		public IDisposable AddToMap(MapView mapView)
		{
			return mapView.AddOverlay(_geometry, _symbolReference);
		}
	}
}
