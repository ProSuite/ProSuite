using System;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto;

public class Overlay
{
	[NotNull] private readonly Geometry _geometry;
	[CanBeNull] private readonly CIMSymbolReference _symbolReference;

	public Overlay([NotNull] Geometry geometry,
	               [CanBeNull] CIMSymbolReference symbolReference = null)
	{
		Assert.ArgumentNotNull(geometry, nameof(geometry));
		Assert.ArgumentCondition(geometry is not GeometryBag,
		                         "GeometryBag geometry type is not supported for overlays");

		_geometry = geometry;
		_symbolReference = symbolReference;
	}

	public Overlay([NotNull] Geometry geometry,
	               [CanBeNull] CIMSymbol symbol)
		: this(geometry, symbol?.MakeSymbolReference()) { }

	public async Task<IDisposable> AddToMapAsync(MapView mapView,
	                                             bool useReferenceScale = false)
	{
		return useReferenceScale
			       ? await mapView.AddOverlayAsync(_geometry, _symbolReference,
			                                       mapView.Map.ReferenceScale)
			       : await mapView.AddOverlayAsync(_geometry, _symbolReference);
	}

	public IDisposable AddToMap(MapView mapView, bool useReferenceScale = false)
	{
		return useReferenceScale
			       ? mapView.AddOverlay(_geometry, _symbolReference, mapView.Map.ReferenceScale)
			       : mapView.AddOverlay(_geometry, _symbolReference);
	}
}
