using System;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

/// <summary>
/// Shared state for Halos of Annotation layer and convenient entry points.
/// Must be singleton! Must be thread-safe!
/// </summary>
public sealed class Halos
{
	//private readonly object _syncRoot = new();
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	#region Singleton

	private static volatile Halos _instance;
	private static readonly object _instanceLock = new();

	private Halos() { } // private to prevent outside instantiation

	public static Halos Instance
	{
		get
		{
			if (_instance is null) // performance optimization
			{
				lock (_instanceLock) // mutual exclusion
				{
					if (_instance is null)
					{
						_instance = new Halos();
					}
				}
			}

			return _instance;
		}
	}

	#endregion

	public bool? ToggleState { get; private set; } // initially unknown

	/// <summary>
	/// Initialize halo toggling functionality.
	/// Call from your module's initialization.
	/// </summary>
	public void Initialize()
	{
		WireEvents();
	}

	public void Shutdown()
	{
		UnwireEvents();
	}

	public bool ToggleHalo(Map map, SymbolSubstitutionType symbolSubstitutionType)
	{
		if (map is null)
			throw new ArgumentNullException(nameof(map));

		bool toggled = false;

		var annotationLayers = map.GetLayersAsFlattenedList()
		                          .OfType<AnnotationLayer>().ToList();

		foreach (AnnotationLayer annotationLayer in annotationLayers)
		{
			var cimBaseLayer = annotationLayer.GetDefinition();
			if (cimBaseLayer is CIMAnnotationLayer cimAnnotationLayer)
			{
				if (cimAnnotationLayer.SymbolSubstitutionType != symbolSubstitutionType)
				{
					cimAnnotationLayer.SymbolSubstitutionType = symbolSubstitutionType;
					annotationLayer.SetDefinition(cimAnnotationLayer);
					toggled = true;
				}
			}
		}

		return toggled;
	}

	private bool InitializeHalos(Map map)
	{
		ToggleState = GetInitialState(map);

		var substitutionType = ToggleState ?? false
			                       ? SymbolSubstitutionType.IndividualSubordinate
			                       : SymbolSubstitutionType.None;

		bool toggled = ToggleHalo(map, substitutionType);

		return toggled;
	}

	public static bool? GetInitialState(Map map)
	{
		if (map is null) return null;

		AnnotationLayer annotationLayer = map.GetLayersAsFlattenedList()
		                                     .OfType<AnnotationLayer>().FirstOrDefault();
		// TODO consider doing a "majority vote" instead of sampling just first layer

		CIMBaseLayer cimBaseLayer = annotationLayer?.GetDefinition();
		if (cimBaseLayer is CIMAnnotationLayer cimAnnotationLayer)
		{
			SymbolSubstitutionType symbolSubstitutionType =
				cimAnnotationLayer.SymbolSubstitutionType;
			if (symbolSubstitutionType == SymbolSubstitutionType.IndividualSubordinate)
			{
				return true;
			}
		}

		return false;
	}

	#region Event handling

	private SubscriptionToken _mapViewInitializedToken;
	private SubscriptionToken _activeMapViewChangedToken;

	private void WireEvents()
	{
		_mapViewInitializedToken ??= MapViewInitializedEvent.Subscribe(OnMapViewInitialized);

		_activeMapViewChangedToken ??= ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);
	}

	private void UnwireEvents()
	{
		if (_mapViewInitializedToken != null)
		{
			MapViewInitializedEvent.Unsubscribe(_mapViewInitializedToken);
			_mapViewInitializedToken = null;
		}

		if (_activeMapViewChangedToken != null)
		{
			ActiveMapViewChangedEvent.Unsubscribe(_activeMapViewChangedToken);
			_activeMapViewChangedToken = null;
		}
	}

	/// <remarks>May be called more than once per map</remarks>
	private async void OnMapViewInitialized(MapViewEventArgs args)
	{
		try
		{
			var map = args.MapView?.Map;
			if (map is null) return;

			await QueuedTask.Run(() =>
			{
				if (InitializeHalos(map))
				{
					//args.MapView.Redraw(false);
				}
			});
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg, nameof(OnMapViewInitialized));
		}
	}

	private async void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
	{
		try
		{
			var incomingMap = args.IncomingView?.Map;
			if (incomingMap is null) return;

			await QueuedTask.Run(() =>
			{
				if (InitializeHalos(incomingMap))
				{
					//	args.MapView.Redraw(false); 
				}
			});
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg, nameof(OnActiveMapViewChanged));
		}
	}

	#endregion
}
