using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Mapping;
using ArcGIS.Core.Events;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Editing;
using ArcGIS.Desktop.Internal.Framework.Events;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.UI;
using SketchMode = ArcGIS.Desktop.Mapping.SketchMode;

namespace ProSuite.AGP.Editing.Annotation;

public abstract class AnnotationConstructionToolBase : MapTool
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();
	private SubscriptionToken _onActivePaneChangingToken;
	private SubscriptionToken _onActiveWindowChangedToken;
	private SubscriptionToken _onActiveToolChangingToken;
	private SubscriptionToken _onActiveToolChangedToken;
	private SubscriptionToken _onSketchModifiedToken;
	private SubscriptionToken _onBeforeSketchCompletedToken;
	private SubscriptionToken _onSketchCompletedToken;
	private SubscriptionToken _onDrawCompletedToken;

	protected AnnotationConstructionToolBase()
	{
		IsSketchTool = true;
		UseSnapping = true;
		// set the sketch type to line
		SketchType = SketchGeometryType.Line;
		
		//SketchOutputMode = SketchOutputMode.Map;
		// needed to call OnSelectionChangedAsync
		UseSelection = true;
		IsWYSIWYG = true;
		FireSketchEvents = true;
	}

	protected override Task OnToolActivateAsync(bool hasMapViewChanged)
	{
		if (_onActivePaneChangingToken == null)
		{
			_onActivePaneChangingToken = ActivePaneChangingEvent.Subscribe(OnActivePaneChanging);
		}

		if (_onActiveWindowChangedToken == null)
		{
			_onActiveWindowChangedToken = ActiveWindowChangedEvent.Subscribe(OnActiveWindowChanged);
		}

		if (_onActiveToolChangingToken == null)
		{
			_onActiveToolChangingToken = ActiveToolChangingEvent.Subscribe(OnActiveToolChanging);
		}

		if (_onActiveToolChangedToken == null)
		{
			_onActiveToolChangedToken = ActiveToolChangedEvent.Subscribe(OnActiveToolChanged);
		}

		if (_onSketchModifiedToken == null)
		{
			_onSketchModifiedToken = SketchModifiedEvent.Subscribe(OnSketchModified);
		}

		if (_onBeforeSketchCompletedToken == null)
		{
			_onBeforeSketchCompletedToken = BeforeSketchCompletedEvent.Subscribe(OnBeforeSketchCompleted);
		}

		if (_onSketchCompletedToken == null)
		{
			_onSketchCompletedToken = SketchCompletedEvent.Subscribe(OnSketchCompleted);
		}

		if (_onDrawCompletedToken == null)
		{
			_onDrawCompletedToken = DrawCompleteEvent.Subscribe(OnDrawCompletedEvent);
		}

		return base.OnToolActivateAsync(hasMapViewChanged);
	}

	private void OnDrawCompletedEvent(MapViewEventArgs obj)
	{
		_msg.Debug($"{nameof(OnDrawCompletedEvent)} {MapView.Active.GetSketchType()} {SketchMode}");
	}

	private void OnSketchCompleted(SketchCompletedEventArgs args)
	{
		_msg.Debug($"{nameof(OnSketchCompleted)} {MapView.Active.GetSketchType()} {SketchMode}");
	}

	private async Task OnBeforeSketchCompleted(BeforeSketchCompletedEventArgs args)
	{
		_msg.Debug($"{nameof(OnBeforeSketchCompleted)} {MapView.Active.GetSketchType()} {SketchMode}");

		await Task.FromResult(0);
	}

	private void OnSketchModified(SketchModifiedEventArgs args)
	{
		_msg.Debug($"{nameof(OnSketchModified)} {MapView.Active.GetSketchType()} {SketchMode}");
	}

	protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
	{
		return base.OnToolDeactivateAsync(hasMapViewChanged);
	}

	protected override async Task OnSelectionChangedAsync(MapSelectionChangedEventArgs args)
	{
		// https://github.com/Esri/arcgis-pro-sdk/wiki/ProConcepts-Editing#useselection
		if (! MapUtils.HasSelection(ActiveMapView.Map))
		{
			await base.OnSelectionChangedAsync(args);
		}

		// add predicate?
		// returns no feataures?!?
		//var selectedAnnotationFeatures = SelectionUtils.GetSelectedFeatures(annotationLayer)

		// todo only AnnoLayers
		foreach (var pair in args.Selection.ToDictionary<AnnotationLayer>())
		{
			AnnotationLayer layer = pair.Key;
			List<long> oids = pair.Value;

			foreach (AnnotationFeature annotationFeature in
			         LayerUtils.SearchRows<AnnotationFeature>(
				         layer, GdbQueryUtils.CreateFilter(oids)))
			{
				if (annotationFeature.GetGraphic() is not CIMTextGraphic cimTextGraphic)
				{
					continue;
				}

				// adds text graphic
				CIMGraphic cimGraphic = annotationFeature.GetGraphic();
				MapView.Active.AddOverlay(cimGraphic, 50000);

				// adds base line
				SketchSymbol = cimTextGraphic.Symbol;
				await SetCurrentSketchAsync(cimTextGraphic.Shape);

				SketchMode = SketchMode.VertexMove;
			}
		}

		await base.OnSelectionChangedAsync(args);
	}

	protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
	{
		if (geometry == null || geometry.IsEmpty)
		{
			return false;
		}

		Task<long> task = QueuedTask.Run(() =>
		{
			// todo daro ToList() needed?
			IEnumerable<FeatureSelectionBase> featureSelection = FindSelectedFeatures(geometry).ToList();

			return SelectionUtils.SelectFeatures(featureSelection,
			                                     SelectionCombinationMethod.New,
			                                     clearExistingSelection: true);
		});

		long selectionCount = await ViewUtils.TryAsync(task, _msg);

		if (selectionCount == 1)
		{
			return true;
		}
		return false;
	}

	protected override void OnToolKeyDown(MapViewKeyEventArgs args)
	{
		base.OnToolKeyDown(args);
	}

	protected override void OnToolMouseMove(MapViewMouseEventArgs args)
	{
		base.OnToolMouseMove(args);
	}

	protected override bool? OnActivePaneChanged(Pane pane)
	{
		return base.OnActivePaneChanged(pane);
	}

	#region display feedback

	[CanBeNull]
	private static IDisposable AddOverlay([CanBeNull] Geometry geometry,
	                                      [NotNull] CIMTextSymbol cimSymbol)
	{
		if (geometry == null || geometry.IsEmpty)
		{
			return null;
		}

		return MapView.Active.AddOverlay(geometry, cimSymbol.MakeSymbolReference(), 50000);
	}

	#endregion

	#region base

	private IEnumerable<FeatureSelectionBase> FindSelectedFeatures(
		Geometry geometry,
		SpatialRelationship spatialRelationship = SpatialRelationship.Intersects)
	{
		var featureFinder = new FeatureFinder(ActiveMapView)
		                    {
			                    SpatialRelationship = spatialRelationship,
			                    DelayFeatureFetching = true
		                    };

		IEnumerable<FeatureSelectionBase> featureSelection =
			featureFinder.FindFeaturesByLayer(
				geometry,
				fl => CanSelectFromLayer(fl));

		// todo daro inline
		return featureSelection;
	}

	protected bool CanUseSelection([NotNull] MapView activeMapView)
	{
		Dictionary<MapMember, List<long>> selectionByLayer =
			SelectionUtils.GetSelection(activeMapView.Map);

		return CanUseSelection(selectionByLayer);
	}

	protected bool CanUseSelection([NotNull] Dictionary<MapMember, List<long>> selectionByLayer)
	{
		return selectionByLayer.All(l => CanSelectFromLayer(l.Key as Layer));
	}

	private bool CanSelectFromLayer([CanBeNull] Layer layer,
	                                NotificationCollection notifications = null)
	{
		var basicFeatureLayer = layer as BasicFeatureLayer;

		if (basicFeatureLayer == null)
		{
			NotificationUtils.Add(notifications, "No feature layer");
			return false;
		}

		string layerName = layer.Name;

		if (! LayerUtils.IsVisible(layer))
		{
			NotificationUtils.Add(notifications, $"Layer {layerName} not visible");
			return false;
		}

		if (! basicFeatureLayer.IsSelectable)
		{
			NotificationUtils.Add(notifications, $"Layer {layerName} not selectable");
			return false;
		}

		//if (SelectOnlyEditFeatures &&
		//    !basicFeatureLayer.IsEditable)
		//{
		//	NotificationUtils.Add(notifications, $"Layer {layerName} not editable");
		//	return false;
		//}

		if (! CanSelectGeometryType(
			    GeometryUtils.TranslateEsriGeometryType(basicFeatureLayer.ShapeType)))
		{
			NotificationUtils.Add(notifications,
			                      $"Layer {layerName}: Cannot use geometry type {basicFeatureLayer.ShapeType}");
			return false;
		}

		//if (basicFeatureLayer is FeatureLayer featureLayer)
		//{
		//	if (featureLayer.GetFeatureClass() == null)
		//	{
		//		NotificationUtils.Add(notifications, $"Layer {layerName} is invalid");
		//		return false;
		//	}
		//}

		return CanSelectFromLayerCore(basicFeatureLayer);
	}

	protected virtual bool CanSelectGeometryType(GeometryType geometryType)
	{
		return true;
	}

	protected virtual bool CanSelectFromLayerCore([NotNull] BasicFeatureLayer basicFeatureLayer)
	{
		return true;
	}

	#endregion

	#region events

	private static void OnActiveToolChanged(ToolEventArgs args)
	{
		Tool activeTool = FrameworkApplication.ActiveTool;

		if (activeTool is SketchTool sketchTool)
		{
		}

		if (activeTool is EditingTool editingTool)
		{
		}

		if (args.CurrentID == "esri_editing_ModifyFeatureImpl")
		{
			//FrameworkApplication.SetCurrentToolAsync(ID);
		}
	}

	private static Task OnActiveToolChanging(ToolEventArgs args)
	{
		Tool activeTool = FrameworkApplication.ActiveTool;

		if (args.CurrentID == "esri_editing_ModifyFeatureImpl")
		{
			//FrameworkApplication.SetCurrentToolAsync(ID);
		}

		if (activeTool is SketchTool sketchTool)
		{
		}

		if (activeTool is EditingTool editingTool)
		{
			
		}

		return Task.FromResult(0);
	}

	private static void OnActiveWindowChanged(WindowEventArgs args)
	{
		if (args.Window is DockPane dockPane)
		{
			if (dockPane.ID == "esri_editing_EditFeaturesDockPane")
			{
				//ActivateDockPane("esri_core_projectDockPane");
			}
		}

		//((DockPane)args.Window).Hide();
	}

	private static void OnActivePaneChanging(PaneEventArgs args)
	{
		Pane incomingPane = args.IncomingPane;
		Pane outgoingPane = args.OutgoingPane;
	}

	#endregion

	private static bool ActivateDockPane(string id, bool focus = false)
	{
		DockPane dockPane = FrameworkApplication.DockPaneManager.Find(id);
		if (dockPane == null)
		{
			return false;
		}
		
		dockPane.Activate(focus);
		return true;
	}
}
