using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Keyboard;
using Application = System.Windows.Application;
using Cursor = System.Windows.Input.Cursor;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace ProSuite.AGP.Editing.OneClick
{
	public abstract class OneClickToolBase : MapTool
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private const Key _keyShowOptionsPane = Key.O;

		protected bool RequiresSelection { get; set; } = true;

		protected List<Key> HandledKeys { get; } = new List<Key>();

		protected Cursor SelectionCursor { get; set; }
		protected Cursor SelectionCursorShift { get; set; }

		protected OneClickToolBase()
		{
			//SketchOutputMode = SketchOutputMode.Screen;
			SketchType = SketchGeometryType.Rectangle;
			UseSnapping = false;

			HandledKeys.Add(Key.Escape);
			HandledKeys.Add(_keyShowOptionsPane);
		}

		protected override Task OnToolActivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug("OnToolActivateAsync");

			MapView.Active.Map.PropertyChanged += Map_PropertyChanged;

			MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);

			try
			{
				return QueuedTask.Run(
					() =>
					{
						OnToolActivatingCore();

						if (RequiresSelection)
						{
							ProcessSelection(SelectionUtils.GetSelectedFeatures(ActiveMapView));
						}

						return OnToolActivatedCore(hasMapViewChanged);
					});
			}
			catch (Exception e)
			{
				HandleError($"Error in tool activation ({Caption}): {e.Message}", e);
			}

			return Task.CompletedTask;
		}

		protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug("OnToolDeactivateAsync");

			MapView.Active.Map.PropertyChanged -= Map_PropertyChanged;

			MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChanged);

			try
			{
				HideOptionsPane();

				return QueuedTask.Run(() => OnToolDeactivateCore(hasMapViewChanged));
			}
			catch (Exception e)
			{
				HandleError($"Error in tool deactivation ({Caption}): {e.Message}", e, true);
			}

			return Task.CompletedTask;
		}

		protected override void OnToolKeyDown(MapViewKeyEventArgs k)
		{
			_msg.VerboseDebug("OnToolKeyDown");

			try
			{
				if (IsModifierKey(k.Key) || HandledKeys.Contains(k.Key))
				{
					k.Handled = true;
				}

				if (k.Key == _keyShowOptionsPane)
				{
					ShowOptionsPane();
				}

				QueuedTaskUtils.Run(
					delegate
					{
						if (k.Key == Key.Escape)
						{
							return HandleEscape();
						}

						if ((k.Key == Key.LeftShift || k.Key == Key.RightShift) &&
						    SelectionCursorShift != null && IsInSelectionPhase())
						{
							SetCursor(SelectionCursorShift);
						}

						OnKeyDownCore(k);

						return true;
					});
			}
			catch (Exception e)
			{
				HandleError($"Error in tool key down ({Caption}): {e.Message}", e, true);
			}
		}

		protected override void OnToolKeyUp(MapViewKeyEventArgs k)
		{
			_msg.VerboseDebug("OnToolKeyUp");

			// TODO: Key pressed management
			//_shiftIsPressed = false;

			try
			{
				QueuedTaskUtils.Run(
					delegate
					{
						if ((k.Key == Key.LeftShift || k.Key == Key.RightShift) &&
						    SelectionCursor != null && IsInSelectionPhase())
						{
							SetCursor(SelectionCursor);
						}

						OnKeyUpCore(k);
						return true;
					});
			}
			catch (Exception e)
			{
				HandleError($"Error in tool key up ({Caption}): {e.Message}", e, true);
			}
		}

		protected override async Task<bool> OnSketchCompleteAsync(Geometry sketchGeometry)
		{
			_msg.VerboseDebug("OnSketchCompleteAsync");

			if (sketchGeometry == null)
			{
				// TODO: if in selection phase select at the current mouse location?
				return false;
			}

			try
			{
				CancelableProgressor progressor = GetCancelableProgressor();

				if (RequiresSelection && IsInSelectionPhase())
				{
					return await QueuedTaskUtils.Run(() => OnSelectionSketchComplete(
						                                 sketchGeometry, progressor));
				}

				return await OnSketchCompleteCoreAsync(sketchGeometry, progressor);
			}
			catch (Exception e)
			{
				HandleError($"{Caption}: Error completing sketch ({e.Message})", e);
				// NOTE: Throwing here results in a process crash (Exception while waiting for a Task to complete)
				// Consider Task.FromException?
			}

			return false;
		}

		protected void StartSelectionPhase()
		{
			SketchOutputMode = SketchOutputMode.Map;
			SketchType = SketchGeometryType.Rectangle;

			UseSnapping = false;

			GeomIsSimpleAsFeature = false;
			CompleteSketchOnMouseUp = true;

			if (KeyboardUtils.IsModifierPressed(Keys.Shift, true))
			{
				SetCursor(SelectionCursorShift);
			}
			else
			{
				SetCursor(SelectionCursor);
			}

			OnSelectionPhaseStarted();
		}

		protected virtual void OnSelectionPhaseStarted() { }

		private static bool IsModifierKey(Key key)
		{
			return key == Key.LeftShift ||
			       key == Key.RightShift ||
			       key == Key.LeftCtrl ||
			       key == Key.RightCtrl ||
			       key == Key.LeftAlt ||
			       key == Key.RightAlt;
		}

		private void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug("OnToolActivateAsync");

			try
			{
				QueuedTaskUtils.Run(
					delegate
					{
						// Used to clear derived geometries etc.
						bool result = OnMapSelectionChangedCore(args);

						return result;
					});
			}
			catch (Exception e)
			{
				HandleError($"Error OnSelectionChanged: {e.Message}", e, true);
			}
		}

		protected virtual void OnToolActivatingCore() { }

		protected virtual bool OnToolActivatedCore(bool hasMapViewChanged)
		{
			return true;
		}

		protected virtual void OnToolDeactivateCore(bool hasMapViewChanged) { }

		protected virtual bool OnMapSelectionChangedCore(MapSelectionChangedEventArgs args)
		{
			return true;
		}

		[CanBeNull]
		protected virtual CancelableProgressor GetCancelableProgressor()
		{
			return null;
		}

		protected virtual Task<bool> OnSketchCompleteCoreAsync(
			[NotNull] Geometry sketchGeometry,
			[CanBeNull] CancelableProgressor progressor)
		{
			return Task.FromResult(true);
		}

		private bool OnSelectionSketchComplete(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			if (SketchOutputMode == SketchOutputMode.Map)
			{
				sketchGeometry =
					MapUtils.ToScreenGeometry(MapView.Active, (Polygon) sketchGeometry);
			}

			Geometry selectionGeometry;
			if (sketchGeometry.Extent.Width > 0 || sketchGeometry.Extent.Height > 0)
			{
				// it seems that screen coordinates are fine!
				selectionGeometry = sketchGeometry;
			}
			else
			{
				// the 'map point' is still screen coordinates...
				selectionGeometry =
					new Coordinate2D(sketchGeometry.Extent.XMin, sketchGeometry.Extent.YMin)
						.ToMapPoint();
			}

			// TODO: Add Utils method to KeyboardUtils to do it in the WPF way
			SelectionCombinationMethod selectionMethod = KeyboardUtils.IsModifierPressed(Keys.Shift)
				                                             ? SelectionCombinationMethod.XOR
				                                             : SelectionCombinationMethod.New;

			bool visualIntersect = SketchOutputMode == SketchOutputMode.Screen;

			// TODO: cycle through the layers, check whether the ShapeType of the layer can be selected,
			//       get the intersecting features with a search cursor, check whether each feature can 
			//       be selected and if there are still several, bring up a picker dialog (if single selection is required)
			Dictionary<BasicFeatureLayer, List<long>> selection = ActiveMapView.SelectFeatures(
				selectionGeometry, selectionMethod, visualIntersect);

			ProcessSelection(SelectionUtils.GetSelectedFeatures(ActiveMapView), progressor);

			// else: feedback to the user to keep selecting
			return true;
		}

		protected virtual bool IsInSelectionPhase()
		{
			return false;
		}

		protected virtual void OnKeyDownCore(MapViewKeyEventArgs k) { }

		protected virtual void OnKeyUpCore(MapViewKeyEventArgs mapViewKeyEventArgs) { }

		private void Map_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnMapPropertyChangedCore(sender, e);
		}

		protected virtual void
			OnMapPropertyChangedCore(object sender, PropertyChangedEventArgs e) { }

		protected virtual void ShowOptionsPane() { }

		protected virtual void HideOptionsPane() { }

		protected abstract bool HandleEscape();

		protected abstract void LogUsingCurrentSelection();

		protected abstract void LogPromptForSelection();

		protected virtual bool CanUseSelection([NotNull] IEnumerable<Feature> selectedFeatures)
		{
			// TODO
			return selectedFeatures.Any();
		}

		protected virtual void AfterSelection(IList<Feature> selectedFeatures,
		                                      CancelableProgressor progressor) { }

		protected void ProcessSelection(IEnumerable<Feature> selectedFeatures,
		                                CancelableProgressor progressor = null)
		{
			// TODO: currently the selection is retrieved twice. Testing the selection should 
			// in the success case return it so that it can be passed to AfterSelection
			// BUT: some genius tools require the selection to be grouped by layer
			IList<Feature> selection = selectedFeatures.ToList();

			if (! CanUseSelection(selection))
			{
				LogPromptForSelection();
				StartSelectionPhase();
				return;
			}

			LogUsingCurrentSelection();

			AfterSelection(selection, progressor);
		}

		protected void HandleError(string message, Exception e, bool noMessageBox = false)
		{
			_msg.Error(message, e);

			if (! noMessageBox)
			{
				Application.Current.Dispatcher.Invoke(
					() =>
					{
						MessageBox.Show(message, "Error", MessageBoxButton.OK,
						                MessageBoxImage.Error);
					});
			}
		}

		protected void SetCursor([CanBeNull] Cursor cursor)
		{
			if (cursor != null)
			{
				Cursor = cursor;
			}
		}
	}
}