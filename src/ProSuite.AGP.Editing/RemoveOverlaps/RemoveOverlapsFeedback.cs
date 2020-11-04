using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.RemoveOverlaps
{
	public class RemoveOverlapsFeedback
	{
		private static CIMLineSymbol _overlapLineSymbol;
		private readonly CIMPolygonSymbol _overlapPolygonSymbol;

		private readonly List<IDisposable> _overlays = new List<IDisposable>();

		public RemoveOverlapsFeedback()
		{
			CIMColor color = ColorFactory.Instance.CreateRGBColor(255, 0, 0);

			_overlapLineSymbol = SymbolFactory.Instance.ConstructLineSymbol(color, 2);

			var hatchFill = new CIMHatchFill
			                {
				                Enable = true,
				                Rotation = 0.0,
				                Separation = 2.5,
				                LineSymbol = _overlapLineSymbol
			                };

			var symbolLayers = new List<CIMSymbolLayer>();

			symbolLayers.AddRange(_overlapLineSymbol.SymbolLayers);
			symbolLayers.Add(hatchFill);

			_overlapPolygonSymbol = new CIMPolygonSymbol {SymbolLayers = symbolLayers.ToArray()};
		}

		public void Update([CanBeNull] Overlaps newOverlaps)
		{
			DisposeOverlays();

			if (newOverlaps == null)
			{
				return;
			}

			foreach (Geometry overlap in newOverlaps.OverlapGeometries)
			{
				CIMSymbol symbol = overlap is Polygon
					                   ? _overlapPolygonSymbol
					                   : (CIMSymbol) _overlapLineSymbol;

				var addedOverlay = MapView.Active.AddOverlay(overlap, symbol.MakeSymbolReference());

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
