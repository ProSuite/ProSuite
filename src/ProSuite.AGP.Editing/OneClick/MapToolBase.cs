using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
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

			if (KeyboardUtils.IsModifierKey(args.Key) || HandledKeys.Contains(args.Key))
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

			if (args.Key == _keyShowOptionsPane)
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
