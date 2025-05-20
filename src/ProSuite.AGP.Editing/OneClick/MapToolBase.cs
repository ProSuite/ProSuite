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
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.OneClick
{
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

		protected static int GetSelectionTolerancePixels()
		{
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
			catch (Exception ex)
			{
				ErrorHandler.HandleError(ex, _msg);
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
			catch (Exception ex)
			{
				_msg.Warn(ex.Message, ex);
			}
		}

		protected override void OnToolKeyDown(MapViewKeyEventArgs args)
		{
			ViewUtils.Try(() =>
			{
				_msg.VerboseDebug(() => nameof(OnToolKeyDown));

				PressedKeys.Add(args.Key);

				if (KeyboardUtils.IsModifierKey(args.Key) || HandledKeys.Contains(args.Key))
				{
					// Trigger the call to HandleKeyDownAsync
					args.Handled = true;
				}

				if (args.Key == _keyShowOptionsPane)
				{
					ShowOptionsPane();
				}

				OnKeyDownCore(args);
			}, _msg, suppressErrorMessageBox: true);
		}

		protected override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
		{
			try
			{
				_msg.VerboseDebug(() => nameof(HandleKeyDownAsync));

				if (KeyboardUtils.IsShiftKey(args.Key))
				{
					await ShiftPressedAsync();
				}

				if (args.Key == Key.Escape)
				{
					await HandleEscapeAsync();
				}

				await HandleKeyDownCoreAsync(args);
			}
			catch (Exception ex)
			{
				// Use ErrorHandler to allow custom implementation of DialogService
				ErrorHandler.HandleError(ex, _msg);
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
				ViewUtils.ShowError(e, _msg, true);
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
			catch (Exception ex)
			{
				// Use ErrorHandler to allow custom implementation of DialogService
				ErrorHandler.HandleError(ex, _msg);
			}
		}

		protected override void OnToolMouseDown(MapViewMouseButtonEventArgs args)
		{
			try
			{
				_msg.VerboseDebug(() => $"{nameof(OnToolMouseDown)} ({Caption})");

				OnToolMouseDownCore(args);

				// Ensure the -Async overload is called
				args.Handled = true;
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

		// TODO: Async Double-Click, Mouse Down, Mouse up etc.

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

				// Ensure the -Async overload is called
				args.Handled = true;
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

		protected virtual Task OnToolMouseDownCoreAsync(MapViewMouseButtonEventArgs args)
		{
			return Task.CompletedTask;
		}

		protected virtual void OnToolMouseMoveCore(MapViewMouseEventArgs args) { }

		protected virtual void OnToolMouseUpCore(MapViewMouseButtonEventArgs args) { }

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

		protected virtual Task ShiftPressedAsync()
		{
			return Task.CompletedTask;
		}

		protected abstract Task HandleEscapeAsync();

		protected virtual void ShowOptionsPane() { }

		protected virtual void HideOptionsPane() { }

		[CanBeNull]
		protected virtual CancelableProgressorSource GetProgressorSource()
		{
			// NOTE: Tools that support thea picker are currently not compatible with a progressor
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
}
