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
		private readonly CIMLineSymbol _preselectedLineSymbol;
		private readonly CIMLineSymbol _filteredReshapeLineSymbol;

		private readonly CIMLineSymbol _noReshapeLineSymbol;

		private readonly List<IDisposable> _overlays = new List<IDisposable>();
		private readonly CIMLineSymbol _reshapeLineSymbol;
		private readonly CIMPointSymbol _candidateLineEnd;
		private readonly CIMPointSymbol _preselectedLineEnd;
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

			var blue = ColorFactory.Instance.CreateRGBColor(0, 0, 255);
			_preselectedLineSymbol =
				SymbolFactory.Instance.ConstructLineSymbol(blue, _lineWidth);

			var grey = ColorFactory.Instance.CreateRGBColor(100, 100, 100);
			_filteredReshapeLineSymbol =
				SymbolFactory.Instance.ConstructLineSymbol(grey, _lineWidth);

			_noReshapeLineEnd = CreateMarkerSymbol(red);
			_reshapeLineEnd = CreateMarkerSymbol(green);
			_candidateLineEnd = CreateMarkerSymbol(yellow);
			_preselectedLineEnd = CreateMarkerSymbol(blue);
			_filteredLineEnd = CreateMarkerSymbol(grey);
		}

		public void Update([CanBeNull] ChangeAlongCurves newCurves)
		{
			DisposeOverlays();

			if (newCurves == null)
			{
				return;
			}

			Predicate<CutSubcurve> noReshape = c =>
				! c.CanReshape && ! c.IsReshapeMemberCandidate && ! c.IsFiltered;

			Predicate<CutSubcurve> isCandidate = c =>
				c.IsReshapeMemberCandidate && ! newCurves.PreSelectedSubcurves.Contains(c);

			Predicate<CutSubcurve> isPreSelected = c => newCurves.PreSelectedSubcurves.Contains(c);

			AddReshapeLines(newCurves.ReshapeCutSubcurves, noReshape, _noReshapeLineSymbol);
			AddReshapeLines(newCurves.ReshapeCutSubcurves, c => c.IsFiltered,
			                _filteredReshapeLineSymbol);
			AddReshapeLines(newCurves.ReshapeCutSubcurves, isCandidate,
			                _candidateReshapeLineSymbol);
			AddReshapeLines(newCurves.ReshapeCutSubcurves, isPreSelected, _preselectedLineSymbol);
			AddReshapeLines(newCurves.ReshapeCutSubcurves, c => c.CanReshape,
			                _reshapeLineSymbol);

			AddReshapeLineEndpoints(newCurves.ReshapeCutSubcurves, noReshape, _noReshapeLineEnd);
			AddReshapeLineEndpoints(newCurves.ReshapeCutSubcurves, c => c.IsFiltered,
			                        _filteredLineEnd);
			AddReshapeLineEndpoints(newCurves.ReshapeCutSubcurves, isCandidate, _candidateLineEnd);
			AddReshapeLineEndpoints(newCurves.ReshapeCutSubcurves, isPreSelected,
			                        _preselectedLineEnd);

			AddReshapeLineEndpoints(newCurves.ReshapeCutSubcurves, c => c.CanReshape,
			                        _reshapeLineEnd);
		}

		public void DisposeOverlays()
		{
			foreach (var overlay in _overlays) overlay.Dispose();

			_overlays.Clear();
		}

		private void AddReshapeLines(IEnumerable<CutSubcurve> subcurves,
		                             Predicate<CutSubcurve> predicate, CIMLineSymbol symbol)
		{
			foreach (var cutSubcurve in subcurves)
			{
				if (predicate == null || predicate(cutSubcurve))
				{
					AddOverlay(cutSubcurve.Path, symbol);
				}
			}
		}

		private void AddReshapeLineEndpoints(IEnumerable<CutSubcurve> subcurves,
		                                     Predicate<CutSubcurve> predicate,
		                                     CIMPointSymbol symbol)
		{
			foreach (var cutSubcurve in subcurves)
			{
				if (predicate == null || predicate(cutSubcurve))
				{
					AddOverlay(cutSubcurve.FromPoint, symbol);
					AddOverlay(cutSubcurve.ToPoint, symbol);
				}
			}
		}

		private void AddOverlay(Geometry polyline, CIMSymbol symbol)
		{
			var addedOverlay = MapView.Active.AddOverlay(polyline,
			                                             symbol.MakeSymbolReference());

			_overlays.Add(addedOverlay);
		}

		private static CIMPointSymbol CreateMarkerSymbol(CIMColor color)
		{
			var circlePtSymbol =
				SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.BlueRGB,
				                                            _markerSize,
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
