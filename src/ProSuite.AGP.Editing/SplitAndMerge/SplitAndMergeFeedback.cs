using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProSuite.AGP.Editing.SplitAndMerge
{
	public class SplitAndMergeFeedback : FeedbackBase
	{

		private CIMPolygonSymbol _polygonFeatureSymbol;
		private CIMLineSymbol _lineFeatureSymbol;

		public SplitAndMergeFeedback()
		{
			const SymbolUtils.FillStyle noFill = SymbolUtils.FillStyle.Null;

			var color = ColorFactory.Instance.CreateRGBColor(0, 255, 255);


			_lineFeatureSymbol = SymbolFactory.Instance.ConstructLineSymbol(color, 0.8);

			var stroke = SymbolUtils.CreateSolidStroke(color, 0.8);
			_polygonFeatureSymbol = SymbolUtils.CreatePolygonSymbol(null, noFill, stroke);
		}

		public Task<bool> UpdatePreviewFeatureToBeUpdated([CanBeNull] Feature feature)
		{
			if (feature == null)
			{
				return Task.FromResult(false);
			}

			Geometry geometry = feature.GetShape();

			return AddOverlayGeometry(geometry, _lineFeatureSymbol, _polygonFeatureSymbol);
		}

		public Task<bool> UpdatePreview([CanBeNull] Geometry geometry)
		{
			DisposeOverlays();

			return AddOverlayGeometry(geometry, _lineSymbol, _polygonSymbol);
		}

		private Task<bool> AddOverlayGeometry(Geometry geometry, CIMLineSymbol lineSymbol, CIMPolygonSymbol polygonSymbol)
		{
			if (geometry == null)
			{
				return Task.FromResult(false);
			}

			GeometryType geometryType = geometry.GeometryType;

			Multipoint vertexMultipoint;
			Multipoint controlMultipoint;

			switch (geometryType)
			{
				case GeometryType.Unknown:
					break;
				case GeometryType.Point:
					break;
				case GeometryType.Envelope:
					break;
				case GeometryType.Multipoint:
					break;
				case GeometryType.Polyline:
					_overlays.Add(AddOverlay(geometry, Assert.NotNull(lineSymbol)));

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
					_overlays.Add(AddOverlay(geometry, Assert.NotNull(polygonSymbol)));

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
				case GeometryType.Multipatch:
					break;
				case GeometryType.GeometryBag:
					break;
				default:
					throw new ArgumentOutOfRangeException(
						nameof(geometryType), geometryType, null);
			}

			return Task.FromResult(true);
		}
	}
}
