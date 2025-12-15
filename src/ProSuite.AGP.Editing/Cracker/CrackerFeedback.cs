using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using CrackPoint = ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker.CrackPoint;

namespace ProSuite.AGP.Editing.Cracker
{
	public class CrackerFeedback
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static CIMSymbolReference _outlinedPointSymRef;

		private readonly CIMSymbolReference redCircleMarker;

		private readonly CIMSymbolReference greenCircleMarker;

		private readonly CIMSymbolReference greenSquareMarker;

		private readonly CIMSymbolReference redCrossMarker;

		private readonly CIMSymbolReference redSquareMarker;

		private readonly CIMSymbolReference mintCircleMarker;

		private readonly CIMSymbolReference greySquareMarker;

		private IDisposable _extentOverlay;

		private readonly List<IDisposable> _overlays = new();

		// Create a point symbol with an outline

		private static CIMSymbolReference CreateOutlinedPointSymbol(
			CIMColor fillColor, CIMColor strokeColor, double size, SymbolUtils.MarkerStyle style)
		{
			var stroke = SymbolUtils.CreateSolidStroke(strokeColor, size / 2);

			var polySym =
				SymbolUtils.CreatePolygonSymbol(fillColor, SymbolUtils.FillStyle.Solid, stroke);

			var marker = SymbolUtils.CreateMarker(style, polySym, size);

			var symbol = SymbolUtils.CreatePointSymbol(marker);

			_outlinedPointSymRef = symbol.MakeSymbolReference();

			return _outlinedPointSymRef;
		}

		public CrackerFeedback()
		{
			CIMColor red = ColorUtils.CreateRGB(255, 0, 0);

			CIMColor green = ColorUtils.CreateRGB(0, 200, 0);

			CIMColor mint = ColorUtils.CreateRGB(0, 255, 150);

			CIMColor grey = ColorUtils.CreateRGB(100, 100, 100);

			CIMColor white = ColorUtils.CreateRGB(255, 255, 255);

			redCircleMarker =
				CreateOutlinedPointSymbol(red, white, 5, SymbolUtils.MarkerStyle.Circle);

			greenCircleMarker =
				CreateOutlinedPointSymbol(green, white, 5, SymbolUtils.MarkerStyle.Circle);

			greenSquareMarker =
				CreateOutlinedPointSymbol(green, white, 5, SymbolUtils.MarkerStyle.Square);

			mintCircleMarker =
				CreateOutlinedPointSymbol(mint, white, 5, SymbolUtils.MarkerStyle.Circle);

			redCrossMarker =
				CreateOutlinedPointSymbol(white, red, 7, SymbolUtils.MarkerStyle.Cross);

			greySquareMarker =
				CreateOutlinedPointSymbol(grey, white, 3, SymbolUtils.MarkerStyle.Square);

			redSquareMarker =
				CreateOutlinedPointSymbol(red, white, 3, SymbolUtils.MarkerStyle.Square);

			//TODO: remove segment line feature
		}

		public void Update([CanBeNull] CrackerResult crackerResult, IList<Feature> selectedFeatures)
		{
			// clear any previous drawings

			DisposeOverlays();

			// get all vertices of selected features

			foreach (var feature in selectedFeatures)
			{
				IEnumerable<MapPoint> vertices = GeometryUtils.GetVertices(feature.GetShape());

				// draw vertices before drawing crack points

				foreach (var vertex in vertices)

				{
					IDisposable addedVertex =
						MapView.Active.AddOverlay(vertex, greySquareMarker);

					_overlays.Add(addedVertex);
				}
			}

			if (crackerResult == null)
			{
				return;
			}

			// draw crackpoints

			foreach (var crackedFeature in crackerResult.ResultsByFeature)
			{
				foreach (CrackPoint crackPoint in crackedFeature.CrackPoints)
				{
					if (crackPoint.ViolatesMinimumSegmentLength)
					{
						IDisposable addedCrackPoint =
							MapView.Active.AddOverlay(crackPoint.Point, redCircleMarker);

						_overlays.Add(addedCrackPoint);
					}
					else if (crackPoint
					         .TargetVertexOnlyDifferentInZ) //not implemented in server yet
					{
						IDisposable addedCrackPoint =
							MapView.Active.AddOverlay(crackPoint.Point, mintCircleMarker);

						_overlays.Add(addedCrackPoint);
					}

					else if (crackPoint.TargetVertexDifferentWithinTolerance)
					{
						IDisposable addedCrackPoint =
							MapView.Active.AddOverlay(crackPoint.Point, greenSquareMarker);

						_overlays.Add(addedCrackPoint);
					}

					else
					{
						IDisposable addedCrackPoint =
							MapView.Active.AddOverlay(crackPoint.Point, greenCircleMarker);

						_overlays.Add(addedCrackPoint);
					}
				}
			}
		}

		public void UpdateExtent(Envelope extent)
		{
			_extentOverlay?.Dispose();

			if (extent == null)
			{
				return;
			}

			var polygon = GeometryFactory.CreatePolygon(extent);

			// Extent symbolization

			var outlineSymbol = SymbolUtils.CreateLineSymbol(255, 255, 255, 5);
			var lineSymbol = SymbolUtils.CreateLineSymbol(0, 255, 150, 2);

			var polygonSymbol =
				SymbolUtils.CreatePolygonSymbol(lineSymbol.SymbolLayers[0],
				                                outlineSymbol.SymbolLayers[0]);

			_extentOverlay =
				MapView.Active.AddOverlay(polygon, polygonSymbol.MakeSymbolReference());

			_overlays.Add(_extentOverlay);
		}

		public void DisposeOverlays()
		{
			foreach (IDisposable overlay in _overlays)
			{
				overlay.Dispose();
			}

			_overlays.Clear();

			_extentOverlay = null;
		}
	}
}
