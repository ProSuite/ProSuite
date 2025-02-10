using System;
using System.Collections.Generic;
using System.Reflection;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

public class SymbolDisplayManager : ISymbolDisplayManager
{
	private readonly Dictionary<string, bool> _sldStateCache = new();
	private readonly Dictionary<string, bool> _lmStateCache = new();
	private readonly Dictionary<string, SymbolDisplaySettings> _settingsByMap = new();

	private SubscriptionToken _mapViewCameraChangedToken;
	private SubscriptionToken _projectClosedToken;

	private SymbolDisplaySettings _defaultSettings;
	private double _lastScaleDenom;

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	#region Singleton

	private static volatile SymbolDisplayManager _instance;
	private static readonly object _instanceLock = new();

	private SymbolDisplayManager() // private to prevent outside instantiation
	{
		_defaultSettings = new SymbolDisplaySettings();
		_lastScaleDenom = double.NaN;

		NoMaskingWithoutSLD = new IndexedProperty<bool>(
			nameof(SymbolDisplaySettings.NoMaskingWithoutSLD), _settingsByMap, GetDefaults);

		AutoSwitch = new IndexedProperty<bool>(
			nameof(SymbolDisplaySettings.AutoSwitch), _settingsByMap, GetDefaults);
		AutoMinScaleDenom = new IndexedProperty<double>(
			nameof(SymbolDisplaySettings.AutoMinScaleDenom), _settingsByMap, GetDefaults);
		AutoMaxScaleDenom = new IndexedProperty<double>(
			nameof(SymbolDisplaySettings.AutoMaxScaleDenom), _settingsByMap, GetDefaults);
	}

	private SymbolDisplaySettings GetDefaults() => _defaultSettings;

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

	// Settings:
	// write: set current and default
	// read: get current (or default)

	public IIndexedProperty<Map, bool> NoMaskingWithoutSLD { get; }

	public IIndexedProperty<Map, bool> AutoSwitch { get; }
	public IIndexedProperty<Map, double> AutoMinScaleDenom { get; }
	public IIndexedProperty<Map, double> AutoMaxScaleDenom { get; }

	public void Initialize(SymbolDisplaySettings settings)
	{
		if (settings is not null)
		{
			_defaultSettings = settings;
		}

		_mapViewCameraChangedToken ??= MapViewCameraChangedEvent.Subscribe(OnMapViewCameraChanged);
		_projectClosedToken ??= ProjectClosedEvent.Subscribe(OnProjectClosed);
	}

	public void Shutdown()
	{
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
		return _sldStateCache.TryGetValue(map.URI, out bool enabled) ? enabled : null;
	}

	/// <remarks>Must run on MCT</remarks>
	public bool UsesSLD(Map map, bool uncached = false)
	{
		if (map is null) return false;

		if (! uncached && _sldStateCache.TryGetValue(map.URI, out bool enabled))
		{
			return enabled;
		}

		enabled = DisplayUtils.UsesSLD(map);

		_sldStateCache[map.URI] = enabled;

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

		_sldStateCache[map.URI] = turnOn; // cache for performance

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
		return _lmStateCache.TryGetValue(map.URI, out bool enabled) ? enabled : null;
	}

	/// <remarks>Must run on MCT</remarks>
	public bool UsesLM(Map map, bool uncached = false)
	{
		if (map is null) return false;

		if (! uncached && _lmStateCache.TryGetValue(map.URI, out bool enabled))
		{
			return enabled;
		}

		enabled = DisplayUtils.UsesLayerMasking(map) ?? false;

		_lmStateCache[map.URI] = enabled;

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

		_lmStateCache[map.URI] = turnOn; // cache for performance

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

	private void ClearSettings()
	{
		_settingsByMap.Clear();
	}

	private void ClearStateCache()
	{
		_sldStateCache.Clear();
		_lmStateCache.Clear();
	}

	#endregion

	#region Nested type: IndexedProperty

	private class IndexedProperty<T> : IIndexedProperty<Map, T>
	{
		private readonly string _propertyName;
		private readonly Dictionary<string, SymbolDisplaySettings> _settings;
		private readonly Func<SymbolDisplaySettings> _getDefaults;

		public IndexedProperty(
			string propertyName, Dictionary<string, SymbolDisplaySettings> settings, Func<SymbolDisplaySettings> getDefaults)
		{
			_propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
			_getDefaults = getDefaults ?? throw new ArgumentNullException(nameof(getDefaults));
		}

		public T this[Map map]
		{
			get
			{
				if (map is null) return GetValue(_getDefaults());
				if (map.URI is null) throw MapHasNoUri();
				return GetValue(_settings.GetValueOrDefault(map.URI, _getDefaults()));
			}
			set
			{
				if (map is not null)
				{
					if (map.URI is null) throw MapHasNoUri();

					if (!_settings.TryGetValue(map.URI, out var settings))
					{
						settings = new SymbolDisplaySettings(_getDefaults());
						_settings.Add(map.URI, settings);
					}

					SetValue(settings, value);
				}

				SetValue(_getDefaults(), value);
			}
		}

		private T GetValue(SymbolDisplaySettings settings)
		{
			if (settings is null) return default;

			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			var type = settings.GetType();
			var property = type.GetProperty(_propertyName, flags)
						   ?? throw PropertyNotFound();
			return (T)property.GetValue(settings);
		}

		private void SetValue(SymbolDisplaySettings settings, T value)
		{
			if (settings is null) return; // no-op

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
