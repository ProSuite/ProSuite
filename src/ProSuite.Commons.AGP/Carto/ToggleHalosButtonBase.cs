using System;
using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

public abstract class ToggleHalosButtonBase : Button
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected ToggleHalosButtonBase()
	{
		// Assume no halos if current state is unknown
		IsChecked = Halos.Instance.ToggleState ?? false;
	}

	protected override async void OnClick()
	{
		try
		{
			Gateway.LogEntry(_msg);

			var mapView = MapView.Active;
			if (mapView?.Map is null) return;

			IsChecked = ! IsChecked;

			await QueuedTask.Run(() => ToggleHalos(mapView, IsChecked));
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	private static void ToggleHalos(MapView mapView, bool on)
	{
		var map = mapView.Map ?? throw new InvalidOperationException();
		var substitutionType = on
			                       ? SymbolSubstitutionType.IndividualSubordinate
			                       : SymbolSubstitutionType.None;

		var modified = Halos.Instance.ToggleHalo(map, substitutionType);

		if (modified)
		{
			mapView.Redraw(false);
		}
	}
}
