using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.RemoveOverlaps;

namespace ProSuite.AGP.Editing.RemoveOverlaps
{
	public class RemoveOverlapsFeedback
	{
		private static CIMLineSymbol _overlapLineSymbol;
		private readonly CIMPolygonSymbol _overlapPolygonSymbol;

		private readonly List<IDisposable> _overlays = new List<IDisposable>();

		public RemoveOverlapsFeedback()
		{
			_overlapLineSymbol =
				SymbolUtils.CreateLineSymbol(255, 0, 0, 2);

			_overlapPolygonSymbol = SymbolUtils.CreateHatchFillSymbol(255, 0, 0);
		}

		public void Update([CanBeNull] Overlaps newOverlaps)
		{
			DisposeOverlays();

			if (newOverlaps == null)
			{
				return;
			}

			foreach (var overlapsBySourceRef in newOverlaps.OverlapGeometries)
			{
				IList<Geometry> overlapGeometries = overlapsBySourceRef.Value;

				if (overlapGeometries.Count == 0)
				{
					continue;
				}

				Geometry overlaps = GeometryEngine.Instance.Union(overlapsBySourceRef.Value);

				CIMSymbol symbol = overlaps is Polygon
					                   ? _overlapPolygonSymbol
					                   : (CIMSymbol) _overlapLineSymbol;

				var addedOverlay =
					MapView.Active.AddOverlay(overlaps, symbol.MakeSymbolReference());

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
