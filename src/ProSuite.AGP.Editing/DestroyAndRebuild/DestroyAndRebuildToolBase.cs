using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core.Events;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

public abstract class DestroyAndRebuildToolBase : ConstructionToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private const Key _keyToggleMoveEndJunction = Key.M;

	private const string _hideEditedFeatureFilterName = "DestroyAndRebuild_HideEditedFeature";

	private DestroyAndRebuildFeedback _feedback;

	// While the edited feature is hidden (HideEditedFeature option), the layer whose display
	// filter we temporarily overrode, together with the display filter state to restore.
	[CanBeNull] private BasicFeatureLayer _hiddenFeatureLayer;
	[CanBeNull] private CIMDisplayFilter[] _savedDisplayFilters;
	private bool _savedEnableDisplayFilters;

	[CanBeNull]
	private OverridableSettingsProvider<PartialDestroyAndRebuildOptions> _settingsProvider;

	protected DestroyAndRebuildToolOptions _destroyAndRebuildToolOptions;

	protected virtual bool UseOldSymbolization => true;

	private GeometryType _currentFeatureGeometryType;
	//private bool? _currentFeatureHasZ;

	protected DestroyAndRebuildToolBase()
	{
		HandledKeys.Add(_keyToggleMoveEndJunction);
	}

	protected override SelectionCursors FirstPhaseCursors { get; } =
		SelectionCursors.CreateArrowCursors(Resources.DestroyAndRebuildOverlay);

	protected virtual string OptionsFileName => "DestroyAndRebuildToolOptions.xml";

	[CanBeNull]
	protected virtual string OptionsDockPaneID => null;

	[CanBeNull]
	protected virtual string CentralConfigDir => null;

	/// <summary>
	/// By default, the local configuration directory shall be in
	/// %APPDATA%\Roaming\ORGANIZATION\PRODUCT>\ToolDefaults.
	/// </summary>
	protected virtual string LocalConfigDir
		=> EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(
			AppDataFolder.Roaming, "ToolDefaults");

	protected override bool AllowSelectionChangeInSketchMode => true;

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

	// Allow re-picking a (single) target feature while sketching by holding SHIFT, even though
	// the tool does not support multi-selection.

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
		_destroyAndRebuildToolOptions = InitializeOptions();

		// Make sure a hidden feature is never persisted into the project: restore its visibility
		// before a save writes the .aprx (see OnProjectSavingAsync).
		ProjectSavingEvent.Subscribe(OnProjectSavingAsync);

		_feedback = new DestroyAndRebuildFeedback(UseOldSymbolization);

		await QueuedTask.Run(() =>
		{
			_feedback.InitializeSymbolsQueued();

			// Defensive: strip any leftover hide-filter that a previous session may have persisted
			// (e.g. after a crash) so that no feature stays stuck hidden.
			RemoveStaleHideFilters(ActiveMapView?.Map);
		});

		await base.OnToolActivateCoreAsync(hasMapViewChanged);
	}

	protected override async Task OnToolDeactivateCoreAsync(bool hasMapViewChanged)
	{
		ProjectSavingEvent.Unsubscribe(OnProjectSavingAsync);

		_settingsProvider?.StoreLocalConfiguration(_destroyAndRebuildToolOptions?.LocalOptions);

		if (_hiddenFeatureLayer != null)
		{
			await QueuedTask.Run(RestoreEditedFeatureVisibility);
		}

		_feedback?.Clear();
		_feedback = null;

		await base.OnToolDeactivateCoreAsync(hasMapViewChanged);
	}

	protected override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
	{
		await base.HandleKeyDownAsync(args);

		if (args.Key == _keyToggleMoveEndJunction)
		{
			_destroyAndRebuildToolOptions.MoveOpenJawEndJunction =
				! _destroyAndRebuildToolOptions.MoveOpenJawEndJunction;

			_msg.Info(_destroyAndRebuildToolOptions.MoveOpenJawEndJunction
				          ? "Enabled move linear network junction option"
				          : "Disabled move linear network junction option");
		}
	}

	protected override async Task HandleEscapeAsync()
	{
		// Clear the feedback up front - before base.HandleEscapeAsync() clears the map selection
		// and resets the sketch (each hopping to the MCT and back) - so the overlay disappears and
		// the hidden feature reappears without a perceptible delay. Only do so when the escape
		// actually leaves the sketch: a non-empty sketch is merely reset and stays on the same
		// feature, which should keep guiding the user with the reference highlight.
		bool leavingSketch = ! IsInSketchMode || ! await HasSketchAsync();

		if (leavingSketch)
		{
			_feedback?.Clear();

			if (_hiddenFeatureLayer != null)
			{
				await QueuedTask.Run(RestoreEditedFeatureVisibility);
			}
		}

		await base.HandleEscapeAsync();
	}

	protected override async Task OnSelectionPhaseStartedAsync()
	{
		// Safety net for transitions back to the selection phase that do not go through
		// HandleEscapeAsync (e.g. the selection being cleared externally or after an edit
		// completes): make sure the reference overlay is gone and the hidden feature is visible.
		_feedback?.Clear();

		if (_hiddenFeatureLayer != null)
		{
			await QueuedTask.Run(RestoreEditedFeatureVisibility);
		}

		await base.OnSelectionPhaseStartedAsync();
	}

	protected override async Task AfterSelectionAsync(IList<Feature> selectedFeatures,
	                                                  CancelableProgressor progressor)
	{
		Feature feature = selectedFeatures.Single();

		FeatureClass featureClass = feature.GetTable();
		_currentFeatureGeometryType = featureClass.GetShapeType();

		// Draw the reference overlay of the original geometry while sketching the replacement.
		if (_destroyAndRebuildToolOptions.HighlightOriginalGeometry)
		{
			_feedback?.Update(selectedFeatures);
		}

		// Optionally hide the actual feature so its (old) symbolized geometry does not visually
		// interfere with the new sketch.
		if (_destroyAndRebuildToolOptions.HideEditedFeature)
		{
			HideEditedFeature(feature, featureClass);
		}

		_msg.Info($"Rebuild the geometry for {GdbObjectUtils.GetDisplayValue(feature)}");

		await base.AfterSelectionAsync(selectedFeatures, progressor);
	}

	protected override void LogEnteringSketchMode()
	{
		_msg.Info("Sketch the new geometry. Hit [ESC] to reselect the target feature.");
		_msg.Info(
			"Change the selected feature while keeping SHIFT pressed (the current selection will be cleared).");
	}

	protected override async Task<bool> OnEditSketchCompleteCoreAsync(
		Geometry sketchGeometry,
		EditingTemplate editTemplate,
		MapView activeView,
		CancelableProgressor cancelableProgressor = null)
	{
		await QueuedTaskUtils.Run(async () =>
		{
			Dictionary<MapMember, List<long>> selectionByLayer =
				SelectionUtils.GetSelection<MapMember>(ActiveMapView.Map);

			// todo daro: assert instead?
			if (selectionByLayer.Count == 0)
			{
				_msg.Debug("no selection");
				_feedback?.Clear();

				return true;
			}

			List<Feature> selectedFeatures =
				GetDistinctApplicableSelectedFeatures(selectionByLayer, UnJoinedSelection)
					.ToList();

			if (selectedFeatures.Count == 0)
			{
				_msg.Debug("no applicable selection");
				_feedback?.Clear();

				return true;
			}

			BasicFeatureLayer featureLayer =
				(BasicFeatureLayer) selectionByLayer.Keys.First(k => k is BasicFeatureLayer);

			Feature originalFeature = selectedFeatures.First();

			await StoreUpdatedFeature(featureLayer, originalFeature, sketchGeometry);

			_feedback?.Clear();

			LogPromptForSelection();

			return true;
		});

		await StartSelectionPhaseAsync();
		return true;
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

	#region Tool Options DockPane

	protected override void ShowOptionsPane()
	{
		var viewModel = GetDestroyAndRebuildViewModel();

		if (viewModel == null)
		{
			return;
		}

		viewModel.Options = _destroyAndRebuildToolOptions;

		viewModel.Activate(true);
	}

	protected override void HideOptionsPane()
	{
		var viewModel = GetDestroyAndRebuildViewModel();
		viewModel?.Hide();
	}

	#endregion

	[CanBeNull]
	private DockPaneDestroyAndRebuildViewModelBase GetDestroyAndRebuildViewModel()
	{
		if (OptionsDockPaneID == null)
		{
			return null;
		}

		var viewModel =
			FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
				DockPaneDestroyAndRebuildViewModelBase;

		return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
		                      OptionsDockPaneID);
	}

	private DestroyAndRebuildToolOptions InitializeOptions()
	{
		Stopwatch watch = _msg.DebugStartTiming();

		// NOTE: by only reading the file locations we can save a couple of 100ms
		string _ = CentralConfigDir;
		string __ = LocalConfigDir;

		// Create a new instance only if it doesn't exist yet, so that any local overrides
		// made through the options dockpane are not lost across tool re-activations.
		_settingsProvider ??= new OverridableSettingsProvider<PartialDestroyAndRebuildOptions>(
			CentralConfigDir, LocalConfigDir, OptionsFileName);

		_settingsProvider.GetConfigurations(
			out PartialDestroyAndRebuildOptions localConfiguration,
			out PartialDestroyAndRebuildOptions centralConfiguration);

		var result = new DestroyAndRebuildToolOptions(centralConfiguration, localConfiguration);

		result.PropertyChanged -= OptionsPropertyChanged;
		result.PropertyChanged += OptionsPropertyChanged;

		_msg.DebugStopTiming(watch, "Destroy and Rebuild Options validated / initialized");

		string optionsMessage = result.GetLocalOverridesMessage();

		if (! string.IsNullOrEmpty(optionsMessage))
		{
			_msg.Info(optionsMessage);
		}

		return result;
	}

	/// <summary>
	/// Reacts to live changes of the highlight/hide options in the tool options pane so their
	/// effect is applied immediately while a replacement is being sketched, rather than only on
	/// the next selection.
	/// </summary>
	private async void OptionsPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		try
		{
			// Only relevant while actually sketching a replacement for a selected feature.
			if (! IsInSketchMode)
			{
				return;
			}

			if (e.PropertyName == nameof(DestroyAndRebuildToolOptions.HighlightOriginalGeometry))
			{
				await ApplyHighlightOptionAsync();
			}
			else if (e.PropertyName == nameof(DestroyAndRebuildToolOptions.HideEditedFeature))
			{
				await ApplyHideOptionAsync();
			}
		}
		catch (Exception ex)
		{
			_msg.Error($"{Caption}: error applying option change", ex);
		}
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

	private async Task<bool> StoreUpdatedFeature([NotNull] BasicFeatureLayer featureLayer,
	                                             [NotNull] Feature originalFeature,
	                                             [NotNull] Geometry sketchGeometry)
	{
		// Prevent invalid Z values and other non-simple geometries:
		Geometry simplifiedSketch = GeometryUtils.Simplify(sketchGeometry);

		if (simplifiedSketch == null || simplifiedSketch.IsEmpty)
		{
			throw new InvalidOperationException("Invalid or empty sketch");
		}

		// For linear features, keep the original edge orientation unless the user
		// suppresses the automatic flip by holding ALT while finishing the sketch.
		if (simplifiedSketch is Polyline newLine &&
		    originalFeature.GetShape() is Polyline oldLine &&
		    ! newLine.IsEmpty && ! oldLine.IsEmpty)
		{
			bool allowFlip = ! KeyboardUtils.IsAltDown();

			simplifiedSketch = FlipIfNeeded(newLine, oldLine, allowFlip, out bool _);
		}

		string subtypeName = GetSubtypeDisplayName(originalFeature, featureLayer);

		bool success;
		if (await TryStoreRebuiltGeometryCoreAsync(featureLayer, originalFeature, simplifiedSketch))
		{
			success = true;
		}
		else
		{
			success = await StoreRebuiltGeometryAsync(originalFeature, simplifiedSketch,
			                                          subtypeName);
		}

		if (success)
		{
			_msg.Info($"Updated geometry in {featureLayer.Name} ({subtypeName}) " +
			          $"ID: {originalFeature.GetObjectID()}");
		}

		return success;
	}

	protected static async Task<bool> StoreRebuiltGeometryAsync(Feature originalFeature,
	                                                            Geometry rebuiltGeometry,
	                                                            string subtypeName)
	{
		// NOTE: Use GdbPersistenceUtils to prevent the progress pop-up
		var dataset = new List<Dataset> { originalFeature.GetTable() };

		return await GdbPersistenceUtils.ExecuteInTransactionAsync(
			       editContext =>
			       {
				       GdbPersistenceUtils.StoreShape(originalFeature, rebuiltGeometry,
				                                      editContext);
				       return true;
			       },
			       $"Destroy and Rebuild {subtypeName}", dataset);
	}

	private static string
		GetSubtypeDisplayName(Feature originalFeature, BasicFeatureLayer featureLayer)
	{
		Subtype featureSubtype = GdbObjectUtils.GetSubtype(originalFeature);

		string subtypeName = featureSubtype != null
			                     ? featureSubtype.GetName()
			                     : featureLayer.Name;
		return subtypeName;
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
		MapPoint oldTo = Assert.NotNull(GeometryUtils.GetEndPoint(oldLine));

		MapPoint newFrom = GeometryUtils.GetStartPoint(newLine);
		MapPoint newTo = Assert.NotNull(GeometryUtils.GetEndPoint(newLine));

		double distanceSumUnchanged = Distance2D(oldFrom, newFrom) + Distance2D(oldTo, newTo);
		double distanceSumReversed = Distance2D(oldFrom, newTo) + Distance2D(oldTo, newFrom);

		return distanceSumReversed < distanceSumUnchanged;
	}

	private static double Distance2D([NotNull] MapPoint a, [NotNull] MapPoint b)
	{
		double dx = a.X - b.X;
		double dy = a.Y - b.Y;

		return Math.Sqrt(dx * dx + dy * dy);
	}

	#region Display Feedback

	/// <summary>
	/// Restores the hidden feature's visibility before the project is saved, so the temporary
	/// display filter is never written into the .aprx (which would leave the feature invisible even
	/// after a normal close and reopen).
	/// </summary>
	private async Task OnProjectSavingAsync(ProjectEventArgs args)
	{
		if (_hiddenFeatureLayer == null)
		{
			return;
		}

		await QueuedTask.Run(RestoreEditedFeatureVisibility);
	}

	private async Task ApplyHighlightOptionAsync()
	{
		if (_destroyAndRebuildToolOptions.HighlightOriginalGeometry)
		{
			await QueuedTask.Run(() =>
			{
				List<Feature> features =
					GetApplicableSelectedFeatures(ActiveMapView).ToList();
				_feedback?.Update(features);
			});
		}
		else
		{
			_feedback?.Clear();
		}
	}

	private async Task ApplyHideOptionAsync()
	{
		await QueuedTask.Run(() =>
		{
			if (_destroyAndRebuildToolOptions.HideEditedFeature)
			{
				if (_hiddenFeatureLayer == null)
				{
					Feature feature =
						GetApplicableSelectedFeatures(ActiveMapView).FirstOrDefault();

					if (feature != null)
					{
						HideEditedFeature(feature, feature.GetTable());
					}
				}
			}
			else
			{
				RestoreEditedFeatureVisibility();
			}
		});
	}

	/// <summary>
	/// Temporarily hides the edited feature by adding a display filter that excludes its
	/// object ID on the layer it belongs to. The previous display-filter state is remembered so
	/// it can be restored in <see cref="RestoreEditedFeatureVisibility()"/>.
	/// </summary>
	/// <remarks>Must be called on the MCT.</remarks>
	private void HideEditedFeature([NotNull] Feature feature, [NotNull] FeatureClass featureClass)
	{
		// If a previous feature is still hidden (it should have been restored already), restore it
		// first so we never capture our own filter as the "original" state to restore to.
		if (_hiddenFeatureLayer != null)
		{
			RestoreEditedFeatureVisibility();
		}

		Dictionary<BasicFeatureLayer, List<long>> selectionByLayer =
			SelectionUtils.GetSelection<BasicFeatureLayer>(ActiveMapView.Map);

		BasicFeatureLayer layer = selectionByLayer.Keys.FirstOrDefault();

		if (layer == null || layer.GetDefinition() is not CIMFeatureLayer cimLayer)
		{
			return;
		}

		long oid = feature.GetObjectID();
		string oidField = featureClass.GetDefinition().GetObjectIDField();

		_hiddenFeatureLayer = layer;
		_savedEnableDisplayFilters = cimLayer.EnableDisplayFilters;
		_savedDisplayFilters = cimLayer.DisplayFilters;

		cimLayer.DisplayFilters = new[]
		                          {
			                          new CIMDisplayFilter
			                          {
				                          Name = _hideEditedFeatureFilterName,
				                          WhereClause = $"{oidField} <> {oid}"
			                          }
		                          };
		cimLayer.EnableDisplayFilters = true;

		SetDefinitionWithoutUndo(layer, cimLayer);
	}

	/// <summary>
	/// Restores the display-filter state that was overridden by <see cref="HideEditedFeature"/>,
	/// making the edited feature visible again.
	/// </summary>
	/// <remarks>Must be called on the MCT.</remarks>
	private void RestoreEditedFeatureVisibility()
	{
		BasicFeatureLayer layer = _hiddenFeatureLayer;

		if (layer == null)
		{
			return;
		}

		try
		{
			if (layer.GetDefinition() is CIMFeatureLayer cimLayer)
			{
				cimLayer.EnableDisplayFilters = _savedEnableDisplayFilters;
				cimLayer.DisplayFilters = _savedDisplayFilters;

				SetDefinitionWithoutUndo(layer, cimLayer);
			}
		}
		finally
		{
			_hiddenFeatureLayer = null;
			_savedDisplayFilters = null;
			_savedEnableDisplayFilters = false;
		}
	}

	/// <summary>
	/// Applies a layer definition change without leaving an entry on the map's undo/redo stack.
	/// The temporary display filter used to hide the edited feature is transient tool state and
	/// must not be undoable: undoing/redoing it would re-apply or drop the filter behind the tool's
	/// back and could leave the feature stuck hidden.
	/// </summary>
	/// <remarks>Must be called on the MCT.</remarks>
	private static void SetDefinitionWithoutUndo([NotNull] BasicFeatureLayer layer,
	                                             [NotNull] CIMBaseLayer definition)
	{
		OperationManager operationManager = layer.Map?.OperationManager;

		var operationsBefore = operationManager?.FindUndoOperations(_ => true);

		layer.SetDefinition(definition);

		if (operationManager == null)
		{
			return;
		}

		// Remove any operation the SetDefinition call just pushed onto the undo stack.
		foreach (var operation in operationManager.FindUndoOperations(_ => true))
		{
			if (operationsBefore == null || ! operationsBefore.Contains(operation))
			{
				operationManager.RemoveUndoOperation(operation);
			}
		}
	}

	/// <summary>
	/// Removes any leftover "hide edited feature" display filter (identified by its well-known
	/// name) from the feature layers of the given map. This guards against a filter that a
	/// previous session persisted without restoring, e.g. after a crash, which would otherwise
	/// leave a feature stuck hidden.
	/// </summary>
	/// <remarks>Must be called on the MCT.</remarks>
	private void RemoveStaleHideFilters([CanBeNull] Map map)
	{
		if (map == null)
		{
			return;
		}

		foreach (BasicFeatureLayer layer in
		         map.GetLayersAsFlattenedList().OfType<BasicFeatureLayer>())
		{
			if (layer.GetDefinition() is not CIMFeatureLayer cimLayer)
			{
				continue;
			}

			CIMDisplayFilter[] filters = cimLayer.DisplayFilters;

			if (filters == null || filters.All(f => f?.Name != _hideEditedFeatureFilterName))
			{
				continue;
			}

			CIMDisplayFilter[] remaining =
				filters.Where(f => f?.Name != _hideEditedFeatureFilterName).ToArray();

			cimLayer.DisplayFilters = remaining.Length > 0 ? remaining : null;

			if (remaining.Length == 0)
			{
				cimLayer.EnableDisplayFilters = false;
			}

			SetDefinitionWithoutUndo(layer, cimLayer);

			_msg.Debug(
				$"{Caption}: removed stale '{_hideEditedFeatureFilterName}' display filter " +
				$"from layer {layer.Name}");
		}
	}

	#endregion
}
