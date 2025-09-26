using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Holes;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.FillHole
{
	public class HoleFeedback
	{
		private static CIMLineSymbol _holeOutlineSymbol;
		private readonly CIMPolygonSymbol _holeSymbol;
		private readonly HoleToolOptions _fillHoleToolOptions;

		private IDisposable _extentOverlay;

		private readonly List<IDisposable> _overlays = new List<IDisposable>();

		private IEnumerable<Holes> _currentHoles;

		public HoleFeedback(HoleToolOptions fillHoleToolOptions)
		{
			_fillHoleToolOptions = fillHoleToolOptions;

			_holeOutlineSymbol =
				SymbolUtils.CreateLineSymbol(0, 255, 0, 2);

			_holeSymbol = SymbolUtils.CreateHatchFillSymbol(0, 255, 0, lineWidth: 0.5);
		}

		public void Update([CanBeNull] IEnumerable<Holes> holes)
		{
			DisposeOverlays();

			if (holes != null)
			{
				_currentHoles = holes;
			}

			if (_currentHoles == null || !_fillHoleToolOptions.ShowPreview)
			{
				return;
			}

			foreach (var holeGeometry in _currentHoles.SelectMany(h => h.HoleGeometries))
			{
				IDisposable addedOverlay =
					MapView.Active.AddOverlay(holeGeometry, _holeSymbol.MakeSymbolReference());
				_overlays.Add(addedOverlay);
			}

		}

		public void UpdateExtent(Envelope extent)

		{
			_extentOverlay?.Dispose();

			if (!_fillHoleToolOptions.LimitPreviewToExtent)

			{
				return;
			}

			Envelope activeExtent = extent ?? MapView.Active?.Extent;

			var polygon = GeometryFactory.CreatePolygon(activeExtent);

			// Extent symbolization

			var outlineSymbol = SymbolUtils.CreateLineSymbol(255, 255, 255, 5);
			var lineSymbol = SymbolUtils.CreateLineSymbol(0, 255, 150, 2);

			var polygonSymbol =
				SymbolUtils.CreatePolygonSymbol(lineSymbol.SymbolLayers[0], outlineSymbol.SymbolLayers[0]);

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
		}
	}
}
