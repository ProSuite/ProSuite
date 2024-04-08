using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
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
	}

	protected bool AllowNoSelection => false;

	protected abstract bool AllowMultiSelection { get; }

	#region overrides

	protected sealed override async Task OnSelectionChangedAsync(MapSelectionChangedEventArgs args)
	{
		await ViewUtils.TryAsync(OnSelectionChangedCoreAsync(args), _msg);
	}

	/// <summary>
	/// Is on the GUI thread.
	/// </summary>
	/// <returns></returns>
	protected sealed override async Task<bool> OnSketchModifiedAsync()
	{
		return await ViewUtils.TryAsync(OnSketchModifiedCoreAsync(), _msg);
	}

	protected sealed override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
	{
		if (geometry == null || geometry.IsEmpty)
		{
			_msg.Debug("Sketch is null or empty");
			return await Task.FromResult(false);
		}

		if (MapUtils.HasSelection(ActiveMapView))
		{
			return await ViewUtils.TryAsync(OnConstructionSketchCompleteAsync(geometry), _msg);
		}

		return await ViewUtils.TryAsync(OnSelectionSketchCompleteAsync(geometry), _msg);
	}

	protected virtual async Task OnSelectionChangedCoreAsync(
		[NotNull] MapSelectionChangedEventArgs args)
	{
		if (! MapUtils.HasSelection(ActiveMapView.Map))
		{
			ResetSketchAppearance();

			await ClearSketchAsync();
			return;
		}

		await ProcessSelectionAsync(SelectionUtils.GetSelection<BasicFeatureLayer>(args.Selection));
	}

	protected virtual Task<bool> OnSketchModifiedCoreAsync()
	{
		return Task.FromResult(true);
	}

	/// <summary>
	/// Is on the GUI thread.
	/// </summary>
	/// <param name="geometry"></param>
	/// <returns></returns>
	protected virtual Task<bool> OnSelectionSketchCompleteAsync([NotNull] Geometry geometry)
	{
		// Return true or false does not seem to have an effect.
		// OnSelectionChangedAsync gets called anyway.

		return QueuedTask.Run(() =>
		{
			// todo daro ToList() needed?
			IEnumerable<FeatureSelectionBase> featureSelection =
				FindFeatureSelection(geometry).ToList();

			long selectFeatures = SelectionUtils.SelectFeatures(featureSelection,
			                                                    SelectionCombinationMethod.New,
			                                                    clearExistingSelection: true);

			return selectFeatures != 0;
		});
	}

	protected virtual Task<bool> OnConstructionSketchCompleteAsync([NotNull] Geometry geometry)
	{
		return Task.FromResult(true);
	}

	#endregion

	#region base

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

	/// <summary>
	/// Whether the required selection can only contain editable features.
	/// </summary>
	protected virtual bool CanSelectOnlyEditFeatures()
	{
		return true;
	}

	protected virtual bool CanSelectGeometryType(GeometryType geometryType)
	{
		return true;
	}

	protected abstract bool CanSelectFromLayerCore([NotNull] BasicFeatureLayer layer);

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

	#endregion

	#region process selection

	private async Task ProcessSelectionAsync(
		[NotNull] Dictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		var notifications = new NotificationCollection();

		IDictionary<BasicFeatureLayer, IList<Feature>> selection =
			GetApplicableSelectedFeatures(selectionByLayer, notifications);

		if (! CanUseSelection(selection, notifications))
		{
			return;
		}

		await AfterSelectionAsync(selection, progressor);
	}

	protected bool CanUseSelection(
		[NotNull] IDictionary<BasicFeatureLayer, IList<Feature>> selectionByLayer,
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
			_msg.Debug($"Cannot use selection: multi selection is not allowed and selection count is {count}");
			return false;
		}

		return CanUseSelectionCore(selectionByLayer, notifications);
	}

	protected virtual bool CanUseSelectionCore(
		[NotNull] IDictionary<BasicFeatureLayer, IList<Feature>> selectionByLayer,
		[NotNull] NotificationCollection notificationCollection)
	{
		return true;
	}

	protected Task AfterSelectionAsync(
		[NotNull] IDictionary<BasicFeatureLayer, IList<Feature>> featuresByLayer,
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

		return AfterSelectionCoreAsync(featuresByLayer, progressor);
	}

	protected virtual Task AfterSelectionCoreAsync(
		[NotNull] IDictionary<BasicFeatureLayer, IList<Feature>> featuresByLayer,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		return Task.FromResult(0);
	}

	protected async Task<IDictionary<T, IList<long>>> GetApplicableSelection<T>()
		where T : BasicFeatureLayer
	{
		Dictionary<T, List<long>> selectionByLayer =
			await QueuedTask.Run(() => SelectionUtils.GetSelection<T>(ActiveMapView.Map));

		IDictionary<T, IList<long>> result =
			new Dictionary<T, IList<long>>(selectionByLayer.Count);

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
	private IDictionary<BasicFeatureLayer, IList<Feature>> GetApplicableSelectedFeatures(
		[NotNull] Dictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		[CanBeNull] NotificationCollection notifications = null)
	{
		IDictionary<BasicFeatureLayer, IList<Feature>> result =
			new Dictionary<BasicFeatureLayer, IList<Feature>>(selectionByLayer.Count);

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

	#endregion

	protected void ResetSketchAppearance()
	{
		SketchType = _defaultSketchGeometryType;
		SketchSymbol = null;
	}

	protected void UpdateSketchAppearance([NotNull] FeatureLayer layer, [CanBeNull] Feature feature)
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
}
