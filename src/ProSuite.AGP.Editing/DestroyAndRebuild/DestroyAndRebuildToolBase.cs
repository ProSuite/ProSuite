using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
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
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

public abstract class DestroyAndRebuildToolBase : ToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private DestroyAndRebuildFeedback _feedback;

	protected override SymbolizedSketchTypeBasedOnSelection GetSymbolizedSketch()
	{
		return new SymbolizedSketchTypeBasedOnSelection(this);
	}

	protected override Cursor GetSelectionCursor()
	{
		return ToolUtils.CreateCursor(Resources.Arrow,
		                              Resources.DestroyAndRebuildOverlay,
		                              null);
	}

	protected override Cursor GetSelectionCursorLasso()
	{
		return ToolUtils.CreateCursor(Resources.Arrow,
		                              Resources.DestroyAndRebuildOverlay,
		                              Resources.Lasso);
	}

	protected override Cursor GetSelectionCursorPolygon()
	{
		return ToolUtils.CreateCursor(Resources.Arrow,
		                              Resources.DestroyAndRebuildOverlay,
		                              Resources.Polygon);
	}

	protected override bool AllowMultiSelection(out string reason)
	{
		reason = "Destroy and rebuild not possible. Please select only one feature.";
		return false;
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
		_feedback?.Clear();
		_feedback = null;

		return base.OnToolDeactivateCoreAsync(hasMapViewChanged);
	}

	protected override async Task OnSelectionChangedCoreAsync(
		MapSelectionChangedEventArgs args)
	{
		if (args.Selection.Count == 0)
		{
			_feedback.Clear();
		}

		await base.OnSelectionChangedCoreAsync(args);
	}

	protected override async Task HandleEscapeAsync()
	{
		var task = QueuedTask.Run(
			() =>
			{
				SelectionUtils.ClearSelection(ActiveMapView?.Map);

				_feedback.Clear();
			});
		await ViewUtils.TryAsync(task, _msg);
	}

	protected override async Task<bool> ProcessSelectionCoreAsync(
		IDictionary<BasicFeatureLayer, List<Feature>> featuresByLayer,
		CancelableProgressor progressor = null)
	{
		Assert.ArgumentCondition(featuresByLayer.Count == 1, "selection count has to be 1");

		(BasicFeatureLayer layer, List<Feature> features) = featuresByLayer.FirstOrDefault();

		Feature feature = features?.FirstOrDefault();

		// todo daro: assert instead?
		if (feature == null)
		{
			_msg.Debug("no selection");
			_feedback.Clear();
			return false; // startConstructionPhase = false
		}

		_feedback.UpdatePreview(feature.GetShape());

		_msg.Info(
			$"Destroy and rebuild feature {GdbObjectUtils.GetDisplayValue(feature, layer.Name)}");

		_msg.Info("Sketch the new geometry. Hit [ESC] to reselect the target feature.");

		await StartSketchAsync();

		return true; // startConstructionPhase = true
	}

	protected override async Task<bool> OnConstructionSketchCompleteAsync(
		Geometry geometry, IDictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		CancelableProgressor progressor)
	{
		// todo daro: assert instead?
		if (selectionByLayer.Count == 0)
		{
			_msg.Debug("no selection");
			_feedback.Clear();

			return true; // startSelectionPhase = true;
		}

		await QueuedTaskUtils.Run(async () =>
		{
			try
			{
				var applicableSelection =
					SelectionUtils.GetApplicableSelectedFeatures(
						selectionByLayer, CanSelectFromLayer);

				List<Feature> selectedFeatures = applicableSelection.Values.FirstOrDefault();

				if (selectedFeatures == null || selectedFeatures.Count == 0)
				{
					_msg.Debug("no applicable selection");
					_feedback.Clear();

					return true; // startSelectionPhase = true;
				}

				BasicFeatureLayer featureLayer = selectionByLayer.Keys.First();
				Feature originalFeature = selectedFeatures.First();

				await StoreUpdatedFeature(featureLayer, originalFeature, geometry);

				_feedback.Clear();

				LogPromptForSelection();
				return true; // startSelectionPhase = true;
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message, ex);
				return true; // startSelectionPhase = true;
			}
		});

		return true; // startSelectionPhase = true;
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

	protected override bool CanSelectFromLayerCore(BasicFeatureLayer layer)
	{
		return layer is FeatureLayer;
	}

	protected override void StartConstructionPhaseCore()
	{
		if (QueuedTask.OnWorker)
		{
			ResetSketchVertexSymbolOptions();
		}
		else
		{
			QueuedTask.Run(ResetSketchVertexSymbolOptions);
		}
	}

	protected override void StartSelectionPhaseCore()
	{
		if (QueuedTask.OnWorker)
		{
			SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
			SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);
		}
		else
		{
			QueuedTask.Run(() =>
			{
				SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
				SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);
			});
		}
	}
}
