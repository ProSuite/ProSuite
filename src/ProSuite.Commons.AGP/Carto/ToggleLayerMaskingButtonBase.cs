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
public abstract class ToggleLayerMaskingButtonBase : Button
{
	private readonly BitmapImage _iconOn16 = GetImage("Images/LayerMaskingOn16.png");
	private readonly BitmapImage _iconOff16 = GetImage("Images/LayerMaskingOff16.png");
	private readonly BitmapImage _iconOn32 = GetImage("Images/LayerMaskingOn32.png");
	private readonly BitmapImage _iconOff32 = GetImage("Images/LayerMaskingOff32.png");

	private bool? _toggleState; // initially unknown

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected ToggleLayerMaskingButtonBase()
	{
		ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);

		Initialize();
	}

	private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
	{
		// empirical: may get this twice, once for the old and once for the new active map
		Initialize();
	}

	/// <remarks>Must call on MCT</remarks>
	private async void Initialize()
	{
		try
		{
			var map = MapView.Active?.Map;
			_toggleState = await QueuedTask.Run(() => DisplayUtils.UsesLayerMasking(map));
		}
		catch (Exception ex)
		{
			_msg.Error($"{GetType().Name}: {ex.Message}", ex);
		}
	}

	protected override async void OnClick()
	{
		Gateway.LogEntry(_msg);

		try
		{
			var map = MapView.Active?.Map;
			if (map is null) return;

			var desiredState = GetToggledState(_toggleState, false);

			_msg.DebugFormat(
				"Toggle Layer Masking (LM): current state is {0}, desired state is {1}",
				Format(_toggleState), Format(desiredState));

			await QueuedTask.Run(() => ToggleLayerMasking(map, desiredState));

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
			TooltipHeading = isOn ? "Turn off LM" : "Turn on LM";
		}
		else
		{
			TooltipHeading = "Toggle Layer Masking (LM)";
		}

		SmallImage = _toggleState == false ? _iconOff16 : _iconOn16;
		LargeImage = _toggleState == false ? _iconOff32 : _iconOn32;
	}

	private void ToggleLayerMasking(Map map, bool turnOn)
	{
		if (map is null)
			throw new ArgumentNullException(nameof(map));

		var undoStack = map.OperationManager;
		if (undoStack is null)
			throw new InvalidOperationException("Map's undo/redo stack is null");

		var name = Caption; // use command's caption as operation name
		if (string.IsNullOrEmpty(name))
		{
			name = $"Turn LM {(turnOn ? "on" : "off")}";
		}

		Gateway.CompositeOperation(
			undoStack, name,
			() => DisplayUtils.ToggleLayerMasking(map, turnOn));
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
