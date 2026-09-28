using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using Geometry = ArcGIS.Core.Geometry.Geometry;

namespace ProSuite.AGP.Editing.AdvancedReshape;

public class AdvancedReshapeFeedback : FeedbackBase
{
	private CIMPointSymbol _openJawEndSymbol;
	private readonly ReshapeToolOptions _advancedReshapeToolOptions;


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
	}

	public void UpdateOpenJawReplacedEndPoint([CanBeNull] MapPoint point)
	{
		_openJawReplacedEndPointOverlay?.Dispose();

		// Make openJawEndSymbol azure or celest blue, depending  on state of MoveOpenJawEndJunction
		_openJawEndSymbol = _advancedReshapeToolOptions.MoveOpenJawEndJunction
			                    ? CreateHollowPointSymbol(
				                    0, 200, 255, 19, SimpleMarkerStyle.Circle, 2,
				                    SimpleLineStyle.Solid)
			                    : CreateHollowPointSymbol(
				                    0, 0, 200, 19, SimpleMarkerStyle.Circle, 2,
				                    SimpleLineStyle.Solid);

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
				removeGeometries.Add(
					GeometryEngine.Instance.Difference(sourcePoly, reshapedPoly));
			}

			Polygon polygonAddArea = GeometryEngine.Instance.Union(addGeometries) as Polygon;
			Polygon polygonRemoveArea =
				GeometryEngine.Instance.Union(removeGeometries) as Polygon;

			_polygonPreviewOverlayAdd =
				AddOverlay(polygonAddArea, Assert.NotNull(_addAreaSymbol));
			_polygonPreviewOverlayRemove =
				AddOverlay(polygonRemoveArea, Assert.NotNull(_removeAreaSymbol));
		}

		return Task.FromResult(true);
	}

	public bool UpdateSelection([CanBeNull] IList<Feature> selectedFeatures)
	{
		DisposeOverlays();

		if (selectedFeatures == null || selectedFeatures.Count == 0)
		{
			return false;
		}

		foreach (Feature selectedFeature in selectedFeatures)
		{
			Geometry geometry = selectedFeature.GetShape();
			GeometryType geometryType = geometry.GeometryType;

			Multipoint vertexMultipoint;
			Multipoint controlMultipoint;

			switch (geometryType)
			{
				case GeometryType.Point:
					//_pointOverlay = AddOverlay(geometry, _pointSymbol);
					break;
				case GeometryType.Polyline:
					_overlays.Add(AddOverlay(geometry, Assert.NotNull(_lineSymbol)));

					var startPointL = GeometryUtils.GetStartPoint(geometry as Polyline);
					var endPointL = GeometryUtils.GetEndPoint(geometry as Polyline);
					_overlays.Add(AddOverlay(startPointL, Assert.NotNull(_startPointSymbol)));

					CreateVertexMultipoint(geometry, out vertexMultipoint,
					                       out controlMultipoint);
					_overlays.Add(AddOverlay(vertexMultipoint,
					                         Assert.NotNull(_vertexMarkerSymbol)));
					_overlays.Add(AddOverlay(controlMultipoint,
					                         Assert.NotNull(_controlPointMarkerSymbol)));

					_overlays.Add(AddOverlay(endPointL, Assert.NotNull(_endPointSymbol)));
					break;
				case GeometryType.Polygon:
					_overlays.Add(AddOverlay(geometry, Assert.NotNull(_polygonSymbol)));

					var startPointP = GeometryUtils.GetStartPoint(geometry as Polygon);
					var endPointP = GeometryUtils.GetEndPoint(geometry as Polygon);
					_overlays.Add(AddOverlay(startPointP, Assert.NotNull(_startPointSymbol)));

					CreateVertexMultipoint(geometry, out vertexMultipoint,
					                       out controlMultipoint);
					_overlays.Add(AddOverlay(vertexMultipoint,
					                         Assert.NotNull(_vertexMarkerSymbol)));
					_overlays.Add(
						AddOverlay(controlMultipoint,
						           Assert.NotNull(_controlPointMarkerSymbol)));

					_overlays.Add(AddOverlay(endPointP, Assert.NotNull(_endPointSymbol)));
					break;

				default:
					throw new ArgumentOutOfRangeException(
						nameof(geometryType), geometryType, null);
			}
		}

		return true;
	}
}
