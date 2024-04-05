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
		// todo daro: is this the right place? _msg is of ToolBase.
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
		return await ViewUtils.TryAsync(OnSketchCompleteCoreAsync(geometry), _msg);
	}

	protected virtual async Task OnSelectionChangedCoreAsync(MapSelectionChangedEventArgs args)
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
	protected virtual Task<bool> OnSketchCompleteCoreAsync(Geometry geometry)
	{
		if (geometry == null || geometry.IsEmpty)
		{
			return Task.FromResult(false);
		}

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

	private async Task ProcessSelectionAsync(
		Dictionary<BasicFeatureLayer, List<long>> selectionByLayer,
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

	private bool CanUseSelection(IDictionary<BasicFeatureLayer,
		                             IList<Feature>> selectionByLayer,
	                             NotificationCollection notifications)
	{
		// todo daro notifications
		int count = selectionByLayer.Values.Sum(features => features.Count);

		if (count == 0 && ! AllowNoSelection)
		{
			return false;
		}

		if (count > 1 && ! AllowMultiSelection)
		{
			return false;
		}

		return CanUseSelectionCore(selectionByLayer);
	}

	protected virtual bool CanUseSelectionCore(
		IDictionary<BasicFeatureLayer, IList<Feature>> selectionByLayer)
	{
		return true;
	}

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

	protected Task AfterSelectionAsync(
		IDictionary<BasicFeatureLayer, IList<Feature>> featuresByLayer,
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
		IDictionary<BasicFeatureLayer, IList<Feature>> featuresByLayer,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		return Task.FromResult(0);
	}

	protected virtual Task AfterSelectionCoreAsync<T>(
		IDictionary<T, IEnumerable<Feature>> featuresByLayer,
		[CanBeNull] CancelableProgressor progressor = null) where T : BasicFeatureLayer
	{
		return Task.FromResult(0);
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
				// todo daro
				_msg.Debug($"");
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, null);
		}

		// todo daro inline
		CIMSymbol symbol = layer.LookupSymbol(oid, ActiveMapView);
		SketchSymbol = symbol.MakeSymbolReference();

		UpdateSketchAppearanceCore(layer, feature);
	}

	protected virtual void UpdateSketchAppearanceCore(FeatureLayer layer, Feature feature)
	{
		
	}
}
