using System;
using System.Collections.Generic;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.RemoveOverlaps;

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

			foreach (Geometry overlap in overlapGeometries)
			{
				CIMSymbol symbol = overlap is Polygon
					                   ? _overlapPolygonSymbol
					                   : (CIMSymbol) _overlapLineSymbol;

				// Fully visible in 3D with show-through factor of 1.0
				const double showThrough = 1.0;
				IDisposable addedOverlay =
					MapView.Active.AddOverlay(overlap, symbol.MakeSymbolReference(), -1,
					                          showThrough);

				_overlays.Add(addedOverlay);
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