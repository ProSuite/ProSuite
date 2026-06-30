using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.SplitLine;

public abstract class SplitLineToolBase : MapToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected SplitLineToolBase()
	{
		IsSketchTool = true;
		SketchType = SketchGeometryType.Point;
		SketchOutputMode = SketchOutputMode.Map;
		UseSnapping = true;
		FireSketchEvents = true;
		SketchMode = SketchMode.Line;
		UseSelection = false;
		UsesCurrentTemplate = false;
		IsWYSIWYG = true;
	}

	protected abstract IPickerPrecedence CreatePickerPrecedence(Geometry sketchGeometry);
	
	protected override Task OnToolActivateCoreAsync(bool hasMapViewChanged)
	{
		try
		{
			var newCursor = ToolUtils.GetCursor(Resources.SplitLineToolCursor);
			SetToolCursor(newCursor);
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Splits the line at the given position.
	/// The line feature is selected using the picker.
	/// </summary>
	private async Task SplitLineAt(MapPoint mapPoint)
	{
		// Create PickerPrecedence with the click point as sketch geometry
		using var precedence = CreatePickerPrecedence(mapPoint);

		var candidates =
			FindFeaturesOfAllLayers(precedence.GetSelectionGeometry(),
			                        precedence.SpatialRelationship).ToList();

		List<IPickableItem> items =
			await PickerUtils.GetItemsAsync(candidates, precedence);

		if (items.Count == 0)
		{
			_msg.Info("SplitLine: Select split point on a line feature.");
			return;
		}

		if (items.Count > 1)
		{
			_msg.Warn(
				"SplitLine: Too many candidate features found. Only considering the first one.");
		}

		IPickableFeatureItem pickableItem = items[0] as IPickableFeatureItem;
		Feature itemFeature = pickableItem?.Feature;
		if (itemFeature == null)
		{
			_msg.Warn("SplitLine: No splittable line picked.");
			return;
		}

		bool featureIsSelected = SelectionUtils.IsSelected(pickableItem.Layer, itemFeature);

		Polyline polyline = (Polyline) itemFeature.GetShape();
		if (polyline == null)
		{
			_msg.Warn("SplitLine: No splittable line picked (invalid geometry type).");
			return;
		}

		double xyTolerance = polyline.SpatialReference.XYTolerance;
		List<MapPoint> uniqueEndPoints = GeometryUtils.GetUniqueEndPoints(polyline);
		if (GeometryUtils.IsSamePointXY(mapPoint, uniqueEndPoints[0]))
		{
			_msg.Warn("SplitLine: Cannot split at start point.");
			return;
		}

		if (GeometryUtils.IsSamePointXY(mapPoint, uniqueEndPoints[1]))
		{
			_msg.Warn("SplitLine: Cannot split at end point.");
			return;
		}

		Multipoint splitPoints = MultipointBuilderEx.CreateMultipoint(mapPoint);
		GeometryUtils.SplitPolycurve(polyline, splitPoints, false, true, xyTolerance,
		                             out var modifiedPolyline);

		ReadOnlyPartCollection parts = modifiedPolyline.Parts;
		if (parts.Count < 2)
		{
			_msg.Warn("SplitLine: Cannot split.");
			return;
		}

		Polyline updatePolyline = PolylineBuilderEx.CreatePolyline(parts[0]);
		Polyline newPolyline = PolylineBuilderEx.CreatePolyline(parts[1]);

		if (newPolyline.Length > updatePolyline.Length)
		{
			// keep the longer one as the original
			(newPolyline, updatePolyline) = (updatePolyline, newPolyline);
		}

		var updates = new Dictionary<Feature, Geometry>
		              {
			              { itemFeature, updatePolyline }
		              };

		var datasets = new List<Dataset> { itemFeature.GetTable() };

		// exclude UUID field, RuleEngine will assign a new UUID
		ICollection<string> exclusionFieldNames = GetExclusionFieldNames(itemFeature);

		// split the feature

		Feature newFeature = null;

		EditOperation editOperation = new EditOperation();
		EditorTransaction transaction = new EditorTransaction(editOperation);
		bool successful = await transaction.ExecuteAsync(
			             editContext =>
			             {
				             _msg.DebugFormat("Saving {0} updates and {1} inserts...",
				                              updates.Count, 1);

				             GdbPersistenceUtils.UpdateTx(editContext, updates);

				             newFeature = GdbPersistenceUtils.InsertTx(
					             editContext, itemFeature, newPolyline, exclusionFieldNames);
			             },
			             "Split line", datasets);
		if (!successful)
		{
			return;
		}

		await PostProcessNewFeatures(itemFeature, newFeature, editOperation);

		// Update selection and flash the new feature

		List<long> objectIds = new List<long>{newFeature.GetObjectID(), itemFeature.GetObjectID()};

		if (featureIsSelected)
		{
			SelectionUtils.SelectRows(pickableItem.Layer, SelectionCombinationMethod.Add,
			                          objectIds);
		}

		var oidsByMapMember = new Dictionary<BasicFeatureLayer, List<long>>
		                      { { pickableItem.Layer, objectIds } };
		SelectionSet selectionSet = SelectionSet.FromDictionary(oidsByMapMember);

		ActiveMapView.FlashFeature(selectionSet, true);
	}

	// Note that this is called after the edit operation has been executed so that
	// addedFeature is fully created and has its own UUID set. Overrides should therefore
	// use a chained operation if they need to make edits (editOperation.CreateChainedOperation).
	protected virtual Task PostProcessNewFeatures(Feature existingFeature,
	                                                    Feature addedFeature,
	                                                    EditOperation editOperation)
	{
		return Task.CompletedTask;
	}

	private IEnumerable<FeatureSelectionBase> FindFeaturesOfAllLayers(
		[NotNull] Geometry searchGeometry,
		SpatialRelationship spatialRelationship = SpatialRelationship.Intersects,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		var mapView = ActiveMapView;

		if (mapView is null)
		{
			return new List<FeatureSelectionBase>();
		}

		var featureFinder = new FeatureFinder(mapView)
		                    {
			                    SpatialRelationship = spatialRelationship,
			                    DelayFeatureFetching = true
		                    };

		const Predicate<Feature> featurePredicate = null;
		return featureFinder.FindFeaturesByLayer(searchGeometry, fl => CanSelectFromLayer(fl),
		                                         featurePredicate, progressor);
	}

	private bool CanSelectFromLayer([CanBeNull] Layer layer)
	{
		if (layer is not FeatureLayer featureLayer)
		{
			return false;
		}

		if (featureLayer.ShapeType != esriGeometryType.esriGeometryPolyline)
		{
			return false;
		}

		if (! featureLayer.IsSnappable)
		{
			return false;
		}

		if (! LayerUtils.IsVisible(layer, ActiveMapView))
		{
			return false;
		}

		if (! featureLayer.IsSelectable)
		{
			return false;
		}

		if (! featureLayer.IsEditable)
		{
			return false;
		}

		return true;
	}

	[CanBeNull]
	protected virtual ICollection<string> GetExclusionFieldNames(Feature feature)
	{
		return null;
	}

	protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
	{
		try
		{
			if (geometry is MapPoint mapPoint)
			{
				await QueuedTask.Run(async () => await SplitLineAt(mapPoint));
			}
			else
			{
				_msg.WarnFormat("Point sketch geometry expected, was: {0}",
				                geometry.GeometryType);
			}
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}

		return true;
	}

	protected override Task HandleEscapeAsync()
	{
		return Task.CompletedTask;
	}
}
