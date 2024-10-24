using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.OneClick
{
	public abstract class ConstructionToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const Key _keyFinishSketch = Key.F2;
		private const Key _keyRestorePrevious = Key.R;

		private Geometry _editSketchBackup;
		private Geometry _previousSketch;

		private List<Operation> _sketchOperations;

		private bool _intermittentSelectionPhase;

		protected ConstructionToolBase()
		{
			ContextMenuID = "esri_editing_SketchContextMenu";

			IsSketchTool = true;

			UseSelection = true;
			GeomIsSimpleAsFeature = false;

			SketchCursor = ToolUtils.GetCursor(Resources.EditSketchCrosshair);

			HandledKeys.Add(_keyFinishSketch);
			HandledKeys.Add(_keyRestorePrevious);
		}

		protected Cursor SketchCursor { get; set; }

		/// <summary>
		/// Whether the geometry sketch (as opposed to the selection sketch) is currently active
		/// and visible and can be manipulated by the user. This property is false during an
		/// intermediate selection (using shift key). <see cref="IsInSketchPhase"/> however
		/// will remain true in an intermediate selection.
		/// </summary>
		protected bool IsInSketchMode
		{
			get
			{
				if (! IsInSketchPhase)
				{
					return false;
				}

				bool selectingDuringSketchPhase =
					RequiresSelection &&
					KeyboardUtils.IsModifierDown(Key.LeftShift, exclusive: true) ||
					KeyboardUtils.IsModifierDown(Key.RightShift, exclusive: true);

				return ! selectingDuringSketchPhase;
			}
		}

		/// <summary>
		/// Property which indicates whether the tool is in the sketch phase. The difference to
		/// <see cref="IsInSketchMode"/> is that this property reflects the general phase of the
		/// tool. Even during the sketch phase an intermittent selection can be performed.
		/// In order to evaluate weather the actual sketch is currently visible and edited,
		/// use <see cref="IsInSketchMode"/>.
		/// </summary>
		protected bool IsInSketchPhase { get; set; }

		protected bool SupportRestoreLastSketch => true;

		protected bool LogSketchVertexZs { get; set; }

		#region MapTool overrides

		protected override async Task<bool> OnSketchModifiedAsync()
		{
			_msg.VerboseDebug(() => "OnSketchModifiedAsync()");

			if (LogSketchVertexZs && IsInSketchMode)
			{
				await LogLastSketchVertexZ();
			}

			return await QueuedTaskUtils.Run(OnSketchModifiedCore);
		}

		protected override Task OnSelectionChangedAsync(MapSelectionChangedEventArgs e)
		{
			// NOTE: This method is not called when the selection is cleared by another command (e.g. by 'Clear Selection')
			//       Is there another way to get the global selection changed event? What if we need the selection changed in a button?
			// NOTE daro: Pro 3.3 this method is called e.g. on 'Clear Selection' or select row in attribute table. So the above
			//			  note is not correct anymore.

			// This method is presumably called in the following situation only:
			// MapTool.UseSelection is true and your MapTool does sketching (i.e. i used SketchType = SketchGeometryType.Line)
			// After start sketching and shift is pressed to change the selection and then the selection is changed:
			// https://community.esri.com/t5/arcgis-pro-sdk-questions/maptool-onselectionchangedasync-not-triggered/td-p/1199664

			if (_intermittentSelectionPhase) // always false -> toolkeyup is first. This method is apparently scheduled to run after key up
			{
				return Task.FromResult(true);
			}

			if (CanUseSelection(SelectionUtils.GetSelection<BasicFeatureLayer>(e.Selection)))
			{
				StartSketchPhase();
			}

			return Task.FromResult(true);
		}

		#endregion

		#region OneClickToolBase overrides

		protected override void OnSelectionPhaseStarted()
		{
			IsInSketchPhase = false;
		}

		protected override void OnToolActivatingCore()
		{
			_msg.VerboseDebug(() => "OnToolActivatingCore");

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

		protected override async Task<bool> IsInSelectionPhaseCoreAsync(bool shiftDown)
		{
			if (! RequiresSelection)
			{
				return false;
			}

			if (shiftDown)
			{
				return true;
			}

			bool result = await QueuedTask.Run(IsInSelectionPhaseQueued);
			return result;
		}

		private bool IsInSelectionPhaseQueued()
		{
			return ! IsInSketchPhase;
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
			_msg.VerboseDebug(() => "OnKeyDownCore");

			if (KeyboardUtils.IsShiftKey(k.Key))
			{
				if (_intermittentSelectionPhase)
				{
					// This is called repeatedly while keeping the shift key pressed
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
				// todo daro await
				_editSketchBackup = GetCurrentSketchAsync().Result;

				// TODO: Only clear the sketch and switch to selection phase if REALLY required
				// (i.e. because a rectangle sketch must be drawn on MouseMove)
				ClearSketchAsync();

				StartSelectionPhase();
			}
		}

		protected override async Task HandleKeyUpCoreAsync(MapViewKeyEventArgs args)
		{
			// todo daro more ViewUtils
			_msg.VerboseDebug(() => $"HandleKeyUpCoreAsync ({Caption})");

			if (KeyboardUtils.IsShiftKey(args.Key))
			{
				_intermittentSelectionPhase = false;

				Task<bool> task = QueuedTask.Run(() => CanUseSelection(ActiveMapView));

				bool canUseSelection =
					await ViewUtils.TryAsync(task, _msg, suppressErrorMessageBox: true);

				if (canUseSelection)
				{
					StartSketchPhase();

					if (_editSketchBackup != null && CanSetSketch(_editSketchBackup))
					{
						await ActiveMapView.SetCurrentSketchAsync(_editSketchBackup);

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

			if (args.Key == _keyFinishSketch)
			{
				// #114: F2 has no effect unless another tool has been used before:
				Geometry currentSketch = await GetCurrentSketchAsync();

				if (CanFinishSketch(currentSketch))
				{
					await OnSketchCompleteAsync(currentSketch);
					await ClearSketchAsync();
				}
			}

			if (args.Key == _keyRestorePrevious)
			{
				await RestorePreviousSketchAsync();
			}
		}

		private bool CanSetSketch(Geometry geometry)
		{
			GeometryType geometryType = geometry.GeometryType;

			switch (GetSketchGeometryType())
			{
				case SketchGeometryType.Point:
					return geometryType == GeometryType.Point;
				case SketchGeometryType.Line:
					return geometryType == GeometryType.Polyline;
				case SketchGeometryType.Polygon:
					return geometryType == GeometryType.Polygon;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		protected override async Task HandleEscapeAsync()
		{
			Task task = QueuedTask.Run(
				() =>
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
							// todo daro await
							Geometry sketch = GetCurrentSketchAsync().Result;

							if (sketch != null && ! sketch.IsEmpty)
							{
								ResetSketch();
							}
							else
							{
								ClearSelection();
							}
						}
					}
					else
					{
						ClearSketchAsync();
						ClearSelection();
					}
				});

			await ViewUtils.TryAsync(task, _msg);
		}

		protected override bool OnMapSelectionChangedCore(MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug(() => "OnMapSelectionChangedCore");

			if (ActiveMapView == null)
			{
				return false;
			}

			if (RequiresSelection && ! CanUseSelection(ActiveMapView))
			{
				//LogPromptForSelection();
				StartSelectionPhase();
			}

			// TODO: virtual RefreshFeedbackCoreAsync(), override in AdvancedReshape

			return true;
		}

		protected override async Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			_msg.VerboseDebug(() => "OnSketchCompleteCoreAsync");

			if (IsInSketchMode)
			{
				// take snapshots
				//Todo: In Pro3 this.CurrentTemplate is null. Investigate further...
				EditingTemplate currentTemplate = EditingTemplate.Current;
				MapView activeView = ActiveMapView;

				RememberSketch(sketchGeometry);

				return await OnEditSketchCompleteCoreAsync(
					       sketchGeometry, currentTemplate, activeView, progressor);
			}

			return false;
		}

		#endregion

		protected abstract SketchGeometryType GetSketchGeometryType();

		/// <summary>
		/// The template that can optionally be used to set up the sketch properties, such as
		/// z/m-awareness. If the tool uses a template create a feature this method should return
		/// the relevant template.
		/// </summary>
		/// <returns></returns>
		protected virtual EditingTemplate GetSketchTemplate()
		{
			return null;
		}

		protected virtual bool OnSketchModifiedCore()
		{
			return true;
		}

		protected virtual bool CanStartSketchPhaseCore(IList<Feature> selectedFeatures)
		{
			return true;
		}

		protected abstract void LogEnteringSketchMode();

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

		protected void StartSketchPhase()
		{
			UseSnapping = true;
			CompleteSketchOnMouseUp = false;
			SetSketchType(GetSketchGeometryType());

			SetCursor(SketchCursor);

			EditingTemplate relevanteTemplate = GetSketchTemplate();

			if (relevanteTemplate != null)
			{
				StartSketchAsync(relevanteTemplate);
			}
			else
			{
				// TODO: Manually set up Z/M-awareness
				StartSketchAsync();
			}

			LogEnteringSketchMode();

			IsInSketchPhase = true;

			OnSketchPhaseStarted();
		}

		protected virtual void OnSketchPhaseStarted() { }

		private static bool CanFinishSketch(Geometry sketch)
		{
			if (sketch == null || sketch.IsEmpty)
			{
				return false;
			}

			if (sketch.GeometryType == GeometryType.Polygon)
			{
				return sketch.PointCount > 2;
			}

			if (sketch.GeometryType == GeometryType.Polyline)
			{
				return sketch.PointCount > 1;
			}

			return true;
		}

		//// todo daro drop
		///// <summary>
		///// Determines whether the provided selection can be used by this tool.
		///// </summary>
		///// <param name="selection"></param>
		///// <returns></returns>
		//private bool CanUseSelection(Dictionary<BasicFeatureLayer, List<long>> selection)
		//{
		//	var mapMemberDictionary = new Dictionary<MapMember, List<long>>(selection.Count);

		//	foreach (var keyValuePair in selection)
		//	{
		//		mapMemberDictionary.Add(keyValuePair.Key, keyValuePair.Value);
		//	}

		//	return CanUseSelection(mapMemberDictionary);
		//}

		private bool CanStartSketchPhase(IList<Feature> selectedFeatures)
		{
			if (KeyboardUtils.IsModifierDown(Key.LeftShift, exclusive: true) ||
			    KeyboardUtils.IsModifierDown(Key.RightShift, exclusive: true))
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

		protected void RememberSketch(Geometry knownSketch = null)
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

		private async Task RestorePreviousSketchAsync()
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

			try
			{
				if (! IsInSketchMode)
				{
					// If a non-rectangular sketch is set while SketchType is rectangle (or probably generally the wrong type)
					// sketching is not possible any more and the application appears hanging

					// Try start sketch mode:
					await QueuedTask.Run(() =>
					{
						var mapView = ActiveMapView; // TODO should be passed in from outside QTR

						IList<Feature> selection =
							GetApplicableSelectedFeatures(mapView).ToList();

						if (CanUseSelection(mapView))
						{
							AfterSelection(selection, null);
						}
					});
				}

				if (IsInSketchMode)
				{
					await SetCurrentSketchAsync(_previousSketch);
				}
				else
				{
					_msg.Warn("Sketch cannot be restored in selection phase. " +
					          "Please try again in the sketch phase.");
				}
			}
			catch (Exception e)
			{
				throw new ApplicationException("Error restoring the previous sketch", e);
			}
		}

		private async Task<bool> LogLastSketchVertexZ()
		{
			Geometry sketch = await GetCurrentSketchAsync();

			if (! sketch.HasZ)
			{
				return false;
			}

			bool result = false;
			await QueuedTaskUtils.Run(() =>
			{
				MapPoint lastPoint = GetLastPoint(sketch);

				if (lastPoint != null)
				{
					_msg.InfoFormat("Vertex added, Z={0:N2}", lastPoint.Z);
					result = true;
				}
			});

			return result;
		}

		private static MapPoint GetLastPoint(Geometry sketch)
		{
			MapPoint lastPoint = null;
			;
			if (sketch is Multipart multipart)
			{
				ReadOnlyPointCollection points = multipart.Points;

				if (points.Count > 0)
				{
					lastPoint = points[points.Count - 1];
				}
			}
			else if (sketch is MapPoint point)
			{
				lastPoint = point;
			}
			else if (sketch is Multipoint multipoint)
			{
				ReadOnlyPointCollection points = multipoint.Points;

				if (points.Count > 0)
				{
					lastPoint = points[points.Count - 1];
				}
			}

			return lastPoint;
		}
	}
}
