using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Keyboard;
using Cursor = System.Windows.Input.Cursor;

namespace ProSuite.AGP.Editing.OneClick
{
	public abstract class ConstructionToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const Key _keyRestorePrevious = Key.R;

		private Geometry _editSketchBackup;
		private Geometry _previousSketch;

		private List<Operation> _sketchOperations;

		private bool _intermittentSelectionPhase;

		protected ConstructionToolBase()
		{
			IsSketchTool = true;
			SketchOutputMode = SketchOutputMode.Screen;

			GeomIsSimpleAsFeature = false;

			SketchCursor = ToolUtils.GetCursor(Resources.EditSketchCrosshair);
		}

		protected Cursor SketchCursor { get; set; }

		protected bool IsInSketchMode
		{
			get
			{
				// TODO: maintain actual property!
				return SketchType != SketchGeometryType.Rectangle;
			}
		}

		protected virtual bool SupportRestoreLastSketch => true;

		#region MapTool overrides

		protected override Task<bool> OnSketchModifiedAsync()
		{
			return QueuedTaskUtils.Run(OnSketchModifiedCore);
		}

		protected override Task OnSelectionChangedAsync(MapSelectionChangedEventArgs e)
		{
			// NOTE: This method is not called when the selection is cleared by another command (e.g. by 'Clear Selection')
			//       Is there another way to get the global selection changed event? What if we need the selection changed in a button?

			if (_intermittentSelectionPhase
			) // always false -> toolkeyup is first. This method is apparently scheduled to run after key up
			{
				return Task.FromResult(true);
			}

			if (CanUseSelection(e.Selection))
			{
				StartSketchPhase();
			}

			return Task.FromResult(true);
		}

		#endregion

		#region OneClickToolBase overrides

		protected override void OnToolActivatingCore()
		{
			_msg.VerboseDebug("OnToolActivatingCore");

			if (! RequiresSelection)
			{
				StartSketchPhase();
			}
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			RememberSketch();

			base.OnToolDeactivateCore(hasMapViewChanged);
		}

		protected override bool IsInSelectionPhase(bool shiftIsPressed)
		{
			return ! IsInSketchMode;
		}

		protected override void LogUsingCurrentSelection()
		{
			// log is written in LogEnteringSketchMode
		}

		protected override void AfterSelection(IList<Feature> selectedFeatures,
		                                       CancelableProgressor progressor)
		{
			if (CanStartSketchPhase(selectedFeatures))
			{
				StartSketchPhase();
			}
		}

		protected override void OnKeyDownCore(MapViewKeyEventArgs k)
		{
			_msg.VerboseDebug("OnKeyDownCore");

			if (IsShiftKey(k.Key))
			{
				if (_intermittentSelectionPhase)
				{
					// This is called repeatedly while keeping the shift key pressed
					return;
				}

				if (! IsInSketchMode)
				{
					return;
				}

				_intermittentSelectionPhase = true;

				// TODO: How can we not destroy the undo stack?

				OperationManager operationManager = ActiveMapView.Map.OperationManager;

				// It is technically possible to put back the operations by calling operationManager.AddUndoOperation(). 
				// But whether we can make them work when actually executed requires more tests.. and this is probably not the good way to do it!
				_sketchOperations =
					operationManager.FindUndoOperations(operation =>
						                                    operation.Category ==
						                                    "SketchOperations");

				// By backing up and re-setting the edit sketch the individual operations that made up the 
				// sketch are lost.
				_editSketchBackup = GetCurrentSketchAsync().Result;

				// TODO: Only clear the sketch and switch to selection phase if REALLY required
				// (i.e. because a rectangle sketch must be drawn on MouseMove)
				ClearSketchAsync();

				StartSelectionPhase();
			}
		}

		protected override void OnKeyUpCore(MapViewKeyEventArgs k)
		{
			_msg.VerboseDebug("OnKeyUpCore");

			if (IsShiftKey(k.Key))
			{
				_intermittentSelectionPhase = false;

				if (CanUseSelection(ActiveMapView))
				{
					StartSketchPhase();

					if (_editSketchBackup != null)
					{
						ActiveMapView.SetCurrentSketchAsync(_editSketchBackup);

						// This puts back the edit operations in the undo stack, but when clicking on the top one, the sketch 
						// is cleared and undoing any previous operation has no effect any more.
						ActiveMapView.Map.OperationManager.ClearUndoCategory("SketchOperations");

						if (_sketchOperations != null)
						{
							foreach (Operation operation in _sketchOperations)
							{
								ActiveMapView.Map.OperationManager.AddUndoOperation(operation);
							}
						}
					}
				}

				_editSketchBackup = null;
			}

			if (k.Key == _keyRestorePrevious)
			{
				RestorePreviousSketch();
			}
		}

		protected override bool HandleEscape()
		{
			QueuedTaskUtils.Run(
				delegate
				{
					if (IsInSketchMode)
					{
						// if sketch is empty, also remove selection and return to selection phase

						if (! RequiresSelection)
						{
							// remain in sketch mode, just reset the sketch
							ResetSketch();
						}
						else
						{
							Geometry sketch = GetCurrentSketchAsync().Result;

							if (sketch != null && ! sketch.IsEmpty)
							{
								ResetSketch();
							}
							else
							{
								SelectionUtils.ClearSelection(ActiveMapView.Map);

								StartSelectionPhase();
							}
						}
					}
					else
					{
						ClearSketchAsync();
						SelectionUtils.ClearSelection(ActiveMapView.Map);
					}

					return true;
				});
			return true;
		}

