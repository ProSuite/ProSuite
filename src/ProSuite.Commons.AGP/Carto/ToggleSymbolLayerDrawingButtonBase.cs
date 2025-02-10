using System;
using System.Windows.Media.Imaging;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.Commons.AGP.Carto;

public class ToggleSymbolLayerDrawingButtonBase : ToggleSymbolDisplayButtonBase
{
	private readonly BitmapImage _iconOn16 = GetImage("Images/SymbolLayerDrawingOn16.png");
	private readonly BitmapImage _iconOff16 = GetImage("Images/SymbolLayerDrawingOff16.png");
	private readonly BitmapImage _iconOn32 = GetImage("Images/SymbolLayerDrawingOn32.png");
	private readonly BitmapImage _iconOff32 = GetImage("Images/SymbolLayerDrawingOff32.png");
	private readonly BitmapImage _iconUnknown = GetImage("Images/SymbolLayerDrawingUnknown32.png");
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
				LargeImage = _iconOn32;
				SmallImage = _iconOn16;
				TooltipHeading = "Turn SLD off";
			}
			else
			{
				LargeImage = _iconOff32;
				SmallImage = _iconOff16;
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
