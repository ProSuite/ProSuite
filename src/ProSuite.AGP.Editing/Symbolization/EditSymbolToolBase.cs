using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.Picker;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.Symbolization;

/// <summary>
/// Common base class for RotateSymbol and MoveSymbol tools
/// </summary>
public abstract class EditSymbolToolBase : MapTool
{
	protected enum ToolMode { Select, Act }

	private bool _isShiftDown;
	private ToolMode _mode = ToolMode.Select;
	private readonly EditSymbolFeedback _feedback;
	private readonly FeaturePreview _preview;
	private Point _currentMousePosition;

	private const Key ToggleLassoSelectKey = Key.L;
	private const Key TogglePolygonSelectKey = Key.P;
	private const SketchGeometryType DefaultSketchType = SketchGeometryType.Rectangle;
	// Sketch type transitions: (Lasso) <---'L'---> (Rectangle) <---'P'---> (Polygon)
	//                             ^----------------'P'--/--'L'-----------------^

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected EditSymbolToolBase(EditSymbolFeedback feedback)
	{
		IsSketchTool = true;
		UseSnapping = false;
		UsesCurrentTemplate = false;
		UseSelection = true; // so we get OnSelectionChanged calls

		SketchOutputMode = SketchOutputMode.Map;
		CompleteSketchOnMouseUp = true; // must set before sketch type
		SketchType = DefaultSketchType;
		GeomIsSimpleAsFeature = false; // so just a click gives a non-empty polygon/extent
		FireSketchEvents = false;
		IsWYSIWYG = false; // do not symbolize sketch

		ContextMenuID = "esri_mapping_selection2DContextMenu";

		_feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
		_preview = new FeaturePreview { TransparencyPercent = 33 };
	}

	protected ToolMode Mode => _mode;

	protected EditSymbolFeedback DisplayFeedback => _feedback;

	protected FeaturePreview FeaturePreview => _preview;

	#region Customizations

	protected abstract IPickerPrecedence CreatePickerPrecedence(
		Geometry sketchGeometry, Point screenPosition);

	protected virtual bool AllowMultipleSelection => true;

	protected virtual bool AllowEscapeToDefaultTool => false;

	/// <returns>The context menu instance or null for none</returns>
	protected abstract ContextMenu GetContextMenu(Point screenLocation);

	#endregion

	private bool HasSelection => (ActiveMapView?.Map?.SelectionCount ?? 0) > 0;

	/// <summary>Clear the selection on the active map</summary>
	/// <remarks>Must call on MCT</remarks>
	private void ClearSelection()
	{
		var map = ActiveMapView?.Map;
		map?.ClearSelection();
	}

	private void ResyncModifiers()
	{
		// We don't get Key up/down events while a Popup is open;
		// re-synchronize our modifier state explicitly now:
		_isShiftDown = KeyboardUtils.IsShiftDown();
	}

	#region MapTool overrides (entry points)

