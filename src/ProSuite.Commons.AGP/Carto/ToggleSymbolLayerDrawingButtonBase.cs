using System;
using System.Windows.Media;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.Commons.AGP.Carto;

public class ToggleSymbolLayerDrawingButtonBase : ToggleSymbolDisplayButtonBase
{
	private readonly ImageSource _iconOn = GetImage("Images/SymbolLayerDrawingOn.xaml");
	private readonly ImageSource _iconOff = GetImage("Images/SymbolLayerDrawingOff.xaml");
	private readonly ImageSource _iconUnknown = GetImage("Images/SymbolLayerDrawingUnknown.xaml");
	private bool? _lastState;

	protected override void UpdateCore()
	{
		var map = MapView.Active?.Map;

		Enabled = map != null;
		IsChecked = false;

		var state = Manager.QuickUsesSLD(map);

		if (state == _lastState)
		{
			return; // avoid overhead if state did not change
		}

		_lastState = state;

		if (state.HasValue)
		{
			if (state.Value)
			{
				LargeImage = _iconOn;
				SmallImage = _iconOn;
				TooltipHeading = "Turn SLD off";
			}
			else
			{
				LargeImage = _iconOff;
				SmallImage = _iconOff;
				TooltipHeading = "Turn SLD on";
			}
		}
		else
		{
			LargeImage = _iconUnknown;
			SmallImage = _iconUnknown;
			TooltipHeading = "Toggle Symbol Layer Drawing (SLD)";
		}
	}

	protected override bool Toggle(Map map)
	{
		if (map is null)
			throw new ArgumentNullException(nameof(map));

		var undoStack = map.OperationManager;
		if (undoStack is null)
			throw new InvalidOperationException("Map's undo/redo stack is null");

		const bool uncached = true;
		bool turnOn = ! Manager.UsesSLD(map, uncached);
		var name = turnOn ? "Turn SLD on" : "Turn SLD off";

		Gateway.CompositeOperation(
			undoStack, name, () => Manager.ToggleSLD(map, turnOn));

		return turnOn;
	}

	protected override bool GetInitialState(Map map)
	{
		const bool uncached = true;
		return Manager.UsesSLD(map, uncached);
	}
}
