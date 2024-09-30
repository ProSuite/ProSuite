using System;
using System.Windows.Media.Imaging;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

// TODO derive from ButtonCommandBase? --> review LogEntry etc.
public abstract class ToggleSymbolLayerDrawingButtonBase : Button
{
	private readonly BitmapImage _iconOn16 = GetImage("Images/SymbolLayerDrawingOn16.png");
	private readonly BitmapImage _iconOff16 = GetImage("Images/SymbolLayerDrawingOff16.png");
	private readonly BitmapImage _iconOn32 = GetImage("Images/SymbolLayerDrawingOn32.png");
	private readonly BitmapImage _iconOff32 = GetImage("Images/SymbolLayerDrawingOff32.png");

	private bool? _toggleState; // initially unknown

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected override async void OnClick()
	{
		Gateway.LogEntry(_msg);

		try
		{
			var map = MapView.Active?.Map;
			if (map is null) return;

			var desiredState = GetToggledState(_toggleState, false);

			_msg.DebugFormat(
				"Toggle Symbol Layer Drawing (SLD): current state is {0}, desired state is {1}",
				Format(_toggleState), Format(desiredState));

			await QueuedTask.Run(() => ToggleSymbolLayerDrawing(map, desiredState));

			_toggleState = desiredState;
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	protected override void OnUpdate()
	{
		Enabled = MapView.Active != null;

		IsChecked = false;

		if (_toggleState.HasValue)
		{
			bool isOn = _toggleState.Value;
			TooltipHeading = isOn ? "Turn off SLD" : "Turn on SLD";
		}
		else
		{
			TooltipHeading = "Toggle Symbol Layer Drawing (SLD)";
		}

		SmallImage = _toggleState == false ? _iconOff16 : _iconOn16;
		LargeImage = _toggleState == false ? _iconOff32 : _iconOn32;
	}

	private void ToggleSymbolLayerDrawing(Map map, bool turnOn)
	{
		if (map is null)
			throw new ArgumentNullException(nameof(map));

		var undoStack = map.OperationManager;
		if (undoStack is null)
			throw new InvalidOperationException("Map's undo/redo stack is null");

		var name = Caption; // use command's caption as operation name
		if (string.IsNullOrEmpty(name))
		{
			name = $"Turn SLD {(turnOn ? "on" : "off")}";
		}

		Gateway.CompositeOperation(
			undoStack, name,
			() => DisplayUtils.ToggleSymbolLayerDrawing(map, turnOn));
	}

	private static bool GetToggledState(bool? state, bool firstState)
	{
		if (!state.HasValue) return firstState;
		return !state.Value;
	}

	private static string Format(bool? flag)
	{
		if (!flag.HasValue) return "unknown";
		return flag.Value ? "on" : "off";
	}

	private static BitmapImage GetImage(string fileName)
	{
		return new BitmapImage(GetPackUri(fileName));
	}

	private static Uri GetPackUri(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			throw new ArgumentNullException(nameof(fileName));
		var s = $"pack://application:,,,/ProSuite.Commons.AGP;component/{fileName}";
		return new Uri(s);
	}
}
