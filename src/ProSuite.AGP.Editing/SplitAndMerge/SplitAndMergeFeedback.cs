using ArcGIS.Core.CIM;
using ProSuite.AGP.Editing.AdvancedReshape;
using ProSuite.Commons.Essentials.CodeAnnotations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;

namespace ProSuite.AGP.Editing.SplitAndMerge
{
	public class SplitAndMergeFeedback : FeedbackBase
	{

		public SplitAndMergeFeedback()
		{

		}

		public Task<bool> UpdatePreview([CanBeNull] Geometry geometry)
		{
			DisposeOverlays();

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
