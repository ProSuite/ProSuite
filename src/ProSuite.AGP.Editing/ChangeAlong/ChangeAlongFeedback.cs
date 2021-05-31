using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public class ChangeAlongFeedback
	{
		private const double _lineWidth = 3;
		private const double _markerSize = 6;
		private readonly CIMLineSymbol _candidateReshapeLineSymbol;
		private readonly CIMLineSymbol _filteredReshapeLineSymbol;

		private readonly CIMLineSymbol _noReshapeLineSymbol;

		private readonly List<IDisposable> _overlays = new List<IDisposable>();
		private readonly CIMLineSymbol _reshapeLineSymbol;
		private readonly CIMPointSymbol _candidateLineEnd;
		private readonly CIMPointSymbol _filteredLineEnd;

		private readonly CIMPointSymbol _noReshapeLineEnd;
		private readonly CIMPointSymbol _reshapeLineEnd;

		public ChangeAlongFeedback()
		{
			var red = ColorFactory.Instance.CreateRGBColor(248, 0, 0);
			_noReshapeLineSymbol = SymbolFactory.Instance.ConstructLineSymbol(red, _lineWidth);

			var green = ColorFactory.Instance.CreateRGBColor(0, 248, 0);
			_reshapeLineSymbol = SymbolFactory.Instance.ConstructLineSymbol(green, _lineWidth);

			var yellow = ColorFactory.Instance.CreateRGBColor(255, 255, 0);
			_candidateReshapeLineSymbol =
				SymbolFactory.Instance.ConstructLineSymbol(yellow, _lineWidth);

			var grey = ColorFactory.Instance.CreateRGBColor(100, 100, 100);
			_filteredReshapeLineSymbol =
				SymbolFactory.Instance.ConstructLineSymbol(grey, _lineWidth);

			_noReshapeLineEnd = CreateMarkerSymbol(red);
			_reshapeLineEnd = CreateMarkerSymbol(green);
			_candidateLineEnd = CreateMarkerSymbol(yellow);
			_filteredLineEnd = CreateMarkerSymbol(grey);
		}

		public void Update([CanBeNull] ChangeAlongCurves newCurves)
		{
			DisposeOverlays();

			if (newCurves == null) return;

			foreach (var cutSubcurve in newCurves.ReshapeCutSubcurves)
				if (cutSubcurve.IsFiltered)
					AddOverlay(cutSubcurve.Path, _filteredReshapeLineSymbol);
				else if (cutSubcurve.IsReshapeMemberCandidate)
					AddOverlay(cutSubcurve.Path, _candidateReshapeLineSymbol);
				else if (cutSubcurve.CanReshape)
					AddOverlay(cutSubcurve.Path, _reshapeLineSymbol);
				else
					AddOverlay(cutSubcurve.Path, _noReshapeLineSymbol);

			foreach (var cutSubcurve in newCurves.ReshapeCutSubcurves)
				if (cutSubcurve.IsFiltered)
				{
					AddOverlay(cutSubcurve.FromPoint, _filteredLineEnd);
					AddOverlay(cutSubcurve.ToPoint, _filteredLineEnd);
				}
				else if (cutSubcurve.IsReshapeMemberCandidate)
				{
					AddOverlay(cutSubcurve.FromPoint, _candidateLineEnd);
					AddOverlay(cutSubcurve.ToPoint, _candidateLineEnd);
				}
				else if (cutSubcurve.CanReshape)
				{
					AddOverlay(cutSubcurve.FromPoint, _reshapeLineEnd);
					AddOverlay(cutSubcurve.ToPoint, _reshapeLineEnd);
				}
				else
				{
					AddOverlay(cutSubcurve.FromPoint, _noReshapeLineEnd);
					AddOverlay(cutSubcurve.ToPoint, _noReshapeLineEnd);
				}
		}

		private void AddOverlay(Geometry polyline, CIMSymbol symbol)
		{
			var addedOverlay = MapView.Active.AddOverlay(polyline,
			                                             symbol.MakeSymbolReference());

			_overlays.Add(addedOverlay);
		}

		public void DisposeOverlays()
		{
			foreach (var overlay in _overlays) overlay.Dispose();

			_overlays.Clear();
		}

		private static CIMPointSymbol CreateMarkerSymbol(CIMColor color)
		{
			var circlePtSymbol =
				SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.BlueRGB, 6,
				                                            SimpleMarkerStyle.Circle);

			var marker = Assert.NotNull(circlePtSymbol.SymbolLayers[0] as CIMVectorMarker);

			var polySymbol = Assert.NotNull(marker.MarkerGraphics[0].Symbol as CIMPolygonSymbol);

			polySymbol.SymbolLayers[0] =
				SymbolFactory.Instance.ConstructStroke(ColorFactory.Instance.WhiteRGB, 2,
				                                       SimpleLineStyle.Solid);
			polySymbol.SymbolLayers[1] = SymbolFactory.Instance.ConstructSolidFill(color);

			return circlePtSymbol;
		}
	}
}
