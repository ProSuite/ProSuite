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

namespace ProSuite.AGP.Editing.Cracker
{
	public class CrackerFeedback
	{
		private static CIMLineSymbol _overlapLineSymbol;
		private readonly CIMPolygonSymbol _overlapPolygonSymbol;
		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private static CIMSymbolReference _outlinedPointSymRef;
		private readonly CIMSymbolReference redCircleMarker;
		private readonly CIMSymbolReference greenCircleMarker;
		private readonly CIMSymbolReference greenSquareMarker;
		private readonly CIMSymbolReference redCrossMarker;
		private readonly CIMSymbolReference redSquareMarker;
		private readonly CIMSymbolReference mintCircleMarker;
		private readonly CIMSymbolReference greySquareMarker;

		private readonly List<IDisposable> _overlays = new();

		// Create a point symbol with an outline
		private static CIMSymbolReference CreateOutlinedPointSymbol(
			CIMColor fillColor, CIMColor strokeColor, double size, SymbolUtils.MarkerStyle style)
		{
			var stroke = SymbolUtils.CreateSolidStroke(strokeColor, size / 5);
			var polySym =
				SymbolUtils.CreatePolygonSymbol(fillColor, SymbolUtils.FillStyle.Solid, stroke);
			var marker = SymbolUtils.CreateMarker(style, polySym, size);
			var symbol = SymbolUtils.CreatePointSymbol(marker);
			_outlinedPointSymRef = symbol.MakeSymbolReference();

			return _outlinedPointSymRef;
		}

		public CrackerFeedback()
		{
			_overlapLineSymbol =
				SymbolUtils.CreateLineSymbol(255, 0, 0, 2);

			_overlapPolygonSymbol = SymbolUtils.CreateHatchFillSymbol(255, 0, 0);

			CIMColor red = ColorUtils.CreateRGB(255, 0, 0);
			CIMColor green = ColorUtils.CreateRGB(0, 200, 0);
			CIMColor mint = ColorUtils.CreateRGB(0, 255, 150);
			CIMColor grey = ColorUtils.CreateRGB(100, 100, 100);
			CIMColor white = ColorUtils.CreateRGB(255, 255, 255);

			redCircleMarker =
				CreateOutlinedPointSymbol(red, white, 10, SymbolUtils.MarkerStyle.Circle);
			greenCircleMarker =
				CreateOutlinedPointSymbol(green, white, 10, SymbolUtils.MarkerStyle.Circle);
			greenSquareMarker =
				CreateOutlinedPointSymbol(green, white, 10, SymbolUtils.MarkerStyle.Square);
			mintCircleMarker =
				CreateOutlinedPointSymbol(mint, white, 10, SymbolUtils.MarkerStyle.Circle);
			redCrossMarker =
				CreateOutlinedPointSymbol(white, red, 10, SymbolUtils.MarkerStyle.Cross);
			greySquareMarker =
				CreateOutlinedPointSymbol(grey, white, 5, SymbolUtils.MarkerStyle.Square);
			redSquareMarker =
				CreateOutlinedPointSymbol(red, white, 5, SymbolUtils.MarkerStyle.Square);
			//TODO: remove segment line feature
		}

		public void Update([CanBeNull] CrackerResult crackerResult, IList<Feature> selectedFeatures)
		{
			// clear any previous drawings
			DisposeOverlays();

			// get all vertices of selected features
			foreach (var feature in selectedFeatures)
			{
				IEnumerable<MapPoint> vertices = GeometryUtils.GetVertices(feature);

				// draw vertices before drawing crack points
				foreach (var vertex in vertices)
				{
					IDisposable addedVertex =
						MapView.Active.AddOverlay(vertex, greySquareMarker);
					_overlays.Add(addedVertex);
				if (DisplayVerticesCoordinates){
						IDisposable addedLabel =
						MapView.Active.AddOverlay(vertex, redCrossMarker);
					_overlays.Add(addedLabel);
				}
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

		public void DisposeOverlays()
		{
			foreach (IDisposable overlay in _overlays)
			{
				overlay.Dispose();
			}

			_overlays.Clear();
		}

		#region VerticesLabels

		public bool DisplayVerticesCoordinates { get; set; }
		private static readonly CIMTextSymbol _textSymbol = null;

		private static CIMTextGraphic CreatePointLabel(string text){
			var textGraphic = new CIMTextGraphic {
				                                     Symbol = _textSymbol
					                                     .MakeSymbolReference(),
				                                     
				                                     Text = text
			                                     };


		   // TODO: Check in SymbolUtils, how the CreatePoint Symbol is done



		   return textGraphic;
		}
		
		private void UpdateVerticesLabels()
		{
			
		}

		public void ToggleVerticesLabels()
		{
			// Toggle the DisplayVerticesCoordinates property
			DisplayVerticesCoordinates = ! DisplayVerticesCoordinates;
			// Log the change
			_msg.Info($"DisplayVerticesCoordinates toggled to {DisplayVerticesCoordinates}");
			// Draw or dispose the coordinate overlay
			if (DisplayVerticesCoordinates)
			{
				Update(CrackerToolBase.ResultCrackPoints, CrackerToolBase.SelectedFeatures);
			}
			else
			{
				DisposeOverlays();
			}
		}

		#endregion
	}
}
