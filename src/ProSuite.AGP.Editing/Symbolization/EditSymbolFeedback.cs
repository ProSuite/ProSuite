using System;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Symbolization;

public abstract class EditSymbolFeedback : IDisposable
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public abstract void Clear();

	public void Dispose()
	{
		Clear();

		GC.SuppressFinalize(this);
	}

	protected static void UpdateOverlay(MapView mapView, IDisposable overlay, Geometry geometry, CIMSymbolReference symbol)
	{
		if (!mapView.UpdateOverlay(overlay, geometry, symbol))
		{
			_msg.Warn("UpdateOverlay() returned false; display feedback may be wrong; see K2#37");
			// ask Redlands when this can happen and what we should do (K2#37)
		}
	}
}
