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
	[CanBeNull] private readonly CIMSymbolReference _symbolReference;
	[NotNull] private Geometry _geometry;

	// TODO: mutable overlay?
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

	[CanBeNull]
	public IDisposable DrawnOverlay { get; private set; }

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

	public void AddToMapEx(MapView mapView, bool useReferenceScale = false)
	{
		DrawnOverlay = useReferenceScale
			                 ? mapView.AddOverlay(_geometry, _symbolReference,
			                                      mapView.Map.ReferenceScale)
			                 : mapView.AddOverlay(_geometry, _symbolReference);
	}

	public void UpdateOverlay(MapView mapView)
	{
		Assert.NotNull(DrawnOverlay);

		UpdateOverlay(mapView, _geometry);
	}

	public void UpdateOverlay(MapView mapView, Geometry geometry)
	{
		Assert.NotNull(DrawnOverlay);

		_geometry = geometry;
		
		mapView.UpdateOverlay(DrawnOverlay, geometry, _symbolReference);
	}
}
