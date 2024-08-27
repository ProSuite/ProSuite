using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Cracker
{
	public class CrackerFeedback
	{
		private static CIMLineSymbol _overlapLineSymbol;
		private readonly CIMPolygonSymbol _overlapPolygonSymbol;

		private readonly CIMMarker _redCircleMarker;
		private readonly CIMMarker _greenSquareMarker;
		private readonly CIMMarker _greenCircleMarker;

		private readonly List<IDisposable> _overlays = new List<IDisposable>();

		public CrackerFeedback()
		{
			_overlapLineSymbol =
				SymbolUtils.CreateLineSymbol(255, 0, 0, 2);

			_overlapPolygonSymbol = SymbolUtils.CreateHatchFillSymbol(255, 0, 0);

			CIMColor red = ColorUtils.CreateRGB(255, 0, 0);
			CIMColor green = ColorUtils.CreateRGB(0, 255, 0);

			_redCircleMarker = SymbolUtils.CreateMarker(red, 5, SymbolUtils.MarkerStyle.Circle);
			_greenSquareMarker = SymbolUtils.CreateMarker(green, 5, SymbolUtils.MarkerStyle.Square);
			_greenCircleMarker = SymbolUtils.CreateMarker(green, 5, SymbolUtils.MarkerStyle.Circle);

			//_crackPointRed = SymbolUtils.CreateMarker(255, 0, 0, 10, SimpleMarkerStyle.Circle);
		}

		public void Update([CanBeNull] CrackerResult crackerResult)
		{
			DisposeOverlays();

			if (crackerResult == null)
			{
				return;
			}

			CIMSymbolReference redCircle = _redCircleMarker.MakePointSymbol().MakeSymbolReference();
			CIMSymbolReference greenSquare =
				_greenSquareMarker.MakePointSymbol().MakeSymbolReference();
			CIMSymbolReference greenCircle =
				_greenCircleMarker.MakePointSymbol().MakeSymbolReference();

			foreach (var crackedFeature in crackerResult.ResultsByFeature)
			{
				foreach (CrackPoint crackPoint in crackedFeature.CrackPoints)
				{
					if (crackPoint.ViolatesMinimumSegmentLength)
					{
						IDisposable addedCrackPoint =
							MapView.Active.AddOverlay(crackPoint.Point, redCircle);

						_overlays.Add(addedCrackPoint);
					}

					else if (crackPoint.TargetVertexDifferentWithinTolerance)
					{
						IDisposable addedCrackPoint =
							MapView.Active.AddOverlay(crackPoint.Point, greenSquare);

						_overlays.Add(addedCrackPoint);
					}
					else
					{
						IDisposable addedCrackPoint =
							MapView.Active.AddOverlay(crackPoint.Point, greenCircle);

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
	}
}
