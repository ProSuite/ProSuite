using System;
using System.Collections.Generic;
using System.Diagnostics;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Generalize;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Generalize
{
	public class GeneralizeFeedback
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static CIMLineSymbol _redSegmentSymbol;
		private static CIMSymbolReference _greenPointMarker;
		private static CIMSymbolReference _outlinedPointSymRef;
		private static CIMSymbolReference _redCrossMarker;
		private static CIMSymbolReference _greySquareMarker;

		private readonly List<IDisposable> _overlays = new();

		// Create a point symbol with an outline

		public GeneralizeFeedback()
		{
			CIMColor white = ColorUtils.CreateRGB(255, 255, 255);
			CIMColor red = ColorUtils.CreateRGB(255, 0, 0);
			CIMColor green = ColorUtils.CreateRGB(0, 200, 0);
			CIMColor grey = ColorUtils.CreateRGB(100, 100, 100);

			_redSegmentSymbol = SymbolUtils.CreateLineSymbol(red, 5);
			_greenPointMarker =
				CreateOutlinedPointSymbol(green, white, 5, SymbolUtils.MarkerStyle.Circle);
			_redCrossMarker =
				CreateOutlinedPointSymbol(white, red, 7, SymbolUtils.MarkerStyle.Cross);
			_greySquareMarker =
				CreateOutlinedPointSymbol(grey, white, 3, SymbolUtils.MarkerStyle.Square);
		}

		public void Update([CanBeNull] GeneralizeResult generalizeResult,
		                   [NotNull] IEnumerable<Feature> selectedFeatures)
		{
			// clear any previous drawings
			DisposeOverlays();

			// get all vertices of selected features
			foreach (var feature in selectedFeatures)
			{
				Geometry geometry = feature.GetShape();

				if (geometry is Multipart multipart)
				{
					Multipoint multipoint = MultipointBuilderEx.CreateMultipoint(multipart);

					IDisposable addedVertex =
						MapView.Active.AddOverlay(multipoint, _greySquareMarker);
					_overlays.Add(addedVertex);
				}
				else
				{
					IEnumerable<MapPoint> vertices = GeometryUtils.GetVertices(feature);

					// draw vertices before drawing the rest
					foreach (var vertex in vertices)
					{
						IDisposable addedVertex =
							MapView.Active.AddOverlay(vertex, _greySquareMarker);
						_overlays.Add(addedVertex);
					}
				}
			}

			if (generalizeResult == null)
			{
				return;
			}

			Stopwatch stopwatch = _msg.DebugStartTiming();

			// draw generalization infos
			IList<GeneralizedFeature> generalizedFeatures = generalizeResult.ResultsByFeature;

			foreach (GeneralizedFeature generalizedFeature in generalizedFeatures)
			{
				if (generalizedFeature.DeletablePoints != null)
				{
					IDisposable addedDeletePoints =
						MapView.Active.AddOverlay(generalizedFeature.DeletablePoints,
						                          _redCrossMarker);

					_overlays.Add(addedDeletePoints);
				}

				if (generalizedFeature.ProtectedPoints != null)
				{
					foreach (MapPoint point in generalizedFeature.ProtectedPoints.Points)
					{
						IDisposable addedProtectedPoint =
							MapView.Active.AddOverlay(point, _greenPointMarker);
						_overlays.Add(addedProtectedPoint);
					}
				}

				foreach (SegmentInfo segmentInfo in generalizedFeature.RemovableSegments)
				{
					Segment segment = segmentInfo.Segment;

					Polyline highLevelSegment =
						PolylineBuilderEx.CreatePolyline(segment, segment.SpatialReference);

					IDisposable addedSegment =
						MapView.Active.AddOverlay(highLevelSegment,
						                          _redSegmentSymbol.MakeSymbolReference());

					_overlays.Add(addedSegment);
				}
			}

			_msg.DebugStopTiming(stopwatch, "Added {0} overlays for {1} features.", _overlays.Count,
			                     generalizedFeatures.Count);
		}

		public void DisposeOverlays()
		{
			foreach (IDisposable overlay in _overlays)
			{
				overlay.Dispose();
			}

			_overlays.Clear();
		}

		private static CIMSymbolReference CreateOutlinedPointSymbol(
			CIMColor fillColor,
			CIMColor strokeColor,
			double size,
			SymbolUtils.MarkerStyle style)
		{
			var stroke = SymbolUtils.CreateSolidStroke(strokeColor, size / 2);
			var polySym =
				SymbolUtils.CreatePolygonSymbol(fillColor, SymbolUtils.FillStyle.Solid, stroke);
			var marker = SymbolUtils.CreateMarker(style, polySym, size);
			var symbol = SymbolUtils.CreatePointSymbol(marker);
			_outlinedPointSymRef = symbol.MakeSymbolReference();

			return _outlinedPointSymRef;
		}
	}
}
