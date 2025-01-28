using System;
using System.Collections.Generic;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

public class SymbolLayerDisplayManager : ISymbolLayerDisplay
{
	private readonly Dictionary<string, bool> _sldState = new();
	private readonly Dictionary<string, bool> _lmState = new();
	private readonly Dictionary<string, Settings> _settingsByMap = new();

	private SubscriptionToken _activeMapViewChangedToken;
	private SubscriptionToken _mapViewCameraChangedToken;
	private SubscriptionToken _projectClosedToken;

	private readonly Settings _defaultSettings;
	private Settings _settings;
	private double _lastScaleDenom;

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	#region Singleton

	private static volatile SymbolLayerDisplayManager _instance;
	private static readonly object _instanceLock = new();

	private SymbolLayerDisplayManager() // private to prevent outside instantiation
	{
		_settings = _defaultSettings = new Settings();
		_lastScaleDenom = double.NaN;
	}

	public static SymbolLayerDisplayManager Instance
	{
		get
		{
			if (_instance is null) // performance optimization
			{
				lock (_instanceLock) // mutual exclusion
				{
					if (_instance is null)
					{
						_instance = new SymbolLayerDisplayManager();
					}
				}
			}

			return _instance;
		}
	}

	#endregion

	public bool NoMaskingWithoutSLD
	{
		get => _settings.NoMaskingWithoutSLD;
		set => _settings.NoMaskingWithoutSLD = value;
	}

	public bool AutoSwitch
	{
		get => _settings.AutoSwitch;
		set => _settings.AutoSwitch = value;
	}

	public double AutoMinScaleDenom
	{
		get => _settings.AutoMinScaleDenom;
		set => _settings.AutoMinScaleDenom = value;
	}

	public double AutoMaxScaleDenom
	{
		get => _settings.AutoMaxScaleDenom;
		set => _settings.AutoMaxScaleDenom = value;
	}

