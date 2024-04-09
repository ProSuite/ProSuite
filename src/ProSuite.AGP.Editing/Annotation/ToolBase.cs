using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.Picker;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.Annotation;

public abstract class ToolBase : MapTool
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly SketchGeometryType _defaultSketchGeometryType;

	protected ToolBase(SketchGeometryType sketchGeometryType)
	{
		// needed to call OnSelectionChangedAsync
		UseSelection = true;
		UseSnapping = true;
		IsSketchTool = true;
		FireSketchEvents = true;
		IsWYSIWYG = true;
		SketchType = sketchGeometryType;

		_defaultSketchGeometryType = sketchGeometryType;

		//_defaultSelectionCursor = GetCursor(Resources.SelectionToolNormal);
		SelectionCursorCore = GetCursor(Resources.CreateFeatureInPickedClassCursor);
		ConstructionCursorCore = GetCursor(Resources.EditSketchCrosshair);
	}

	private List<Key> HandledKeys { get; } = new() { Key.Escape, Key.F2 };

	protected virtual Cursor SelectionCursorCore { get; }

	protected virtual Cursor ConstructionCursorCore { get; }

	protected virtual bool AllowNoSelection => false;

	#region abstract

	protected abstract bool AllowMultiSelection { get; }

	protected abstract Task HandleEscapeAsync();

	protected abstract bool CanSelectFromLayerCore([NotNull] BasicFeatureLayer layer);

	#endregion

	#region overrides

	protected sealed override async Task OnToolActivateAsync(bool hasMapViewChanged)
	{
		_msg.Debug($"{Caption} activated");

		await ViewUtils.TryAsync(OnToolActivateCoreAsync(hasMapViewChanged), _msg);
	}

	protected sealed override async Task OnToolDeactivateAsync(bool hasMapViewChanged)
	{
		_msg.Debug($"{Caption} deactivated");

		await ViewUtils.TryAsync(OnToolDeactivateCoreAsync(hasMapViewChanged), _msg);
	}

	protected sealed override async Task OnSelectionChangedAsync(MapSelectionChangedEventArgs args)
	{
		await ViewUtils.TryAsync(OnSelectionChangedCoreAsync(args), _msg);
	}

	/// <summary>
	/// Is on the GUI thread.
	/// </summary>
	protected sealed override async Task<bool> OnSketchModifiedAsync()
	{
		return await ViewUtils.TryAsync(OnSketchModifiedCoreAsync(), _msg);
	}

	protected sealed override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
	{
		return await ViewUtils.TryAsync(OnSketchCompleteCoreAsync(geometry), _msg);
	}

	protected override void OnToolKeyDown(MapViewKeyEventArgs args)
	{
		if (HandledKeys.Contains(args.Key))
		{
			args.Handled = true;
		}
	}

	protected sealed override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
	{
		if (args.Key == Key.F2)
		{
			await FinishSketchAsync();
		}

		if (args.Key == Key.Escape)
		{
			await ViewUtils.TryAsync(HandleEscapeAsync, _msg);

			SetSelectionCursor();
		}

		await ViewUtils.TryAsync(HandleKeyDownCoreAsync(args), _msg);
	}

	#endregion

	#region tool

	protected virtual Task OnToolActivateCoreAsync(bool hasMapViewChanged)
	{
		return Task.CompletedTask;
	}

	protected virtual Task OnToolDeactivateCoreAsync(bool hasMapViewChanged)
	{
		return Task.CompletedTask;
	}

	protected virtual Task HandleKeyDownCoreAsync(MapViewKeyEventArgs args)
	{
		return Task.CompletedTask;
	}

	#endregion

	#region sketch

	protected virtual Task<bool> OnSketchModifiedCoreAsync()
	{
		return Task.FromResult(true);
	}

	private async Task<bool> OnSketchCompleteCoreAsync(Geometry geometry)
	{
		if (geometry == null || geometry.IsEmpty)
		{
			_msg.Debug("Sketch is null or empty");
			return await Task.FromResult(false);
		}

		if (MapUtils.HasSelection(ActiveMapView))
		{
			var notifications = new NotificationCollection();

			IDictionary<BasicFeatureLayer, List<long>> selection =
				await GetApplicableSelection<BasicFeatureLayer>();

			if (CanUseSelection(selection, notifications))
			{
				return await OnConstructionSketchCompleteAsync(geometry, selection);
			}
		}

		return await OnSelectionSketchCompleteAsync(geometry);
	}

	/// <summary>
	/// Is on the GUI thread.
	/// </summary>
	protected virtual async Task<bool> OnSelectionSketchCompleteAsync([NotNull] Geometry geometry)
	{
		List<IPickableItem> items =
			await QueuedTask.Run(() =>
			{
				IEnumerable<FeatureSelectionBase> selection = FindFeatureSelection(geometry);

				// ToList is needed otherwise thread exception!
				return PickableItemsFactory
				       .CreateFeatureItems(PickerUtils.OrderByGeometryDimension(selection))
				       .ToList();
			});

		if (! items.Any())
		{
			_msg.Debug("selection is empty");
			// todo daro return what?
			return true;
		}

		var picker = new PickerService();

		Func<Task<IPickableFeatureItem>> showControlOrPickBest =
			await QueuedTask.Run(() =>
			{
				Point pickerLocation = MapView.Active.MapToScreen(geometry.Extent.Center);

				return picker.Pick<IPickableFeatureItem>(
					items, pickerLocation, new SelectionToolPickerPrecedence
					                       {
						                       SelectionGeometry = geometry
					                       });
			});

		// show control on GUI thread
		IPickableFeatureItem pickedItem = await showControlOrPickBest();

		if (pickedItem == null)
		{
			return true;
		}

		await QueuedTask.Run(() =>
		{
			SelectionUtils.SelectFeature(pickedItem.Layer,
			                             SelectionCombinationMethod.New,
			                             pickedItem.Oid, true);
		});

		return true;
	}

	protected virtual Task<bool> OnConstructionSketchCompleteAsync([NotNull] Geometry geometry,
		IDictionary<BasicFeatureLayer, List<long>> selectionByLayer)
	{
		return Task.FromResult(true);
	}

	private void ResetSketchAppearance()
	{
		SketchType = _defaultSketchGeometryType;
		SketchSymbol = null;
	}

	private void UpdateSketchAppearance([NotNull] FeatureLayer layer, [CanBeNull] Feature feature)
	{
		if (feature == null)
		{
			ResetSketchAppearance();
			return;
		}

		GeometryType geometryType = feature.GetShape().GeometryType;
		long oid = feature.GetObjectID();

		switch (geometryType)
		{
			case GeometryType.Point:
				SketchType = SketchGeometryType.Point;
				break;
			case GeometryType.Polyline:
				SketchType = SketchGeometryType.Line;
				break;
			case GeometryType.Polygon:
				SketchType = SketchGeometryType.Polygon;
				break;
			case GeometryType.Unknown:
			case GeometryType.Envelope:
			case GeometryType.Multipoint:
			case GeometryType.Multipatch:
			case GeometryType.GeometryBag:
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, null);
		}

		CIMSymbol symbol = layer.LookupSymbol(oid, ActiveMapView);

		if (symbol == null)
		{
			_msg.Debug(
				$"Cannot set sketch symbol: no symbol found in layer {layer.Name} for oid {oid}.");
		}
		else
		{
			SketchSymbol = symbol.MakeSymbolReference();
		}

		UpdateSketchAppearanceCore(layer, feature);
	}

	protected virtual void UpdateSketchAppearanceCore([NotNull] FeatureLayer layer,
	                                                  [NotNull] Feature feature) { }

	#endregion

	#region selection

	private async Task OnSelectionChangedCoreAsync(
		[NotNull] MapSelectionChangedEventArgs args)
	{
		if (! MapUtils.HasSelection(ActiveMapView.Map))
		{
			SetSelectionCursor();

			ResetSketchAppearance();
			await ClearSketchAsync();

			return;
		}

		Dictionary<BasicFeatureLayer, List<long>> selectionByLayer =
			SelectionUtils.GetSelection<BasicFeatureLayer>(args.Selection);

		var notifications = new NotificationCollection();

		if (! CanUseSelection(selectionByLayer, notifications))
		{
			SetSelectionCursor();

			ResetSketchAppearance();
			await ClearSketchAsync();

			return;
		}

		SetConstructionCursor();

		IDictionary<BasicFeatureLayer, List<Feature>> selection =
			GetApplicableSelectedFeatures(selectionByLayer, notifications);

		await ProcessSelectionAsync(selection);
	}

	private Task ProcessSelectionAsync(
		[NotNull] IDictionary<BasicFeatureLayer, List<Feature>> featuresByLayer,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		if (! AllowMultiSelection)
		{
			(BasicFeatureLayer layer, IList<Feature> features) = featuresByLayer.FirstOrDefault();

			if (layer is FeatureLayer featureLayer)
			{
				UpdateSketchAppearance(featureLayer, features.FirstOrDefault());
			}
		}

		return ProcessSelectionCoreAsync(featuresByLayer, progressor);
	}

	protected virtual Task ProcessSelectionCoreAsync(
		[NotNull] IDictionary<BasicFeatureLayer, List<Feature>> featuresByLayer,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		return Task.CompletedTask;
	}

	private IEnumerable<FeatureSelectionBase> FindFeatureSelection(
		[NotNull] Geometry geometry,
		SpatialRelationship spatialRelationship = SpatialRelationship.Intersects)
	{
		var featureFinder = new FeatureFinder(ActiveMapView)
		                    {
			                    SpatialRelationship = spatialRelationship,
			                    DelayFeatureFetching = true
		                    };

		return featureFinder.FindFeaturesByLayer(
			geometry,
			fl => CanSelectFromLayer(fl));
	}

	private async Task<IDictionary<T, List<long>>> GetApplicableSelection<T>()
		where T : BasicFeatureLayer
	{
		// todo daro rename, revise method

		Dictionary<T, List<long>> selectionByLayer =
			await QueuedTask.Run(() => SelectionUtils.GetSelection<T>(ActiveMapView.Map));

		IDictionary<T, List<long>> result =
			new Dictionary<T, List<long>>(selectionByLayer.Count);

		var notifications = new NotificationCollection();

		foreach (KeyValuePair<T, List<long>> oidsByLayer in selectionByLayer)
		{
			T layer = oidsByLayer.Key;
			List<long> oids = oidsByLayer.Value;

			if (! CanSelectFromLayer(layer, notifications))
			{
				continue;
			}

			result.Add(layer, oids);
		}

		return result;
	}

	[NotNull]
	private IDictionary<BasicFeatureLayer, List<Feature>> GetApplicableSelectedFeatures(
		[NotNull] Dictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		[CanBeNull] NotificationCollection notifications = null)
	{
		var result = new Dictionary<BasicFeatureLayer, List<Feature>>(selectionByLayer.Count);

		SpatialReference mapSpatialReference = MapView.Active.Map.SpatialReference;

		foreach (KeyValuePair<BasicFeatureLayer, List<long>> oidsByLayer in selectionByLayer)
		{
			BasicFeatureLayer layer = oidsByLayer.Key;
			List<long> oids = oidsByLayer.Value;

			if (! CanSelectFromLayer(layer, notifications))
			{
				continue;
			}

			var features = MapUtils.GetFeatures(layer, oids, false, mapSpatialReference).ToList();

			result.Add(layer, features);
		}

		return result;
	}

	private bool CanSelectFromLayer([CanBeNull] BasicFeatureLayer layer,
	                                NotificationCollection notifications = null)
	{
		if (layer == null)
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

		if (! layer.IsSelectable)
		{
			NotificationUtils.Add(notifications, $"Layer {layerName} not selectable");
			return false;
		}

		if (CanSelectOnlyEditFeatures() &&
		    ! layer.IsEditable)
		{
			NotificationUtils.Add(notifications, $"Layer {layerName} not editable");
			return false;
		}

		if (! CanSelectGeometryType(
			    GeometryUtils.TranslateEsriGeometryType(layer.ShapeType)))
		{
			NotificationUtils.Add(notifications,
			                      $"Layer {layerName}: Cannot use geometry type {layer.ShapeType}");
			return false;
		}

		return CanSelectFromLayerCore(layer);
	}

	private bool CanUseSelection(
		[NotNull] IDictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		[NotNull] NotificationCollection notifications)
	{
		int count = selectionByLayer.Values.Sum(features => features.Count);

		if (count == 0 && ! AllowNoSelection)
		{
			_msg.Debug(
				$"Cannot use selection: tool has to have a selection and selection count is {count}");
			return false;
		}

		if (count > 1 && ! AllowMultiSelection)
		{
			_msg.Debug(
				$"Cannot use selection: multi selection is not allowed and selection count is {count}");
			return false;
		}

		return CanUseSelectionCore(selectionByLayer, notifications);
	}

	protected virtual bool CanUseSelectionCore(
		IDictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		NotificationCollection notifications)
	{
		return true;
	}

	protected virtual bool CanSelectOnlyEditFeatures()
	{
		return true;
	}

	protected virtual bool CanSelectGeometryType(GeometryType geometryType)
	{
		return true;
	}

	#endregion

	private void SetSelectionCursor()
	{
		//new MemoryStream()
		Cursor = SelectionCursorCore;
	}

	private void SetConstructionCursor()
	{
		Cursor = ConstructionCursorCore;
	}

	[NotNull]
	private static Cursor GetCursor([NotNull] byte[] bytes)
	{
		return new Cursor(new MemoryStream(bytes));
	}
}
