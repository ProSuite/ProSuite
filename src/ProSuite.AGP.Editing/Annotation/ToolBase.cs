using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

	protected ToolBase(SketchGeometryType sketchGeometryType)
	{
		// needed to call OnSelectionChangedAsync
		UseSelection = true;
		UseSnapping = true;
		IsSketchTool = true;
		FireSketchEvents = true;
		IsWYSIWYG = true;
		SketchType = sketchGeometryType;
	}

	#region overrides

	protected sealed override async Task OnSelectionChangedAsync(MapSelectionChangedEventArgs args)
	{
		if (! MapUtils.HasSelection(ActiveMapView.Map))
		{
			await ClearSketchAsync();
			return;
		}

		await ProcessSelection(args.Selection.ToDictionary<BasicFeatureLayer>());
	}

	protected override void OnCurrentTemplateUpdated()
	{
		base.OnCurrentTemplateUpdated();
	}

	protected override Task<bool> OnSketchModifiedAsync()
	{
		return OnSketchModifiedCoreAsync();
	}

	protected virtual Task<bool> OnSketchModifiedCoreAsync()
	{
		return Task.FromResult(true);
	}

	protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
	{
		if (geometry == null || geometry.IsEmpty)
		{
			return false;
		}

		Task<bool> task = QueuedTask.Run(() =>
		{
			// todo daro ToList() needed?
			IEnumerable<FeatureSelectionBase> featureSelection =
				FindFeatureSelection(geometry).ToList();

			long selectFeatures = SelectionUtils.SelectFeatures(featureSelection,
			                                                    SelectionCombinationMethod.New,
			                                                    clearExistingSelection: true);

			return selectFeatures != 0;
		});

		// Return true or false does not seem to have an effect.
		// OnSelectionChangedAsync gets called anyway.
		return await ViewUtils.TryAsync(task, _msg);
	}

	#endregion

	#region base

	private IEnumerable<FeatureSelectionBase> FindFeatureSelection(
		Geometry geometry,
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

	protected virtual bool CanSelectFromLayerCore([NotNull] BasicFeatureLayer basicFeatureLayer)
	{
		return true;
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

	#endregion

	#region process selection

	private async Task ProcessSelection(
		Dictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		var notifications = new NotificationCollection();

		await AfterSelection(GetApplicableSelectedFeatures(selectionByLayer, notifications),
		                     progressor);
	}

	private IDictionary<BasicFeatureLayer, IEnumerable<Feature>> GetApplicableSelectedFeatures(
		[NotNull] Dictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		[CanBeNull] NotificationCollection notifications = null)
	{
		IDictionary<BasicFeatureLayer, IEnumerable<Feature>> result =
			new Dictionary<BasicFeatureLayer, IEnumerable<Feature>>(selectionByLayer.Count);

		SpatialReference mapSpatialReference = MapView.Active.Map.SpatialReference;

		foreach (KeyValuePair<BasicFeatureLayer, List<long>> oidsByLayer in selectionByLayer)
		{
			BasicFeatureLayer layer = oidsByLayer.Key;
			List<long> oids = oidsByLayer.Value;

			if (! CanSelectFromLayer(layer, notifications))
			{
				continue;
			}

			result.Add(layer, MapUtils.GetFeatures(layer, oids, false, mapSpatialReference));
		}

		return result;
	}

	protected virtual Task AfterSelection(
		IDictionary<BasicFeatureLayer, IEnumerable<Feature>> featuresByLayer,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		return Task.FromResult(0);
	}

	#endregion

	#region unused

	private IEnumerable<KeyValuePair<BasicFeatureLayer, IEnumerable<Feature>>>
		GetApplicableSelectedFeatures_(
			[NotNull] Dictionary<BasicFeatureLayer, List<long>> selectionByLayer,
			[CanBeNull] NotificationCollection notifications = null)
	{
		SpatialReference mapSpatialReference = MapView.Active.Map.SpatialReference;

		foreach (KeyValuePair<BasicFeatureLayer, List<long>> oidsByLayer in selectionByLayer)
		{
			BasicFeatureLayer layer = oidsByLayer.Key;
			List<long> oids = oidsByLayer.Value;

			if (! CanSelectFromLayer(layer, notifications))
			{
				continue;
			}

			yield return new KeyValuePair<BasicFeatureLayer, IEnumerable<Feature>>(
				layer, MapUtils.GetFeatures(layer, oids, false, mapSpatialReference));
		}
	}

	protected virtual Task AfterSelection_(
		IEnumerable<KeyValuePair<BasicFeatureLayer, IEnumerable<Feature>>> featuresByLayer,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		return Task.FromResult(0);
	}

	#endregion
}