	public void Initialize()
	{
		_activeMapViewChangedToken ??= ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);
		_mapViewCameraChangedToken ??= MapViewCameraChangedEvent.Subscribe(OnMapViewCameraChanged);
		_projectClosedToken ??= ProjectClosedEvent.Subscribe(OnProjectClosed);
	}

	public void Shutdown()
	{
		if (_activeMapViewChangedToken is not null)
		{
			ActiveMapViewChangedEvent.Unsubscribe(_activeMapViewChangedToken);
		}

		if (_mapViewCameraChangedToken is not null)
		{
			MapViewCameraChangedEvent.Unsubscribe(_mapViewCameraChangedToken);
		}

		if (_projectClosedToken is not null)
		{
			ProjectClosedEvent.Unsubscribe(_projectClosedToken);
		}

		ClearStateCache();
	}

	#region Event handling

	private void OnProjectClosed(ProjectEventArgs obj)
	{
		try
		{
			ClearStateCache();
			ClearSettings();
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg, nameof(OnProjectClosed));
		}
	}

	private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
	{
		try
		{
			var activeMap = MapView.Active?.Map;
			// TODO vs args.IncomingView?!
			var map = args.IncomingView?.Map;

			SwitchSettings(map);
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg, nameof(OnActiveMapViewChanged));
		}
	}

	private void OnMapViewCameraChanged(MapViewCameraChangedEventArgs args)
	{
		try
		{
			var camera = args.CurrentCamera;
			if (camera is null) return;

			var currentScaleDenom = camera.Scale;

			// Note: args.MapView is "ahead" of MapView.Active (the latter may still be null)
			var map = args.MapView?.Map;
			if (map is null) return;

			QueuedTask.Run(() => ScaleChanged(map, currentScaleDenom));
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg, nameof(OnMapViewCameraChanged));
		}
	}

	#endregion

	public bool? QuickUsesSLD(Map map)
	{
		if (map is null) return false;
		if (map.URI is null) return null; // paranoia
		return _sldState.TryGetValue(map.URI, out bool enabled) ? enabled : null;
	}

	/// <remarks>Must run on MCT</remarks>
	public bool UsesSLD(Map map, bool uncached = false)
	{
		if (map is null) return false;

		if (! uncached && _sldState.TryGetValue(map.URI, out bool enabled))
		{
			return enabled;
		}

		enabled = DisplayUtils.UsesSLD(map);

		_sldState[map.URI] = enabled;

		return enabled;
	}

	/// <summary>
	/// Toggle, enable, or disable SLD on the given map.
	/// May also toggle LM, depending on <see cref="NoMaskingWithoutSLD"/>.
	/// </summary>
	/// <returns>true iff any layer or the map was modified</returns>
	/// <remarks>Must run on MCT</remarks>
	public bool ToggleSLD(Map map, bool? enable = null)
	{
		bool usesSLD = UsesSLD(map);
		bool turnOn = enable ?? ! usesSLD;

		bool modified = false;

		if (turnOn != usesSLD)
		{
			modified = DisplayUtils.ToggleSymbolLayerDrawing(map, turnOn);
		}

		_sldState[map.URI] = turnOn; // cache for performance

		if (NoMaskingWithoutSLD && !turnOn && UsesLM(map))
		{
			modified |= ToggleLM(map, false);
		}

		return modified;
	}

	public bool? QuickUsesLM(Map map)
	{
		if (map is null) return false;
		if (map.URI is null) return null; // paranoia
		return _lmState.TryGetValue(map.URI, out bool enabled) ? enabled : null;
	}

	/// <remarks>Must run on MCT</remarks>
	public bool UsesLM(Map map, bool uncached = false)
	{
		if (map is null) return false;

		if (! uncached && _lmState.TryGetValue(map.URI, out bool enabled))
		{
			return enabled;
		}

		enabled = DisplayUtils.UsesLayerMasking(map) ?? false;

		_lmState[map.URI] = enabled;

		return enabled;
	}

	/// <summary>
	/// Toggle, enable, or disable LM on the given map.
	/// May also toggle SLD, depending on <see cref="NoMaskingWithoutSLD"/>.
	/// </summary>
	/// <returns>true iff any layer or the map was modified</returns>
	/// <remarks>Must run on MCT</remarks>
	public bool ToggleLM(Map map, bool? enable = null)
	{
		bool usesLM = UsesLM(map);
		bool turnOn = enable ?? ! usesLM;

		bool modified = false;

		if (turnOn != usesLM)
		{
			modified = DisplayUtils.ToggleLayerMasking(map, turnOn);
		}

		_lmState[map.URI] = turnOn; // cache for performance

		if (NoMaskingWithoutSLD && turnOn && ! UsesSLD(map))
		{
			modified |= ToggleSLD(map, true);
		}

		return modified;
	}

	#region Private methods

	/// <remarks>Must run on MCT</remarks>
	private void ScaleChanged(Map map, double currentScaleDenom)
	{
		if (!AutoSwitch) return; // nothing to do

		if (double.IsNaN(currentScaleDenom)) return;

		var delta = _lastScaleDenom - currentScaleDenom;
		if (Math.Abs(delta) < double.Epsilon) return; // no change

		_lastScaleDenom = currentScaleDenom;

		var min = AutoMinScaleDenom;
		if (!(min > 0)) min = 0;

		var max = AutoMaxScaleDenom;
		if (!(max > 0)) max = double.MaxValue;

		bool wantOn = min <= currentScaleDenom && currentScaleDenom <= max;

		if (wantOn)
		{
			if (!UsesSLD(map))
			{
				ToggleSLD(map, true);
			}

			if (!UsesLM(map))
			{
				ToggleLM(map, true);
			}
		}
		else
		{
			if (UsesLM(map))
			{
				ToggleLM(map, false);
			}

			if (UsesSLD(map))
			{
				ToggleSLD(map, false);
			}
		}
	}

	private void SwitchSettings([CanBeNull] Map activeMap)
	{
		if (activeMap is null)
		{
			_settings = _defaultSettings;
		}
		else
		{
			if (activeMap.URI is null)
				throw new InvalidOperationException("Map has no URI");

			if (!_settingsByMap.TryGetValue(activeMap.URI, out var settings))
			{
				settings = new(_defaultSettings);
				_settingsByMap.Add(activeMap.URI, settings);
			}

			_settings = settings;
		}
	}

	private void ClearSettings()
	{
		_settingsByMap.Clear();
		_settings = _defaultSettings;
	}

	private void ClearStateCache()
	{
		_sldState.Clear();
		_lmState.Clear();
	}

	#endregion

	#region Nested type: Settings

	private class Settings
	{
		public bool AutoSwitch { get; set; }
		public double AutoMinScaleDenom { get; set; }
		public double AutoMaxScaleDenom { get; set; }

		public bool NoMaskingWithoutSLD { get; set; }

		public Settings() { }

		public Settings(Settings settings)
		{
			AutoSwitch = settings.AutoSwitch;
			AutoMinScaleDenom = settings.AutoMinScaleDenom;
			AutoMaxScaleDenom = settings.AutoMaxScaleDenom;
			NoMaskingWithoutSLD = settings.NoMaskingWithoutSLD;
		}
	}

	#endregion
}
