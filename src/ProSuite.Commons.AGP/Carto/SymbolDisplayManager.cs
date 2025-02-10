using System;
using System.Collections.Generic;
using System.Reflection;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

public interface IIndexedProperty<in TKey, TValue>
{
	TValue this[TKey key] { get; set; }
}

public class SymbolDisplayManager : ISymbolDisplayManager
{
	private readonly Dictionary<string, bool> _sldState = new();
	private readonly Dictionary<string, bool> _lmState = new();
	private readonly Dictionary<string, Settings> _settingsByMap = new();

	private SubscriptionToken _activeMapViewChangedToken;
	private SubscriptionToken _mapViewCameraChangedToken;
	private SubscriptionToken _projectClosedToken;

	private readonly Settings _defaultSettings;
	//private Settings _settings;
	private double _lastScaleDenom;

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	#region Singleton

	private static volatile SymbolDisplayManager _instance;
	private static readonly object _instanceLock = new();

	private SymbolDisplayManager() // private to prevent outside instantiation
	{
		_defaultSettings = new Settings();
		_lastScaleDenom = double.NaN;

		NoMaskingWithoutSLD = new IndexedProperty<bool>(_settingsByMap, _defaultSettings,
		                                                nameof(Settings.NoMaskingWithoutSLD));

		AutoSwitch = new IndexedProperty<bool>(_settingsByMap, _defaultSettings,
		                                       nameof(Settings.AutoSwitch));
		AutoMinScaleDenom = new IndexedProperty<double>(_settingsByMap, _defaultSettings,
		                                                nameof(Settings.AutoMinScaleDenom));
		AutoMaxScaleDenom = new IndexedProperty<double>(_settingsByMap, _defaultSettings,
		                                                nameof(Settings.AutoMaxScaleDenom));
	}

	public static SymbolDisplayManager Instance
	{
		get
		{
			if (_instance is null) // performance optimization
			{
				lock (_instanceLock) // mutual exclusion
				{
					if (_instance is null)
					{
						_instance = new SymbolDisplayManager();
					}
				}
			}

			return _instance;
		}
	}

	#endregion

	public IIndexedProperty<Map, bool> NoMaskingWithoutSLD { get; }

	public IIndexedProperty<Map, bool> AutoSwitch { get; }
	public IIndexedProperty<Map, double> AutoMinScaleDenom { get; }
	public IIndexedProperty<Map, double> AutoMaxScaleDenom { get; }

	//public bool NoMaskingWithoutSLD
	//{
	//	get => _settings.NoMaskingWithoutSLD;
	//	set => _settings.NoMaskingWithoutSLD = value;
	//}

	//public bool AutoSwitch
	//{
	//	get => _settings.AutoSwitch;
	//	set => _settings.AutoSwitch = value;
	//}

	//public double AutoMinScaleDenom
	//{
	//	get => _settings.AutoMinScaleDenom;
	//	set => _settings.AutoMinScaleDenom = value;
	//}

	//public double AutoMaxScaleDenom
	//{
	//	get => _settings.AutoMaxScaleDenom;
	//	set => _settings.AutoMaxScaleDenom = value;
	//}

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
			var map = args.IncomingView?.Map;

			//SwitchSettings(map);
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

		bool noMaskingWithoutSLD = NoMaskingWithoutSLD[map];

		if (noMaskingWithoutSLD && !turnOn && UsesLM(map))
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

		bool noMaskingWithoutSLD = NoMaskingWithoutSLD[map];

		if (noMaskingWithoutSLD && turnOn && ! UsesSLD(map))
		{
			modified |= ToggleSLD(map, true);
		}

		return modified;
	}

	#region Private methods

	/// <remarks>Must run on MCT</remarks>
	private void ScaleChanged(Map map, double currentScaleDenom)
	{
		var autoSwitch = AutoSwitch[map];
		if (! autoSwitch) return; // nothing to do

		if (double.IsNaN(currentScaleDenom)) return;

		var delta = _lastScaleDenom - currentScaleDenom;
		if (Math.Abs(delta) < double.Epsilon) return; // no change

		_lastScaleDenom = currentScaleDenom;

		var min = AutoMinScaleDenom[map];
		if (!(min > 0)) min = 0;

		var max = AutoMaxScaleDenom[map];
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
			//_currentSettings = _defaultSettings;
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

			//_currentSettings = settings;
		}
	}

	private void ClearSettings()
	{
		_settingsByMap.Clear();
		//_currentSettings = _defaultSettings;
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

	#region Nested type: IndexedProperty

	private class IndexedProperty<T> : IIndexedProperty<Map, T>
	{
		private readonly string _propertyName;
		private readonly Dictionary<string, Settings> _settings;
		private readonly Settings _defaultSettings;

		public IndexedProperty(Dictionary<string, Settings> settings, Settings defaultSettings, string propertyName)
		{
			_settings = settings;
			_defaultSettings = defaultSettings;
			_propertyName = propertyName;
		}

		public T this[Map map]
		{
			get
			{
				if (map is null) return GetValue(_defaultSettings);
				if (map.URI is null) throw MapHasNoUri();
				return GetValue(_settings.GetValueOrDefault(map.URI, _defaultSettings));
			}
			set
			{
				if (map is null)
				{
					SetValue(_defaultSettings, value);
				}
				else
				{
					if (map.URI is null) throw MapHasNoUri();
					if (!_settings.TryGetValue(map.URI, out var settings))
					{
						settings = new Settings(_defaultSettings);
						_settings.Add(map.URI, settings);
					}

					SetValue(settings, value);
				}
			}
		}

		private T GetValue(Settings settings)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			var type = settings.GetType();
			var property = type.GetProperty(_propertyName, flags)
			               ?? throw PropertyNotFound();
			return (T) property.GetValue(settings);
		}

		private void SetValue(Settings settings, T value)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			var type = settings.GetType();
			var property = type.GetProperty(_propertyName, flags)
			               ?? throw new InvalidOperationException(
				               $"No such property: {_propertyName}");
			property.SetValue(settings, value);
		}

		private InvalidOperationException PropertyNotFound()
		{
			return new InvalidOperationException($"Property not found: {_propertyName}");
		}

		private static ArgumentException MapHasNoUri()
		{
			return new ArgumentException("Map has no URI");
		}
	}

	#endregion
}
