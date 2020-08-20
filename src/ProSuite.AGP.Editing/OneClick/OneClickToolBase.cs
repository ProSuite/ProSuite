using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Keyboard;
using Cursor = System.Windows.Input.Cursor;

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

		protected void SetCursor([CanBeNull] Cursor cursor)
		{
			if (cursor != null)
			{
				if (cursor != SelectionCursor &&
				    cursor != SelectionCursorShift)
				{
					_msg.Info("Other cursor");
				}
				_msg.InfoFormat("Setting cursor {0}",
				                cursor == SelectionCursor ? "Selection" : Environment.StackTrace);
				Cursor = cursor;
			}
		}

		protected virtual void OnSelectionPhaseStarted() { }

		protected static Task<bool> RunQueuedTask(Func<bool> function,
		                                          CancelableProgressor progressor = null)
		{
			// NOTE: if the progressor is null, there's an argument exception... and a crash
			// TODO: Use Result class (such as https://gist.github.com/vkhorikov/7852c7606f27c52bc288)
			// with Either type monad that allows transportation of both return values and failure information
			// to be used by the caller e.g in a message box.

			// NOTE: Throwing into the queued task results in crash

			bool FunctionToQueue()
			{
				try
				{
					return function();
				}
				catch (Exception e)
				{
					_msg.Error(e.Message, e);

					return false;
				}
			}

			Task<bool> result = progressor == null
				                    ? QueuedTask.Run(FunctionToQueue)
				                    : QueuedTask.Run(FunctionToQueue, progressor);

			return result;
		}

		protected override Task OnToolActivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug("OnToolActivateAsync");

			MapView.Active.Map.PropertyChanged += Map_PropertyChanged;

			MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);

			return QueuedTask.Run(
				() =>
				{
					try
					{
						OnToolActivatingCore();

						if (RequiresSelection)
						{
							ProcessSelection(SelectionUtils.GetSelectedFeatures(ActiveMapView));
						}

						return OnToolActivatedCore(hasMapViewChanged);
					}
					catch (Exception e)
					{
						_msg.Error(e.Message, e);

						return false;
					}
				});
		}

		protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
		{
			_msg.VerboseDebug("OnToolDeactivateAsync");

			MapView.Active.Map.PropertyChanged -= Map_PropertyChanged;

			MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChanged);

			HideOptionsPane();

			return QueuedTask.Run(() =>
			                      {
				                      try
				                      {
					                      OnToolDeactivateCore(hasMapViewChanged);
				                      }
				                      catch (Exception e)
				                      {
					                      _msg.Error(e.Message, e);
				                      }
			                      });
		}

		protected override void OnToolKeyDown(MapViewKeyEventArgs k)
		{
			_msg.VerboseDebug("OnToolKeyDown");

			if (IsModifierKey(k.Key) || HandledKeys.Contains(k.Key))
			{
				k.Handled = true;
			}

			if (k.Key == _keyShowOptionsPane)
			{
				ShowOptionsPane();
			}

			RunQueuedTask(
				delegate
				{
					if (k.Key == Key.Escape)
					{
						return HandleEscape();
					}

					// NOTE: There is no performance penalty when setting the cursor from the QueuedTask
					if ((k.Key == Key.LeftShift || k.Key == Key.RightShift) &&
					    SelectionCursorShift != null && IsInSelectionPhase())
					{
						SetCursor(SelectionCursorShift);
					}

					OnKeyDownCore(k);

					return true;
				});
		}

		protected override void OnToolKeyUp(MapViewKeyEventArgs k)
		{
			_msg.VerboseDebug("OnToolKeyUp");

			// TODO: Key pressed management
			//_shiftIsPressed = false;

			RunQueuedTask(
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

		/// <summary>
		///     Called when the sketch finishes. This is where we will create the sketch operation and then execute it.
		/// </summary>
		/// <param name="sketchGeometry">The sketchGeometry created by the sketch.</param>
		/// <returns>A Task returning a Boolean indicating if the sketch complete event was successfully handled.</returns>
		protected override Task<bool> OnSketchCompleteAsync(Geometry sketchGeometry)
		{
			_msg.VerboseDebug("OnSketchCompleteAsync");

			if (sketchGeometry == null)
			{
				// TODO: if in selection phase select at the current mouse location?
				return Task.FromResult(false);
			}

			CancelableProgressor progressor = GetCancelableProgressor();

			return
				RunQueuedTask(
					() => RequiresSelection && IsInSelectionPhase()
						      ? OnSelectionSketchComplete(sketchGeometry, progressor)
						      : OnSketchCompleteCore(sketchGeometry, progressor), progressor);
		}

		private static bool IsModifierKey(Key key)
		{
			return key == Key.LeftShift ||
			       key == Key.RightShift ||
			       key == Key.LeftCtrl ||
			       key == Key.RightCtrl ||
			       key == Key.LeftAlt ||
			       key == Key.RightAlt;
		}

		/// <summary>
		/// Called after the feature selection changed
		/// </summary>
		/// <param name="args"></param>
		private void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug("OnToolActivateAsync");

			RunQueuedTask(
				delegate
				{
					// Used to clear derived geometries etc.
					bool result = OnMapSelectionChangedCore(args);

					return result;
				});
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

		protected virtual bool OnSketchCompleteCore([NotNull] Geometry sketchGeometry,
		                                            [CanBeNull] CancelableProgressor progressor)
		{
			return true;
		}

		private bool OnSelectionSketchComplete(Geometry sketchGeometry,
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
	}
}
