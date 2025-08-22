using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

public abstract class DestroyAndRebuildToolBase : ConstructionToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected DestroyAndRebuildToolBase()
	{
		FireSketchEvents = true;
	}

	private DestroyAndRebuildFeedback _feedback;

	protected override SelectionCursors FirstPhaseCursors { get; } =
		SelectionCursors.CreateArrowCursors(Resources.DestroyAndRebuildOverlay);

	protected override SymbolizedSketchTypeBasedOnSelection GetSymbolizedSketch()
	{
		return new SymbolizedSketchTypeBasedOnSelection(this);
	}

	protected override bool AllowMultiSelection(out string reason)
	{
		reason = "Destroy and rebuild not possible. Please select only one feature.";
		return false;
	}

	protected override SketchGeometryType GetSelectionSketchGeometryType()
	{
		return SketchGeometryType.Rectangle;
	}

	protected override Task OnToolActivateCoreAsync(bool hasMapViewChanged)
	{
		// NOTE CompleteSketchOnMouseUp has not to be set before the sketch geometry type.
		// Set it on tool activate. In ctor is not enough.
		CompleteSketchOnMouseUp = true;
		GeomIsSimpleAsFeature = false;

		_feedback = new DestroyAndRebuildFeedback();

		return base.OnToolActivateCoreAsync(hasMapViewChanged);
	}

	protected override Task OnToolDeactivateCoreAsync(bool hasMapViewChanged)
	{
		_feedback?.ClearSelection();
		_feedback = null;

		return base.OnToolDeactivateCoreAsync(hasMapViewChanged);
	}

	protected override async Task OnSelectionPhaseStartedAsync()
	{
		await base.OnSelectionPhaseStartedAsync();

		await QueuedTask.Run(async () => { await ActiveMapView.ClearSketchAsync(); });
	}

	protected override async Task<bool> OnMapSelectionChangedCoreAsync(
		MapSelectionChangedEventArgs args)
	{
		if (args.Selection.Count == 0)
		{
			_feedback?.ClearSelection();
		}

		return await base.OnMapSelectionChangedCoreAsync(args);
	}

	protected override async Task AfterSelectionAsync(IList<Feature> selectedFeatures,
	                                                  CancelableProgressor progressor)
	{
		Assert.ArgumentCondition(selectedFeatures.Count == 1, "selection count has to be 1");

		Feature feature = selectedFeatures.FirstOrDefault();

		// todo daro: assert instead?
		if (feature == null)
		{
			_msg.Debug("no selection");
			_feedback.ClearSelection();
			return;
		}

		_feedback?.UpdateSelection(selectedFeatures);

		_msg.Info(
			$"Destroy and rebuild feature {GdbObjectUtils.GetDisplayValue(feature, feature.GetTable().GetName())}");

		await base.AfterSelectionAsync(selectedFeatures, progressor);
	}

	protected override void LogEnteringSketchMode()
	{
		_msg.Info("Sketch the new geometry. Hit [ESC] to reselect the target feature.");
	}

	protected override async Task<bool> OnEditSketchCompleteCoreAsync(
		Geometry sketchGeometry,
		EditingTemplate editTemplate,
		MapView activeView,
		CancelableProgressor cancelableProgressor = null)
	{
		await QueuedTaskUtils.Run(async () =>
		{
			Dictionary<BasicFeatureLayer, List<long>> selectionByLayer =
				SelectionUtils.GetSelection<BasicFeatureLayer>(ActiveMapView.Map);

			// todo daro: assert instead?
			if (selectionByLayer.Count == 0)
			{
				_msg.Debug("no selection");
				_feedback?.ClearSelection();

				return true;
			}

			try
			{
				var applicableSelection =
					SelectionUtils.GetApplicableSelectedFeatures(
						selectionByLayer, (layer) => CanSelectFromLayer(layer));

				List<Feature> selectedFeatures = applicableSelection.Values.FirstOrDefault();

				if (selectedFeatures == null || selectedFeatures.Count == 0)
				{
					_msg.Debug("no applicable selection");
					_feedback?.ClearSelection();

					return true;
				}

				BasicFeatureLayer featureLayer = selectionByLayer.Keys.First();
				Feature originalFeature = selectedFeatures.First();

				await StoreUpdatedFeature(featureLayer, originalFeature, sketchGeometry);

				_feedback?.ClearSelection();

				LogPromptForSelection();

				return true;
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message, ex);
				return true;
			}
		});

		await StartSelectionPhaseAsync();
		return true;
	}

	private async Task StoreUpdatedFeature([NotNull] BasicFeatureLayer featureLayer,
	                                       [NotNull] Feature originalFeature,
	                                       [NotNull] Geometry sketchGeometry)
	{
		// Prevent invalid Z values and other non-simple geometries:
		Geometry simplifiedSketch =
			Assert.NotNull(GeometryUtils.Simplify(sketchGeometry), "Geometry is null");

		Subtype featureSubtype = GdbObjectUtils.GetSubtype(originalFeature);

		string subtypeName = featureSubtype != null
			                     ? featureSubtype.GetName()
			                     : featureLayer.Name;

		// note: TooltipHeading is null here.
		var operation = new EditOperation
		                {
			                Name = $"Destroy and Rebuild {subtypeName}",
			                SelectModifiedFeatures = true
		                };

		// todo: daro move to base? make utils?
		operation.Modify(featureLayer, originalFeature.GetObjectID(), simplifiedSketch);

		if (operation.IsEmpty)
		{
			_msg.Debug($"{Caption}: edit operation is empty");
			return;
		}

		bool succeed = false;
		try
		{
			succeed = await operation.ExecuteAsync();
		}
		catch (Exception e)
		{
			_msg.Debug($"{Caption}: edit operation threw an exception", e);
		}
		finally
		{
			if (succeed)
			{
				_msg.Info(
					$"Updated feature in {featureLayer.Name} ({subtypeName}) ID: {originalFeature.GetObjectID()}");
			}
			else
			{
				_msg.Debug($"{Caption}: edit operation failed");
			}
		}
	}

	protected override bool CanSelectGeometryType(GeometryType geometryType)
	{
		switch (geometryType)
		{
			case GeometryType.Point:
			case GeometryType.Polyline:
			case GeometryType.Polygon:
			case GeometryType.Multipoint:
			case GeometryType.Multipatch:
				return true;
			case GeometryType.Unknown:
			case GeometryType.Envelope:
			case GeometryType.GeometryBag:
				_msg.Debug($"{Caption}: cannot select from geometry of type {geometryType}");
				return false;
			default:
				throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, null);
		}
	}

	protected override void LogPromptForSelection()
	{
		_msg.Info("Select feature for destroy and rebuild.");
	}

	protected override bool CanSelectFromLayerCore(
		BasicFeatureLayer basicFeatureLayer,
		NotificationCollection notifications)
	{
		return basicFeatureLayer is FeatureLayer;
	}

	protected override async Task OnSketchPhaseStartedAsync()
	{
		if (QueuedTask.OnWorker)
		{
			ResetSketchVertexSymbolOptions();
		}
		else
		{
			await QueuedTask.Run(ResetSketchVertexSymbolOptions);
		}

		await base.OnSketchPhaseStartedAsync();
	}
}