	protected override async Task OnToolActivateAsync(bool hasMapViewChanged)
	{
		Gateway.LogEntry(_msg);

		try
		{
			ResyncModifiers();

			IsSketchTool = true;
			UseSnapping = false;
			UseSelection = true; // set true to receive OnSelectionChangedAsync

			EditCompletedEvent.Unsubscribe(OnEditCompletedAsync); // in case we missed OnToolDeactivate
			EditCompletedEvent.Subscribe(OnEditCompletedAsync);

			EnterMode(ToolMode.Select);

			if (HasSelection)
			{
				await QueuedTask.Run(TryEnterActionMode);
			}
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
	{
		try
		{
			EditCompletedEvent.Unsubscribe(OnEditCompletedAsync);

			CancelAction();
			RemoveAllOverlays();
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}

		return Task.CompletedTask;
	}

	protected override async Task OnSelectionChangedAsync(MapSelectionChangedEventArgs args)
	{
		try
		{
			CancelAction();

			EnterMode(ToolMode.Select);

			if (HasSelection && !_isShiftDown)
			{
				await QueuedTask.Run(TryEnterActionMode);
			}
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}

	protected override void OnToolKeyDown(MapViewKeyEventArgs args)
	{
		try
		{
			if (args.Key is Key.LeftShift or Key.RightShift)
			{
				_isShiftDown = true;
				args.Handled = false;

				if (_mode != ToolMode.Select)
				{
					CancelAction();
					EnterMode(ToolMode.Select);
				}
			}
			else if (args.Key == Key.Escape)
			{
				var keepActivated = !AllowEscapeToDefaultTool;
				args.Handled = _mode == ToolMode.Act || HasSelection || keepActivated;
			}
			else if (args.Key == ToggleLassoSelectKey)
			{
				ToggleLassoSelect();
			}
			else if (args.Key == TogglePolygonSelectKey)
			{
				TogglePolygonSelect();
			}
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}

	protected override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
	{
		try
		{
			if (args.Key == Key.Escape)
			{
				if (_mode == ToolMode.Act)
				{
					CancelAction();
					EnterMode(ToolMode.Select);
				}
				else
				{
					await QueuedTask.Run(ClearSelection);
				}
			}
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}
	}

	protected override async void OnToolKeyUp(MapViewKeyEventArgs args)
	{
		try
		{
			if (args.Key is Key.LeftShift or Key.RightShift)
			{
				_isShiftDown = false;
				args.Handled = false;

				if (_mode == ToolMode.Select && HasSelection)
				{
					await QueuedTask.Run(TryEnterActionMode);
				}
			}
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}

	private Task OnEditCompletedAsync(EditCompletedEventArgs args)
	{
		try
		{
			if (args.CompletedType is EditCompletedType.Undo or EditCompletedType.Redo)
			{
				// Our feature(s) MAY have changed:
				// cancel the action (if any) but stay in current mode
				CancelAction();
			}
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}

		return Task.CompletedTask;
	}

	protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
	{
		try
		{
			if (geometry is null) return false;

			// TODO detect and skip duplicate calls? (see OneClickToolBase)

			if (_mode == ToolMode.Select)
			{
				await QueuedTask.Run(() => PerformSelection(geometry));
			}

			// Rely on Selection Changed being called...
			//if (HasSelection && ! _isShiftDown)
			//{
			//	await QueuedTask.Run(TryEnterActionMode);
			//}
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}

		return true;
	}

	protected override void OnToolMouseDown(MapViewMouseButtonEventArgs args)
	{
		args.Handled = _mode == ToolMode.Act;
	}

	protected override async Task HandleMouseDownAsync(MapViewMouseButtonEventArgs args)
	{
		try
		{
			if (args.ChangedButton == MouseButton.Right)
			{
				CancelAction();

				var screenPosition = ActiveMapView.ClientToScreen(args.ClientPoint);
				ShowContextMenu(screenPosition);
			}
			else if (args.ChangedButton == MouseButton.Left)
			{
				await QueuedTask.Run(() =>
				{
					bool ok = StartActionMCT(args.ClientPoint);

					if (! ok)
					{
						EnterMode(ToolMode.Select);
					}
				});
			}
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}
	}

	protected override async void OnToolMouseMove(MapViewMouseEventArgs args)
	{
		try
		{
			_currentMousePosition = args.ClientPoint;

			// notice there's no HandleMouseMoveAsync(), so do the magic here!
			if (_mode != ToolMode.Act) return;
			if (! IsInAction) return;

			if (QueuedTask.Busy) return; // a bit brute, but should avoid overload

			await QueuedTask.Run(() => { MoreActionMCT(args.ClientPoint); });
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}

	protected override void OnToolMouseUp(MapViewMouseButtonEventArgs args)
	{
		args.Handled = _mode == ToolMode.Act && args.ChangedButton == MouseButton.Left;
	}

	protected override async Task HandleMouseUpAsync(MapViewMouseButtonEventArgs args)
	{
		if (args.ChangedButton != MouseButton.Left) return;

		if (_mode != ToolMode.Act) return;
		if (!IsInAction) return;

		try
		{
			await QueuedTask.Run(() =>
			{
				EndActionMCT(args.ClientPoint);
				CancelAction();
				EnterMode(ToolMode.Act);
			});
		}
		catch (Exception ex)
		{
			Gateway.ReportError(ex, _msg);
		}
		finally
		{
			CancelAction();
		}
	}

	#endregion

	/// <remarks>Must run on MCT</remarks>
	private void TryEnterActionMode()
	{
		if (CanEnterActionMode(out string message))
		{
			if (! string.IsNullOrEmpty(message))
			{
				_msg.Info(message); // TODO display in OverlayControl?
			}

			EnterMode(ToolMode.Act);
		}
		else
		{
			_msg.Info($"Cannot {ActionVerb}: {message}");
			EnterMode(ToolMode.Select);
		}
	}

	protected void EnterMode(ToolMode mode)
	{
		switch (mode)
		{
			case ToolMode.Select:
				SketchOutputMode = SketchOutputMode.Map;
				CompleteSketchOnMouseUp = true; // set before SketchType!?
				SketchType = DefaultSketchType;
				GeomIsSimpleAsFeature = false; // so just a click gives a non-empty polygon/extent
				SetupSelectMode();
				break;

			case ToolMode.Act:
				// No sketch, just WYSIWYG rotation
				SketchType = SketchGeometryType.None;
				SetupActionMode();
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, "unknown tool mode");
		}

		_mode = mode;
	}

	protected virtual void SetupSelectMode() { }

	protected virtual void SetupActionMode() { }

	protected abstract string ActionVerb { get; }

	protected abstract bool CanEnterActionMode(out string message);

	protected abstract bool IsInAction { get; }

	protected abstract bool StartActionMCT(Point clientPoint);

	protected abstract void MoreActionMCT(Point clientPoint);

	protected abstract void EndActionMCT(Point clientPoint);

	protected abstract void CancelAction();

	private void ToggleLassoSelect()
	{
		if (_mode != ToolMode.Select) return;

		if (SketchType == SketchGeometryType.Lasso)
		{
			SketchType = DefaultSketchType;
			_msg.Info($"{Caption}: Select vertices using a rectangle sketch");
		}
		else
		{
			SketchType = SketchGeometryType.Lasso;
			_msg.Info($"{Caption}: Select vertices using a lasso sketch");
		}
	}

	private void TogglePolygonSelect()
	{
		if (_mode != ToolMode.Select) return;

		if (SketchType == SketchGeometryType.Polygon)
		{
			SketchType = DefaultSketchType;
			_msg.Info($"{Caption}: Select vertices using a rectangle sketch");
		}
		else
		{
			SketchType = SketchGeometryType.Polygon;
			_msg.Info($"{Caption}: Select vertices using a polygon sketch");
		}
	}

	protected void RemoveAllOverlays()
	{
		_preview.Clear();
		_feedback.Clear();
	}

	/// <remarks>Must call on MCT</remarks>>
	protected Candidates PrepareAction(out string message)
	{
		// - all features have same symbol (implies from same layer)
		// - symbol has field override for at least one suitable property

		var activeMap = ActiveMapView?.Map;
		if (activeMap is null)
		{
			message = "No active map";
			return null;
		}

		var selection = activeMap.GetSelection();
		if (selection.IsEmpty)
		{
			message = "No feature selected";
			return null;
		}

		var dict = selection.ToDictionary<FeatureLayer>();
		if (dict.Count < 1)
		{
			message = "No feature selected";
			return null;
		}

		if (dict.Count > 1)
		{
			message = "More than one layer selected";
			return null;
		}

		var single = dict.Single();
		var layer = single.Key;
		var oids = single.Value;

		if (oids == null || oids.Count < 1)
		{
			message = "No feature selected";
			return null;
		}

		var scaleDenom = activeMap.ReferenceScale;
		var renderer = layer.GetRenderer();
		var candidates = new Candidates(layer, scaleDenom);

		if (!AllowMultipleSelection && oids.Count > 1)
		{
			message = "More than one feature selected";
			return null;
		}

		// TODO optimize: get all features at once (not one by one)
		foreach (var oid in oids)
		{
			//var symbol = layer.LookupSymbol(oid, ActiveMapView);
			// it's CIMSymbol (no overrides), not CIMSymbolReference (having overrides)

			var feature = GetFeature(layer, oid);
			var shape = feature.GetShape();
			var values = new NamedValues(feature);
			var symref = SymbolUtils.GetSymbol(renderer, values, scaleDenom, out var overrides);
			// NB: symref has no PrimitiveOverrides: they were applied by GetSymbol()

			candidates.AddFeature(oid, shape, symref);

			if (overrides is null || overrides.Length < 1)
			{
				continue; // symbol has no primitive overrides
			}

			foreach (var po in overrides)
			{
				if (!IsSuitableOverride(po)) continue;
				var fieldName = SymbolUtils.GetOverrideField(po);
				if (fieldName is null) continue; // not a plain field override (but an expression)
				int fieldIndex = feature.FindField(fieldName);
				if (fieldIndex < 0) continue; // no such field
				var currentValue = feature[fieldIndex];

				candidates.AddOverride(po, fieldName, currentValue);
			}
		}

		var ok = candidates.Validate(out message);
		return ok ? candidates : null;
	}

	protected abstract bool IsSuitableOverride(CIMPrimitiveOverride po);

	private static Feature GetFeature(FeatureLayer layer, long oid)
	{
		using var featureClass = layer.GetFeatureClass();
		if (featureClass is null) return null;
		return GdbQueryUtils.GetFeature(featureClass, oid);
	}

	#region Context menu

	private void ShowContextMenu(Point screenLocation)
	{
		var contextMenu = GetContextMenu(screenLocation);

		if (contextMenu is not null)
		{
			contextMenu.Closed -= OnContextMenuClosed; // avoid accumulation
			contextMenu.Closed += OnContextMenuClosed;
			contextMenu.IsOpen = true;
		}
	}

	private void OnContextMenuClosed(object o, RoutedEventArgs e)
	{
		ResyncModifiers();
	}

	#endregion

	#region Feature selection

	private async Task PerformSelection(Geometry sketchGeometry)
	{
		Point screenPosition = ActiveMapView.ClientToScreen(_currentMousePosition);
		using var pickerPrecedence = CreatePickerPrecedence(sketchGeometry, screenPosition);

		IEnumerable<FeatureSelectionBase> candidates =
			FindFeaturesOfAllLayers(pickerPrecedence.GetSelectionGeometry(),
			                        pickerPrecedence.SpatialRelationship);

		List<IPickableItem> items = await  PickerUtils.GetItems(candidates, pickerPrecedence);

		PickerUtils.Select(items, pickerPrecedence.SelectionCombinationMethod);

		ResyncModifiers();

		// ProcessSelection()
	}

	private IEnumerable<FeatureSelectionBase> FindFeaturesOfAllLayers(
		[NotNull] Geometry searchGeometry,
		SpatialRelationship spatialRelationship,
		[CanBeNull] CancelableProgressor progressor = null)
	{
		var mapView = ActiveMapView;

		if (mapView is null)
		{
			return Enumerable.Empty<FeatureSelectionBase>();
		}

		var featureFinder = new FeatureFinder(mapView)
		                    {
			                    SpatialRelationship = spatialRelationship,
			                    DelayFeatureFetching = true
		                    };

		const Predicate<Feature> featurePredicate = null;
		return featureFinder.FindFeaturesByLayer(searchGeometry, CanSelectFromLayer,
		                                         featurePredicate, progressor);
	}

	private bool CanSelectFromLayer([CanBeNull] Layer layer)
	{
		if (layer is not FeatureLayer featureLayer)
		{
			return false; // not a feature layer
		}

		if (!LayerUtils.IsVisible(layer))
		{
			return false; // layer not visible
		}

		if (!layer.IsVisibleInView(ActiveMapView))
		{
			return false; // not visible on map
		}

		if (!featureLayer.IsSelectable)
		{
			return false;
		}

		if (!featureLayer.IsEditable)
		{
			return false;
		}

		// TODO what ever you desire... geometryType, anything

		return true;
	}

	#endregion

	#region Nested type: Candidates

	protected class Candidates : IEnumerable<Candidates.Candidate>, IDisposable
	{
		private readonly List<Candidate> _candidates = new();

		public FeatureLayer Layer { get; }
		public double ReferenceScale { get; }

		private Candidate CurrentCandidate => // always the last added candidate
			_candidates.Count < 1 ? null : _candidates[_candidates.Count - 1];

		public Candidates(FeatureLayer layer, double referenceScale = -1)
		{
			Layer = layer ?? throw new ArgumentNullException(nameof(layer));
			ReferenceScale = referenceScale;
		}

		public void AddFeature(long oid, Geometry shape, CIMSymbolReference symbol)
		{
			_candidates.Add(new Candidate(oid, shape, symbol));
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<Candidate> GetEnumerator()
		{
			foreach (var candidate in _candidates)
			{
				yield return candidate;
			}
		}

		public void AddOverride(CIMPrimitiveOverride po, string fieldName, object currentValue)
		{
			if (po is null)
				throw new ArgumentNullException(nameof(po));
			if (string.IsNullOrEmpty(fieldName))
				throw new ArgumentNullException(nameof(fieldName));
			if (CurrentCandidate is null)
				throw new InvalidOperationException("Must first add a feature");

			CurrentCandidate.AddOverride(po, fieldName, currentValue);
		}

		public bool Validate(out string message)
		{
			if (_candidates.Count < 0)
			{
				message = "No features selected";
				return false;
			}

			var suitableCount = _candidates.Count(c => c.Overrides.Any());
			if (suitableCount <= 0)
			{
				message = "No feature(s) have suitable override(s)";
				return false;
			}

			if (suitableCount < _candidates.Count)
			{
				// we can proceed, but should warn
				var oids = _candidates.Where(c => !c.Overrides.Any()).Select(c => c.OID);
				var oidsText = string.Join(", ", oids);
				message = $"Features have no suitable primitive overrides: OID {oidsText}";
			}
			else
			{
				message = null;
			}

			return true;
		}

		public MapPoint ReferencePoint => Centroid(CurrentCandidate.Shape);

		private static MapPoint Centroid(Geometry shape)
		{
			if (shape is null) return null;
			if (shape.IsEmpty) return null;

			try
			{
				return GeometryUtils.Centroid(shape);
			}
			catch (Exception ex)
			{
				_msg.Debug($"Cannot get centroid: {ex.Message}", ex);

				return null;
			}
		}

		public void Dispose()
		{
			// nothing to dispose for now
		}

		public class Candidate : IDisposable
		{
			private readonly List<Override> _overrides = new();

			public long OID { get; }
			public Geometry Shape { get; }
			public CIMSymbolReference Symbol { get; }
			public IReadOnlyList<Override> Overrides { get; }

			public Candidate(long oid, Geometry shape, CIMSymbolReference symbol)
			{
				OID = oid;
				Shape = shape ?? throw new ArgumentNullException(nameof(shape));
				Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
				Overrides = new ReadOnlyCollection<Override>(_overrides);
			}

			public void AddOverride(CIMPrimitiveOverride po, string fieldName, object currentValue)
			{
				if (po is null)
					throw new ArgumentNullException(nameof(po));
				if (string.IsNullOrEmpty(fieldName))
					throw new ArgumentNullException(nameof(fieldName));

				_overrides.Add(new Override(po.PrimitiveName, po.PropertyName, fieldName, currentValue));
			}

			public void Dispose()
			{
				// nothing to dispose for now
			}
		}

		public readonly struct Override
		{
			public string PrimitiveName { get; }
			public string PropertyName { get; }
			public string FieldName { get; }
			public object CurrentValue { get; }

			public Override(string primitiveName, string propertyName, string fieldName, object currentValue)
			{
				PrimitiveName = primitiveName ?? throw new ArgumentNullException(nameof(primitiveName));
				PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
				FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
				CurrentValue = currentValue; // can be null
			}
		}
	}

	#endregion

	#region Nestsed type: NamedValues

	private class NamedValues : INamedValues
	{
		private readonly Row _row;

		public NamedValues(Row row)
		{
			_row = row ?? throw new ArgumentNullException(nameof(row));
		}

		public bool Exists(string name)
		{
			return name is not null && _row.FindField(name) >= 0;
		}

		public object GetValue(string name)
		{
			return _row[name];
		}
	}

	#endregion
}
