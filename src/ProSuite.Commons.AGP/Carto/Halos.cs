using ArcGIS.Core.CIM;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Events;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProSuite.Commons.AGP.Carto
{
	/// <summary>
	/// Shared state for Halos of Annotation layer and convenient entry points.
	/// Must be singleton! Must be thread-safe!
	/// </summary>
	public sealed class Halos
	{
		private readonly object _syncRoot;
		private bool? _toggleState; // initially unknown
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Singleton

		private static volatile Halos _instance;
		private static readonly object _instanceLock = new();

		private Halos() // private to prevent outside instantiation
		{
			_syncRoot = new object();
		}

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

		public bool ToggleState
		{
			get { return _toggleState.Value; }
		}

		#endregion

		/// <summary>
		/// Initialize dangles functionality.
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

		public bool ToggleHalo(SymbolSubstitutionType symbolSubstitutionType, Map map = null)
		{
			bool toggled = false;
			try
			{
				map ??= MapView.Active?.Map;
				if (map is null) return false;

				IEnumerable<AnnotationLayer> annotationLayers = map.GetLayersAsFlattenedList().OfType<AnnotationLayer>();
				foreach (AnnotationLayer annotationLayer in annotationLayers)
				{
					CIMAnnotationLayer cimAnnotationLayer = annotationLayer.GetDefinition() as CIMAnnotationLayer;
					if (cimAnnotationLayer != null)
					{
						if (cimAnnotationLayer.SymbolSubstitutionType != symbolSubstitutionType)
						{
							cimAnnotationLayer.SymbolSubstitutionType = symbolSubstitutionType;
							annotationLayer.SetDefinition(cimAnnotationLayer);
							toggled = true;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Gateway.LogError(ex, _msg);
			}

			return toggled;
		}


		//private async Task<bool> InitializeHalos(Map map = null)
		private bool InitializeHalos(Map map = null)
		{
			try
			{
				map ??= MapView.Active?.Map;
				if (map is null) return false;

				//_toggleState = await QueuedTask.Run(() => GetInitialState(map));

				//QueuedTask.Run(() => ToggleHalo(_toggleState.Value
				//	                                ? SymbolSubstitutionType.IndividualSubordinate
				//	                                : SymbolSubstitutionType.None));
				lock (_syncRoot)
				{
					_toggleState = GetInitialState(map);
					bool toggled = ToggleHalo(_toggleState.Value
						                          ? SymbolSubstitutionType.IndividualSubordinate
						                          : SymbolSubstitutionType.None);
					return toggled;
				}
			}
			catch (Exception ex)
			{
				Gateway.LogError(ex, _msg);
			}

			return false;
		}

		public bool GetInitialState(Map map)
		{
			AnnotationLayer annotationLayer = map.GetLayersAsFlattenedList()
			                                     .OfType<AnnotationLayer>().FirstOrDefault();

			CIMBaseLayer cimBaseLayer = annotationLayer?.GetDefinition();
			if (cimBaseLayer is CIMAnnotationLayer cimAnnotationLayer)
			{
				SymbolSubstitutionType symbolSubstitutionType = cimAnnotationLayer.SymbolSubstitutionType;
				if (symbolSubstitutionType == SymbolSubstitutionType.IndividualSubordinate)
				{
					return true;
				}
			}
			return false;
		}


		#region Event handling

		// project opened: initialize dangle tables (that may have been created before)
		// project closed: invalidate cache
		// active map view changed: invalidate (since we work on active map, not a particular map)
		// map removed: invalidate cache
		// layer modified: nothing
		// layers added: nothing
		// TODO layers removed: what if a dangles source layer is removed? dangles layer removed?

		private SubscriptionToken _projectOpenedToken;
		private SubscriptionToken _projectClosedToken;
		private SubscriptionToken _mapViewInitializedToken;
		private SubscriptionToken _activeMapViewChangedToken;
		private SubscriptionToken _mapRemovedToken;

		private void WireEvents()
		{
			if (_projectOpenedToken is null)
			{
				_projectOpenedToken = ProjectOpenedEvent.Subscribe(OnProjectOpened);
			}

			if (_projectClosedToken is null)
			{
				_projectClosedToken = ProjectClosedEvent.Subscribe(OnProjectClosed);
			}

			if (_mapViewInitializedToken is null)
			{
				MapViewInitializedEvent.Subscribe(OnMapViewInitialized);
			}

			if (_activeMapViewChangedToken is null)
			{
				_activeMapViewChangedToken =
					ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);
			}

			if (_mapRemovedToken is null)
			{
				_mapRemovedToken = MapRemovedEvent.Subscribe(OnMapRemoved);
			}
		}

		private void UnwireEvents()
		{
			if (_projectOpenedToken != null)
			{
				ProjectOpenedEvent.Unsubscribe(_projectOpenedToken);
				_projectOpenedToken = null;
			}

			if (_projectClosedToken != null)
			{
				ProjectClosedEvent.Unsubscribe(_projectClosedToken);
				_projectClosedToken = null;
			}

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

			if (_mapRemovedToken != null)
			{
				MapRemovedEvent.Unsubscribe(_mapRemovedToken);
				_mapRemovedToken = null;
			}
		}

		private void OnProjectOpened(ProjectEventArgs args)
		{ // Note: occurs before any maps are opened
			try
			{
			}
			catch (Exception ex)
			{
				_msg.Error($"{nameof(OnProjectOpened)}: {ex.Message}", ex);
			}
		}

		private void OnProjectClosed(ProjectEventArgs args)
		{
			try
			{
			}
			catch (Exception ex)
			{
				_msg.Error($"{nameof(OnProjectClosed)}: {ex.Message}", ex);
			}
		}

		private async void OnMapViewInitialized(MapViewEventArgs args)
		{ // Note: may be called more than once per map!
			try
			{
				var map = args.MapView?.Map;
				if (map is null) return;

				await QueuedTask.Run(() =>
				{
					//InitializeHalos(map);
					//args.MapView.Redraw(false); // TODO clearCache: true/false
				});
			}
			catch (Exception ex)
			{
				_msg.Error($"{nameof(OnMapViewInitialized)}: {ex.Message}", ex);
			}
		}

		private async void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
		{ // Note: also called when changing from Map Pane to Attribute Table and vice versa
			try
			{
				var incoming = args.IncomingView?.Map;
				if (incoming != null)
				{
					await QueuedTask.Run(() =>
					{
						if (InitializeHalos(incoming))
						{
							//	args.MapView.Redraw(false); 
						}
					});

				}

				var outgoing = args.OutgoingView?.Map;
				if (outgoing != null)
				{

				}
			}
			catch (Exception ex)
			{
				_msg.Error($"{nameof(OnActiveMapViewChanged)}: {ex.Message}", ex);
			}
		}

		private void OnMapRemoved(MapRemovedEventArgs args)
		{
			try
			{
				var mapUri = args.MapPath; // empirical

			}
			catch (Exception ex)
			{
				_msg.Error($"{nameof(OnMapRemoved)}: {ex.Message}", ex);
			}
		}

		#endregion

	}
}
