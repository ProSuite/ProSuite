using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using GeometryType = ArcGIS.Core.Geometry.GeometryType;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

public class DestroyAndRebuildFeedback
{
	private IDisposable _pointOverlay;

	private IDisposable _lineOverlay;
	private IDisposable _startPointOverlay;
	private IDisposable _endPointOverlay;

	private IDisposable _polygonOverlay;

	private readonly CIMPointSymbol _pointSymbol;

	private readonly CIMLineSymbol _lineSymbol;
	private readonly CIMPointSymbol _startPointSymbol;
	private readonly CIMPointSymbol _endPointSymbol;

	private readonly CIMPolygonSymbol _polygonSymbol;

	public DestroyAndRebuildFeedback()
	{
		const SymbolUtils.FillStyle noFill = SymbolUtils.FillStyle.Null;
		var blue = ColorFactory.Instance.CreateRGBColor(0, 0, 200);
		var red = ColorFactory.Instance.CreateRGBColor(255, 0, 0);

		_pointSymbol = QueuedTask
		               .Run(() => SymbolFactory.Instance.ConstructPointSymbol(
			                    blue, 6, SimpleMarkerStyle.Circle)).Result;

		_lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(blue, 2.0);
		_startPointSymbol = QueuedTask
		                    .Run(() => SymbolFactory.Instance.ConstructPointSymbol(
			                         blue, 8, SimpleMarkerStyle.Circle)).Result;
		_endPointSymbol = QueuedTask
		                  .Run(() => SymbolFactory.Instance.ConstructPointSymbol(
			                       red, 12, SimpleMarkerStyle.Diamond)).Result;

		var stroke = SymbolUtils.CreateSolidStroke(blue, 2.0);
		_polygonSymbol = SymbolUtils.CreatePolygonSymbol(null, noFill, stroke);
	}

	public void UpdatePreview([CanBeNull] Geometry geometry)
	{
		Clear();

		if (geometry == null || geometry.IsEmpty)
		{
			return;
		}

		GeometryType geometryType = geometry.GeometryType;

		switch (geometryType)
		{
			case GeometryType.Point:
				_pointOverlay = AddOverlay(geometry, _pointSymbol);
				break;
			case GeometryType.Polyline:
				_lineOverlay = AddOverlay(geometry, _lineSymbol);

				var startPoint = GeometryUtils.GetStartPoint(geometry as Polyline);
				var endPoint = GeometryUtils.GetEndPoint(geometry as Polyline);
				_startPointOverlay = AddOverlay(startPoint, _startPointSymbol);
				_endPointOverlay = AddOverlay(endPoint, _endPointSymbol);
				break;
			case GeometryType.Polygon:
				_polygonOverlay = AddOverlay(geometry, _polygonSymbol);
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, null);
		}
	}

	public void Clear()
	{
		_pointOverlay?.Dispose();
		_pointOverlay = null;

		_lineOverlay?.Dispose();
		_lineOverlay = null;
		_startPointOverlay?.Dispose();
		_startPointOverlay = null;
		_endPointOverlay?.Dispose();
		_endPointOverlay = null;

		_polygonOverlay?.Dispose();
		_polygonOverlay = null;
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
}
