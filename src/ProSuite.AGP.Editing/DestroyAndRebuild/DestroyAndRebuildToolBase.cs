using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

public abstract class DestroyAndRebuildToolBase : ConstructionToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private DestroyAndRebuildFeedback _feedback;

	protected virtual bool UseOldSymbolization => true;

	private GeometryType _currentFeatureGeometryType;
	//private bool? _currentFeatureHasZ;

	protected override SelectionCursors FirstPhaseCursors { get; } =
		SelectionCursors.CreateArrowCursors(Resources.DestroyAndRebuildOverlay);

	protected override SymbolizedSketchTypeBasedOnSelection GetSymbolizedSketch()
	{
		return MapUtils.IsStereoMapView(ActiveMapView)
			       ? null
			       : new SymbolizedSketchTypeBasedOnSelection(this);
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

	protected override SketchGeometryType GetEditSketchGeometryType()
	{
		return ToolUtils.GetSketchGeometryType(_currentFeatureGeometryType);
	}

	protected override async Task<bool?> GetEditSketchHasZ()
	{
		Stopwatch watch = Stopwatch.StartNew();

		bool? result = await QueuedTask.Run(() =>
		{
			var selectedOidByLayer =
				SelectionUtils.GetSelection(ActiveMapView.Map).FirstOrDefault();

			FeatureLayer layer = selectedOidByLayer.Key as FeatureLayer;

			if (layer == null)
			{
				_msg.Debug($"{Caption}: no feature layer found in selection");
				return null;
			}

			FeatureClass featureClass = layer.GetFeatureClass();

			return featureClass?.GetDefinition()?.HasZ();
		});

		_msg.DebugStopTiming(watch, "Determined sketch has Z: {0}", result);

		return result;
	}

	protected override async Task OnToolActivateCoreAsync(bool hasMapViewChanged)
	{
		_feedback = new DestroyAndRebuildFeedback(UseOldSymbolization);

		await QueuedTask.Run(_feedback.InitializeSymbolsQueued);

		await base.OnToolActivateCoreAsync(hasMapViewChanged);
	}

	protected override Task OnToolDeactivateCoreAsync(bool hasMapViewChanged)
	{
		_feedback?.ClearSelection();
		_feedback = null;

		return base.OnToolDeactivateCoreAsync(hasMapViewChanged);
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
		Feature feature = selectedFeatures.Single();

		FeatureClass featureClass = feature.GetTable();
		_currentFeatureGeometryType = featureClass.GetShapeType();

		_feedback?.UpdateSelection(selectedFeatures);

		_msg.Info($"Rebuild the geometry for {GdbObjectUtils.GetDisplayValue(feature)}");

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

	/// <summary>
	/// Hook that lets a subclass store the rebuilt feature in a specialized way, for example by
	/// performing additional geometry manipulation before storing.
	/// The geometry has already been simplified and, for edges, flipped to keep the original
	/// orientation. Return <c>true</c> if the store was fully handled; return <c>false</c> to let
	/// the base tool perform the standard modify.
	/// </summary>
	protected virtual Task<bool> TryStoreRebuiltGeometryCoreAsync(
		[NotNull] BasicFeatureLayer featureLayer,
		[NotNull] Feature originalFeature,
		[NotNull] Geometry newGeometry)
	{
		return Task.FromResult(false);
	}

	private async Task StoreUpdatedFeature([NotNull] BasicFeatureLayer featureLayer,
	                                       [NotNull] Feature originalFeature,
	                                       [NotNull] Geometry sketchGeometry)
	{
		// Prevent invalid Z values and other non-simple geometries:
		Geometry simplifiedSketch =
			Assert.NotNull(GeometryUtils.Simplify(sketchGeometry), "Geometry is null");

		// For linear features, keep the original edge orientation unless the user
		// suppresses the automatic flip by holding ALT while finishing the sketch.
		if (simplifiedSketch is Polyline newLine &&
		    originalFeature.GetShape() is Polyline oldLine &&
		    ! newLine.IsEmpty && ! oldLine.IsEmpty)
		{
			bool allowFlip = ! KeyboardUtils.IsAltDown();

			simplifiedSketch = FlipIfNeeded(newLine, oldLine, allowFlip, out bool _);
		}

		if (await TryStoreRebuiltGeometryCoreAsync(featureLayer, originalFeature, simplifiedSketch))
		{
			return;
		}

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

	/// <summary>
	/// Returns the new edge geometry with its orientation reversed if it turns out to be
	/// oriented against the original edge geometry (i.e. its from/to points are closer to the
	/// original to/from points). Reversing is suppressed when <paramref name="allowFlip"/> is
	/// <c>false</c> (ALT held while finishing the sketch), in which case the geometry is
	/// returned as sketched and <paramref name="isReversed"/> stays <c>true</c>.
	/// </summary>
	[NotNull]
	private static Polyline FlipIfNeeded([NotNull] Polyline newLine,
	                                     [NotNull] Polyline oldLine,
	                                     bool allowFlip,
	                                     out bool isReversed)
	{
		isReversed = IsReversed(newLine, oldLine);

		if (! isReversed)
		{
			return newLine;
		}

		if (allowFlip)
		{
			Polyline flipped = GeometryUtils.ReverseOrientation(newLine);
			isReversed = false;

			_msg.Info("New edge geometry flipped to maintain the original orientation");
			using (_msg.IncrementIndentation())
			{
				_msg.Info(
					"- Use 'Flip' on the sketch context menu if the orientation should be reversed.");
				_msg.Info(
					"- Press 'ALT' while finishing the sketch, to apply the new edge orientation as is.");
			}

			return flipped;
		}

		_msg.Info(
			"The new edge geometry has reversed orientation, but 'ALT' was pressed to suppress " +
			"automatic flip. Geometry is used as is.");

		return newLine;
	}

	private static bool IsReversed([NotNull] Polyline newLine, [NotNull] Polyline oldLine)
	{
		Assert.ArgumentNotNull(newLine, nameof(newLine));
		Assert.ArgumentNotNull(oldLine, nameof(oldLine));
		Assert.False(newLine.IsEmpty, "new line is empty");
		Assert.False(oldLine.IsEmpty, "old line is empty");

		MapPoint oldFrom = GeometryUtils.GetStartPoint(oldLine);
		MapPoint oldTo = GeometryUtils.GetEndPoint(oldLine);
		MapPoint newFrom = GeometryUtils.GetStartPoint(newLine);
		MapPoint newTo = GeometryUtils.GetEndPoint(newLine);

		double distanceSumUnchanged =
			Distance2D(oldFrom, newFrom) + Distance2D(oldTo, newTo);

		double distanceSumReversed =
			Distance2D(oldFrom, newTo) + Distance2D(oldTo, newFrom);

		return distanceSumReversed < distanceSumUnchanged;
	}

	private static double Distance2D([NotNull] MapPoint a, [NotNull] MapPoint b)
	{
		double dx = a.X - b.X;
		double dy = a.Y - b.Y;

		return Math.Sqrt(dx * dx + dy * dy);
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
