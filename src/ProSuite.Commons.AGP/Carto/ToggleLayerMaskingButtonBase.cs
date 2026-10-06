using System;
using System.Windows.Media;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;

namespace ProSuite.Commons.AGP.Carto;

public class ToggleLayerMaskingButtonBase : ToggleSymbolDisplayButtonBase
{
	private readonly ImageSource _iconOn = GetImage("Images/LayerMaskingOn.xaml");
	private readonly ImageSource _iconOff = GetImage("Images/LayerMaskingOff.xaml");
	private readonly ImageSource _iconUnknown = GetImage("Images/LayerMaskingUnknown.xaml");
	private bool? _lastState;

	protected override void UpdateCore()
	{
		var map = MapView.Active?.Map;

		Enabled = map != null;
		IsChecked = false;

		var state = Manager.QuickUsesLM(map);

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
				TooltipHeading = "Turn SLM off";
			}
			else
			{
				LargeImage = _iconOff;
				SmallImage = _iconOff;
				TooltipHeading = "Turn SLM on";
			}
		}
		else
		{
			LargeImage = _iconUnknown;
			SmallImage = _iconUnknown;
			TooltipHeading = "Toggle Layer Masking (SLM)";
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
		bool turnOn = ! Manager.UsesLM(map, uncached);
		var name = turnOn ? "Turn SLM on" : "Turn SLM off";

		Gateway.CompositeOperation(
			undoStack, name, () => Manager.ToggleLM(map, turnOn));

		return turnOn;
	}

	protected override bool GetInitialState(Map map)
	{
		const bool uncached = true;
		return Manager.UsesLM(map, uncached);
	}
}