		protected override bool OnMapSelectionChangedCore(MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug("OnMapSelectionChangedCore");

			if (ActiveMapView == null)
			{
				return false;
			}

			// TODO: only if selection was cleared? Generally allow changing the selection through attribute selection?

			if (! CanUseSelection(ActiveMapView))
			{
				//LogPromptForSelection();
				StartSelectionPhase();
			}

			return true;
		}

		protected override async Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			_msg.VerboseDebug("OnSketchCompleteCoreAsync");

			if (IsInSketchMode)
			{
				// take snapshots
				EditingTemplate currentTemplate = CurrentTemplate;
				MapView activeView = ActiveMapView;

				RememberSketch(sketchGeometry);

				return await OnEditSketchCompleteCoreAsync(
					       sketchGeometry, currentTemplate, activeView, progressor);
			}

			return false;
		}

		#endregion

		protected abstract SketchGeometryType GetSketchGeometryType();

		protected virtual bool OnSketchModifiedCore()
		{
			return true;
		}

		protected virtual bool CanStartSketchPhaseCore(IList<Feature> selectedFeatures)
		{
			return true;
		}

		protected abstract void LogEnteringSketchMode();

		/// <summary>
		/// Determines whether the provided selection can be used by this tool.
		/// </summary>
		/// <param name="selection"></param>
		/// <returns></returns>
		protected virtual bool CanUseSelection(Dictionary<MapMember, List<long>> selection)
		{
			// TODO
			return selection.Count > 0;
		}

		protected abstract Task<bool> OnEditSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			EditingTemplate editTemplate,
			MapView activeView,
			CancelableProgressor cancelableProgressor = null);

		protected virtual CancelableProgressor GetSelectionProgressor()
		{
			return null;
		}

		protected virtual CancelableProgressor GetSketchCompleteProgressor()
		{
			return null;
		}

		protected virtual void OnSketchResetCore() { }

		private void StartSketchPhase()
		{
			SketchOutputMode = SketchOutputMode.Map;

			SketchType = GetSketchGeometryType();

			UseSnapping = true;
			CompleteSketchOnMouseUp = false;

			SetCursor(SketchCursor);

			StartSketchAsync();

			LogEnteringSketchMode();
		}

		/// <summary>
		/// Determines whether the provided selection can be used by this tool.
		/// </summary>
		/// <param name="selection"></param>
		/// <returns></returns>
		private bool CanUseSelection(Dictionary<BasicFeatureLayer, List<long>> selection)
		{
			var mapMemberDictionary = new Dictionary<MapMember, List<long>>(selection.Count);

			foreach (var keyValuePair in selection)
			{
				mapMemberDictionary.Add(keyValuePair.Key, keyValuePair.Value);
			}

			return CanUseSelection(mapMemberDictionary);
		}

		private bool CanStartSketchPhase(IList<Feature> selectedFeatures)
		{
			if (KeyboardUtils.IsModifierPressed(Keys.Shift, true))
			{
				return false;
			}

			return CanStartSketchPhaseCore(selectedFeatures);
		}

		private async Task ResetSketchAsync()
		{
			Geometry currentSketch = await GetCurrentSketchAsync();

			if (currentSketch != null && ! currentSketch.IsEmpty)
			{
				await ClearSketchAsync();
				OnSketchModifiedCore();
			}

			OnSketchResetCore();

			await StartSketchAsync();
		}

		private void ResetSketch()
		{
			RememberSketch();

			ClearSketchAsync();
			OnSketchModifiedCore();

			OnSketchResetCore();

			StartSketchAsync();
		}

		private void RememberSketch(Geometry knownSketch = null)
		{
			if (! SupportRestoreLastSketch)
			{
				return;
			}

			var sketch = knownSketch ?? GetCurrentSketchAsync().Result;

			if (sketch != null && ! sketch.IsEmpty)
			{
				_previousSketch = sketch;
			}
		}

		private void RestorePreviousSketch()
		{
			if (! SupportRestoreLastSketch)
			{
				return;
			}

			if (_previousSketch == null || _previousSketch.IsEmpty)
			{
				_msg.Warn("There is no previous sketch to restore.");

				return;
			}

			if (! IsInSketchMode)
			{
				// If a non-rectangular sketch is set while SketchType is rectangle (or probably generally the wrong type)
				// sketching is not possible any more and the application appears hanging

				// Try start sketch mode:
				IList<Feature> selection = GetApplicableSelectedFeatures(ActiveMapView).ToList();

				if (CanUseSelection(ActiveMapView))
				{
					AfterSelection(selection, null);
				}
			}

			if (IsInSketchMode)
			{
				SetCurrentSketchAsync(_previousSketch);
			}
			else
			{
				_msg.Warn("Sketch cannot be restored in selection phase. " +
				          "Please try again in the sketch phase.");
			}
		}
	}
}
