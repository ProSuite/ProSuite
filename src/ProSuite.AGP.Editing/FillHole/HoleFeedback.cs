using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.FillHole;

namespace ProSuite.AGP.Editing.FillHole
{
	public class HoleFeedback
	{
		private static CIMLineSymbol _holeOutlineSymbol;
		private readonly CIMPolygonSymbol _holeSymbol;

		private readonly List<IDisposable> _overlays = new List<IDisposable>();

		public HoleFeedback()
		{
			_holeOutlineSymbol =
				SymbolUtils.CreateLineSymbol(0, 255, 0, 2);

			_holeSymbol = SymbolUtils.CreateHatchFillSymbol(0, 255, 0);

			//_perimeterSymbol = SymbolUtils.CreateHollowFillSymbol();
		}

		public void Update([CanBeNull] IEnumerable<Holes> holes)
		{
			DisposeOverlays();

			if (holes == null)
			{
				return;
			}

			foreach (var holeGeometry in holes.SelectMany(h => h.HoleGeometries))
			{
				IDisposable addedOverlay =
					MapView.Active.AddOverlay(holeGeometry, _holeSymbol.MakeSymbolReference());

				_overlays.Add(addedOverlay);
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
