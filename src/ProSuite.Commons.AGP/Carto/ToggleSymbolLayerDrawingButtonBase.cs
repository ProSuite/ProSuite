using System;
using System.Windows.Media.Imaging;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;
using Button = ArcGIS.Desktop.Framework.Contracts.Button;

namespace ProSuite.Commons.AGP.Carto;

// TODO derive from ButtonCommandBase? --> review LogEntry etc.
public abstract class ToggleSymbolLayerDrawingButtonBase : Button
{
	private readonly BitmapImage _iconOn16 = GetImage("Images/SymbolLayerDrawingOn16.png");
	private readonly BitmapImage _iconOff16 = GetImage("Images/SymbolLayerDrawingOff16.png");
	private readonly BitmapImage _iconOn32 = GetImage("Images/SymbolLayerDrawingOn32.png");
	private readonly BitmapImage _iconOff32 = GetImage("Images/SymbolLayerDrawingOff32.png");
	private readonly BitmapImage _iconUnknown = GetImage("Images/SymbolLayerDrawingUnknown32.png");

	private bool? _toggleState; // initially unknown

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected virtual ISymbolLayerDisplay Manager => SymbolLayerDisplayManager.Instance;

	protected ToggleSymbolLayerDrawingButtonBase()
	{
		MapViewInitializedEvent.Subscribe(OnMapViewInitialized);
		ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);

		Initialize();
	}

	private void OnMapViewInitialized(MapViewEventArgs args)
	{
		Initialize(args.MapView?.Map);
	}

	private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
	{
		// empirical: may get this twice, once for the old and once for the new active map
		Initialize(args.IncomingView?.Map);
	}

	private async void Initialize(Map map = null)
	{
		try
		{
			map ??= MapView.Active?.Map;
			if (map is null) return;

			const bool uncached = true;
			_toggleState = await QueuedTask.Run(() => Manager.UsesSLD(map, uncached));
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}

	protected override async void OnClick()
	{
		Gateway.LogEntry(_msg);

		try
		{
			var map = MapView.Active?.Map;
			if (map is null) return;

			//var desiredState = GetToggledState(_toggleState, false);

			//_msg.DebugFormat(
			//	"Toggle Symbol Layer Drawing (SLD): current state is {0}, desired state is {1}",
			//	Format(_toggleState), Format(desiredState));

			//await QueuedTask.Run(() => ToggleSymbolLayerDrawing(map, desiredState));
			await QueuedTask.Run(() => ToggleSLD(map));

			//_toggleState = desiredState;
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	protected override void OnUpdate()
	{
		var map = MapView.Active?.Map;

		Enabled = map != null;
		IsChecked = false;

		var state = Manager.QuickUsesSLD(map);

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

	private void ToggleSLD(Map map)
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

		_toggleState = turnOn;
	}

	//private void ToggleSymbolLayerDrawing(Map map, bool turnOn)
	//{
	//	if (map is null)
	//		throw new ArgumentNullException(nameof(map));

	//	var undoStack = map.OperationManager;
	//	if (undoStack is null)
	//		throw new InvalidOperationException("Map's undo/redo stack is null");

	//	var name = Caption; // use command's caption as operation name
	//	if (string.IsNullOrEmpty(name))
	//	{
	//		name = $"Turn SLD {(turnOn ? "on" : "off")}";
	//	}

	//	Gateway.CompositeOperation(
	//		undoStack, name,
	//		() => DisplayUtils.ToggleSymbolLayerDrawing(map, turnOn));
	//}

	//private static bool GetToggledState(bool? state, bool firstState)
	//{
	//	if (!state.HasValue) return firstState;
	//	return !state.Value;
	//}

	//private static string Format(bool? flag)
	//{
	//	if (!flag.HasValue) return "unknown";
	//	return flag.Value ? "on" : "off";
	//}

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
