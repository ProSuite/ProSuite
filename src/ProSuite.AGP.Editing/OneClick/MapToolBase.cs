using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Env;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.OneClick;

/// <summary>
/// Minimal base class for an ArcGIS Pro map tool that handles the most basic aspects, such as
/// key handling, exception handling and .
/// </summary>
public abstract class MapToolBase : MapTool
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private int _updateErrorCounter;
	private const int MaxUpdateErrors = 10;

	private const Key _keyShowOptionsPane = Key.O;

	/// <summary>
	/// The list of handled keys, i.e. the keys for which <see cref="MapTool.HandleKeyDownAsync" />
	/// will be called (and potentially in the future also MapTool.HandleKeyUpAsync)
	/// </summary>
	protected List<Key> HandledKeys { get; } = new();

	/// <summary>
	/// Whether the SHIFT key currently held has been combined with a key this tool does not
	/// handle itself. That makes it part of someone else's keyboard shortcut rather than the
	/// tool's own SHIFT - for example Right SHIFT + ä, which toggles ArcGIS Pro's Z-only
	/// snapping. The tool's SHIFT behaviour is suspended until SHIFT is released. It is
	/// recognised from the key events, so it works whatever the keyboard layout and whatever
	/// the other key is. The tool's own SHIFT combinations are not affected: their keys are in
	/// <see cref="HandledKeys"/>.
	/// </summary>
	protected bool IsShiftPartOfShortcut { get; private set; }

	/// <summary>
	/// Which key it was, while <see cref="IsShiftPartOfShortcut"/> holds; null otherwise.
	/// <para>
	/// A <see cref="Key"/> is a virtual key, which for the OEM keys is the key's position on the
	/// keyboard rather than the character printed on it, so the same value is expected whatever
	/// the layout - the ä of a Swiss German keyboard and whatever sits in that position
	/// elsewhere. That makes it usable for telling one SHIFT shortcut from another, which
	/// matters where a tool has to react to one of them in particular: ArcGIS Pro's XY-only
	/// snapping publishes no property, so the key is the only sign of it.
	/// </para>
	/// </summary>
	protected Key? ShiftShortcutKey { get; private set; }

	/// <summary>
	/// Called once when the SHIFT key being held turns out to be part of a shortcut (see
	/// <see cref="IsShiftPartOfShortcut"/>), so that a subclass can undo what SHIFT already
	/// started - SHIFT goes down before the shortcut's other key does.
	/// </summary>
	protected virtual void OnShiftBecameShortcut() { }

	/// <summary>
	/// Whether <paramref name="key"/>, pressed while SHIFT is held, makes SHIFT part of a
	/// shortcut (<see cref="IsShiftPartOfShortcut"/>). By default every key the tool does not
	/// handle itself; a tool whose own keys mean something else with SHIFT adds them.
	/// </summary>
	protected virtual bool IsShiftShortcutKey(Key key)
	{
		return ! HandledKeys.Contains(key);
	}

	/// <summary>
	/// The currently pressed keys.
	/// </summary>
	protected HashSet<Key> PressedKeys { get; } = new();

	/// <summary>
	/// The current location of the mouse in the map view in client coordinates (relative to
	/// the top-left corner of the view).
	/// </summary>
	protected Point CurrentMousePosition { get; set; }

	protected MapToolBase()
	{
		UseSnapping = false;

		HandledKeys.Add(Key.Escape);
		HandledKeys.Add(_keyShowOptionsPane);
	}

	protected virtual int GetSelectionTolerancePixels()
	{
		// Subclasses can choose to use fixed settings or some other custom implementation.
		return SelectionEnvironment.SelectionTolerance;
	}

	#region PlugIn overrides

	protected override void OnUpdate()
	{
		try
		{
			OnUpdateCore();
		}
		catch (Exception ex)
		{
			if (_updateErrorCounter < MaxUpdateErrors)
			{
				_msg.Error($"{GetType().Name}.{nameof(OnUpdate)}: {ex.Message}", ex);

				_updateErrorCounter += 1;

				if (_updateErrorCounter == MaxUpdateErrors)
				{
					_msg.Error("Error reporting stopped to avoid flooding the logs");
				}
			}
			//else: silently ignore to avoid flooding the logs
		}
	}

	#endregion

	#region MapTool overrides

	protected override async Task OnToolActivateAsync(bool hasMapViewChanged)
	{
		try
		{
			_msg.VerboseDebug(() => $"{nameof(OnToolActivateAsync)} ({Caption})");

			PressedKeys.Clear();
			IsShiftPartOfShortcut = false;
			ShiftShortcutKey = null;

			// ReSharper disable once MethodHasAsyncOverload
			OnToolActivatingCore();

			await OnToolActivateCoreAsync(hasMapViewChanged);

			// TODO: Activated here...
		}
		catch (Exception e)
		{
			ErrorHandler.HandleError(e, _msg);
		}
	}

	protected override async Task OnToolDeactivateAsync(bool hasMapViewChanged)
	{
		try
		{
			// If hasMapViewChanged: MapTool.OnToolDeactivateAsync() is called twice!
			_msg.VerboseDebug(() => $"{nameof(OnToolDeactivateAsync)} ({Caption})");

			HideOptionsPane();

			OnToolDeactivatingCore();

			await OnToolDeactivateCoreAsync(hasMapViewChanged);
		}
		catch (Exception e)
		{
			ErrorHandler.LogWarn(e, _msg);
		}
	}

	protected override void OnToolKeyDown(MapViewKeyEventArgs args)
	{
		try
		{
			_msg.VerboseDebug(() => nameof(OnToolKeyDown));

			PressedKeys.Add(args.Key);

			// A key this tool does not handle, pressed while SHIFT is held, belongs to a
			// shortcut such as Right SHIFT + ä (ArcGIS Pro's Z-only snapping). Until SHIFT is
			// released it is not the tool's SHIFT.
			if (! IsShiftPartOfShortcut && KeyboardUtils.IsShiftDown() &&
			    ! KeyboardUtils.IsModifierKey(args.Key) && IsShiftShortcutKey(args.Key))
			{
				IsShiftPartOfShortcut = true;
				ShiftShortcutKey = args.Key;
				OnShiftBecameShortcut();
			}

			// NOTE: Only claim a handled key while no modifier is down. Otherwise the tool
			//       swallows application shortcuts that happen to use the same letter, such as
			//       CTRL+SHIFT+O, and they never reach the DAML shortcut table.
			if (KeyboardUtils.IsModifierKey(args.Key) ||
			    (HandledKeys.Contains(args.Key) && ! KeyboardUtils.IsAnyModifierDown()))
			{
				// Trigger the call to HandleKeyDownAsync
				args.Handled = true;
			}

			OnKeyDownCore(args);
		}
		catch (Exception e)
		{
			ErrorHandler.LogError(e, _msg);
		}
	}

	protected override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
	{
		try
		{
			_msg.VerboseDebug(() => nameof(HandleKeyDownAsync));

			if (KeyboardUtils.IsShiftKey(args.Key))
			{
				await ShiftPressedAsync(args);
			}

			if (args.Key == Key.Escape)
			{
				await HandleEscapeAsync();
			}

			if (args.Key == _keyShowOptionsPane && ! KeyboardUtils.IsAnyModifierDown())
			{
				await UIEnvironment.ReleaseCursorAsync();

				ShowOptionsPane();
			}

			await HandleKeyDownCoreAsync(args);
		}
		catch (Exception e)
		{
			ErrorHandler.LogError(e, _msg);
		}
	}

	protected override void OnToolKeyUp(MapViewKeyEventArgs args)
	{
		try
		{
			_msg.VerboseDebug(() => nameof(OnToolKeyUp));

			OnKeyUpCore(args);

			// NOTE: The HandleKeyUpAsync is only called for handled keys.
			// However, they will not perform the standard functionality devised by the
			// application! Examples: F8 (Toggle stereo fixed cursor mode), B (snap to ground, ...)
			if (KeyboardUtils.IsModifierKey(args.Key) || HandledKeys.Contains(args.Key))
			{
				args.Handled = true;
			}
		}
		catch (Exception e)
		{
			ErrorHandler.LogError(e, _msg);
		}
		finally
		{
			PressedKeys.Remove(args.Key);
		}
	}

	protected override async Task HandleKeyUpAsync(MapViewKeyEventArgs args)
	{
		try
		{
			_msg.VerboseDebug(() => nameof(HandleKeyUpAsync));

			await HandleKeyUpCoreAsync(args);
		}
		catch (Exception e)
		{
			// Use ErrorHandler to allow custom implementation of DialogService
			ErrorHandler.HandleError(e, _msg);
		}
		finally
		{
			// Only now, after the release was handled: the handlers need to know that the SHIFT
			// being released was a shortcut's.
			if (KeyboardUtils.IsShiftKey(args.Key))
			{
				IsShiftPartOfShortcut = false;
				ShiftShortcutKey = null;
			}
		}
	}

	protected override void OnToolMouseDown(MapViewMouseButtonEventArgs args)
	{
		try
		{
			_msg.VerboseDebug(() => $"{nameof(OnToolMouseDown)} ({Caption})");

			OnToolMouseDownCore(args);

			// NOTE: If args.Handled = true the HandleMouseDownAsync/HandleMouseUpAsync
			//       methods are called. However, no sketch is created and (OnSketchFinishedAsync)
			//       is not called, even if IsSketchTool = true.

			// In order to get the HandleMouseDownAsync method call, do this:
			// Ensure the -Async overload is called
			//args.Handled = true;
		}
		catch (Exception e)
		{
			ErrorHandler.HandleError(e, _msg);
		}
	}

	protected override async Task HandleMouseDownAsync(MapViewMouseButtonEventArgs args)
	{
		try
		{
			await OnToolMouseDownCoreAsync(args);
		}
		catch (Exception e)
		{
			ErrorHandler.HandleError(e, _msg);
		}
	}

	protected override async void OnToolDoubleClick(MapViewMouseButtonEventArgs args)
	{
		try
		{
			_msg.VerboseDebug(() => $"{nameof(OnToolDoubleClick)} ({Caption})");

			await OnToolDoubleClickCoreAsync(args);
		}
		catch (Exception ex)
		{
			ErrorHandler.HandleError(ex, _msg);
		}
	}

	// TODO: Async Double-Click, etc.

	protected override void OnToolMouseMove(MapViewMouseEventArgs args)
	{
		ViewUtils.Try(() =>
		              {
			              CurrentMousePosition = args.ClientPoint;

			              OnToolMouseMoveCore(args);
		              }, _msg,
		              suppressErrorMessageBox: true);
	}

	protected override void OnToolMouseUp(MapViewMouseButtonEventArgs args)
	{
		try
		{
			_msg.VerboseDebug(() => $"{nameof(OnToolMouseUp)} ({Caption})");

			OnToolMouseUpCore(args);

			// NOTE: If args.Handled = true the HandleMouseDownAsync/HandleMouseUpAsync
			//       methods are called. However, no sketch is created and (OnSketchFinishedAsync)
			//       is not called, even if IsSketchTool = true.

			// In order to get the HandleMouseUpAsync method call, do this:
			//args.Handled = true;
		}
		catch (Exception e)
		{
			ErrorHandler.HandleError(e, _msg);
		}
	}

	protected override async Task HandleMouseUpAsync(MapViewMouseButtonEventArgs args)
	{
		try
		{
			_msg.VerboseDebug(() => $"{nameof(HandleMouseUpAsync)} ({Caption})");

			await OnToolMouseUpCoreAsync(args);
		}
		catch (Exception e)
		{
			ErrorHandler.HandleError(e, _msg);
		}
	}

	protected override async Task<bool> OnSketchCompleteAsync(Geometry sketchGeometry)
	{
		try
		{
			_msg.VerboseDebug(() => $"{nameof(OnSketchCompleteAsync)} ({Caption})");

			using var source = GetProgressorSource();
			CancelableProgressor progressor = source?.Progressor;

			return await OnSketchFinishedAsync(sketchGeometry, progressor);
		}
		catch (Exception e)
		{
			// Consider Task.FromException? --> no, as it throws once awaited!
			ErrorHandler.HandleError(
				$"{Caption}: Error completing sketch ({e.Message})", e, _msg);

			return await Task.FromResult(true);
		}
	}

	#endregion

	#region Core methods to implement by subclasses

	protected virtual void OnUpdateCore() { }

	/// <remarks>Called first when the tool is activated. Will be called on GUI thread</remarks>
	protected virtual void OnToolActivatingCore() { }

	protected abstract Task OnToolActivateCoreAsync(bool hasMapViewChanged);

	/// <remarks>Called first on de-activation of the tool. Will be called on GUI thread</remarks>
	protected virtual void OnToolDeactivatingCore() { }

	protected abstract Task OnToolDeactivateCoreAsync(bool hasMapViewChanged);

	protected virtual void OnKeyDownCore(MapViewKeyEventArgs args) { }

	protected virtual void OnKeyUpCore(MapViewKeyEventArgs args) { }

	protected virtual Task HandleKeyDownCoreAsync(MapViewKeyEventArgs args)
	{
		return Task.CompletedTask;
	}

	protected virtual Task HandleKeyUpCoreAsync(MapViewKeyEventArgs args)
	{
		return Task.CompletedTask;
	}

	protected virtual void OnToolMouseDownCore(MapViewMouseButtonEventArgs args) { }

	/// <summary>
	/// Handles the mouse down event. This method is called only if the Handled property of the
	/// event args in <see cref="OnToolMouseDownCore"/> is set to true.
	/// </summary>
	/// <param name="args"></param>
	/// <returns></returns>
	protected virtual Task OnToolMouseDownCoreAsync(MapViewMouseButtonEventArgs args)
	{
		return Task.CompletedTask;
	}

	protected virtual void OnToolMouseMoveCore(MapViewMouseEventArgs args) { }

	protected virtual void OnToolMouseUpCore(MapViewMouseButtonEventArgs args) { }

	/// <summary>
	/// Handles the mouse up event. This method is called only if the Handled property of the
	/// event args in <see cref="OnToolMouseUpCore"/> is set to true.
	/// </summary>
	/// <param name="args"></param>
	/// <returns></returns>
	protected virtual Task OnToolMouseUpCoreAsync(MapViewMouseButtonEventArgs args)
	{
		return Task.CompletedTask;
	}

	protected virtual Task OnToolDoubleClickCoreAsync(MapViewMouseButtonEventArgs args)
	{
		return Task.CompletedTask;
	}

	protected virtual Task<bool> OnSketchFinishedAsync(
		[NotNull] Geometry sketchGeometry,
		[CanBeNull] CancelableProgressor progressor)
	{
		return Task.FromResult(true);
	}

	#endregion

	protected virtual Task ShiftPressedAsync(MapViewKeyEventArgs keyArgs)
	{
		return Task.CompletedTask;
	}

	protected abstract Task HandleEscapeAsync();

	protected virtual void ShowOptionsPane() { }

	protected virtual void HideOptionsPane() { }

	/// <summary>
	/// Override and return null to don't show progressor.
	/// </summary>
	[CanBeNull]
	protected virtual CancelableProgressorSource GetProgressorSource()
	{
		// NOTE: Tools that support the picker are currently not compatible with a progressor
		//       ArcGIS Pro crashes, whenever the picker and the progress window are both open.

		// Subclasses shall individually configure the progressor source
		return null;
	}

	/// <summary>
	/// The height of the feature the given sketch point snapped to, or NaN where it did not
	/// snap to anything that has one.
	/// </summary>
	protected double GetSnappedZ([NotNull] MapPoint sketchPoint)
	{
		IReadOnlyList<SnapResult> snapResults = SnappingResults;

		if (snapResults == null || sketchPoint.IsEmpty)
		{
			return double.NaN;
		}

		// The snapping results live on while the cursor moves: only a result the sketch
		// point actually landed on is the one this click snapped to. The two are the same
		// location, but they get there by different routes, so they are compared with the
		// tolerance of the data - and never tighter than double arithmetic can tell apart.
		double tolerance =
			MathUtils.GetDoubleSignificanceEpsilon(sketchPoint.X, sketchPoint.Y);

		double xyTolerance = GeometryUtils.GetXyTolerance(sketchPoint);

		if (xyTolerance > tolerance)
		{
			tolerance = xyTolerance;
		}

		foreach (SnapResult snapResult in snapResults)
		{
			if (snapResult == null || snapResult.SnapType == SnapType.None)
			{
				continue;
			}

			MapPoint snapLocation = snapResult.SnapLocation;

			if (snapLocation == null || snapLocation.IsEmpty || ! snapLocation.HasZ ||
			    double.IsNaN(snapLocation.Z))
			{
				continue;
			}

			if (! MathUtils.AreEqual(snapLocation.X, sketchPoint.X, tolerance) ||
			    ! MathUtils.AreEqual(snapLocation.Y, sketchPoint.Y, tolerance))
			{
				continue;
			}

			SnapType snapType = snapResult.SnapType;
			double snappedZ = snapLocation.Z;

			_msg.VerboseDebug(() => $"Sketch point snapped to {snapType} at Z {snappedZ}.");

			return snappedZ;
		}

		return double.NaN;
	}

	protected void SetToolCursor([CanBeNull] Cursor cursor)
	{
		if (cursor == null)
		{
			return;
		}

		if (Application.Current.Dispatcher.CheckAccess())
		{
			Cursor = cursor;
		}
		else
		{
			Application.Current.Dispatcher.Invoke(() => { Cursor = cursor; });
		}
	}
}
