using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
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

		private Geometry _previousSketch;

		[CanBeNull] private IntermittentSelectionPhase _intermittentSelectionPhase;
		[CanBeNull] private SketchRecorder _sketchRecorder;

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

			// Does it make any difference what the return value is?
			return await OnSketchModifiedAsyncCore();
		}

		#endregion

		#region OneClickToolBase overrides

		protected override void OnSelectionPhaseStarted()
		{
			if (QueuedTask.OnWorker)
			{
				SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
				SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);
			}
			else
			{
				QueuedTask.Run(() =>
				{
					SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
					SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);
				});
			}

			IsInSketchPhase = false;
		}

		protected override async Task OnToolActivatingCoreAsync()
		{
			_msg.VerboseDebug(() => "OnToolActivatingCoreAsync");

			if (! RequiresSelection)
			{
				StartSketchPhase();
			}
			else
			{
				_intermittentSelectionPhase = new IntermittentSelectionPhase();
				_intermittentSelectionPhase.Reset();

				_sketchRecorder = new SketchRecorder();
				await _sketchRecorder.RecordAsync();
			}
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_sketchRecorder?.Reset();
			RememberSketch();
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

			if (IsInSketchPhase)
			{
				return false;
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

		protected override async void AfterSelection(IList<Feature> selectedFeatures,
		                                             CancelableProgressor progressor)
		{
			// Release latch. The tool might not get the shift released when the sift key was
			// released while picker window was visible.
			if (_intermittentSelectionPhase is { Running: true } && ! KeyboardUtils.IsShiftDown())
			{
				_intermittentSelectionPhase.Stop();
			}

			if (await CanStartSketchPhaseAsync(selectedFeatures))
			{
				StartSketchPhase();
			}
		}

		protected override async Task ShiftPressedCoreAsync()
		{
			if (! RequiresSelection)
			{
				return;
			}

			// This is called repeatedly while keeping the shift key pressed.
			// Return if intermittent selection phase is running.
			Assert.NotNull(_intermittentSelectionPhase);
			if (_intermittentSelectionPhase.Running)
			{
				return;
			}

			try
			{
				// must not be null because of entrance guard RequiresSelection
				Assert.NotNull(_sketchRecorder);
				await _sketchRecorder.SuspendAsync();

				await ClearSketchAsync();

				StartSelectionPhase();
			}
			finally
			{
				_intermittentSelectionPhase.Start();
			}
		}

		protected override async Task SetupLassoSketchAsync()
		{
			if (await IsInSelectionPhaseCoreAsync(KeyboardUtils.IsShiftDown()))
			{
				await base.SetupLassoSketchAsync();
			}
			// Else do nothing: no lasso in construction sketch phase.
		}

		protected override async Task SetupPolygonSketchAsync()
		{
			if (await IsInSelectionPhaseCoreAsync(KeyboardUtils.IsShiftDown()))
			{
				await base.SetupPolygonSketchAsync();
			}
			// Else do nothing: no polygon sketch cursor in construction sketch phase.
		}

		protected override async Task ShiftReleasedCoreAsync()
		{
			if (! RequiresSelection)
			{
				return;
			}

			Assert.NotNull(_intermittentSelectionPhase);
			_intermittentSelectionPhase.Stop();

			// todo: daro Use CanStartSketchPhase?
			if (await ViewUtils.TryAsync(QueuedTask.Run(() => CanUseSelection(ActiveMapView)), _msg,
			                             suppressErrorMessageBox: true))
			{
				StartSketchPhase();

				Assert.NotNull(_sketchRecorder);
				await _sketchRecorder.SetSketchesAsync();
				_sketchRecorder.Resume();
			}
		}

		protected override async Task HandleKeyUpCoreAsync(MapViewKeyEventArgs args)
		{
			_msg.VerboseDebug(() => $"HandleKeyUpCoreAsync ({Caption})");

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

		protected override async Task HandleEscapeAsync()
		{
			await ViewUtils.TryAsync(
				QueuedTask.Run(
					async () =>
					{
						if (IsInSketchMode)
						{
							if (! RequiresSelection)
							{
								// remain in sketch mode, just reset the sketch
								await ResetSketchAsync();
							}
							else
							{
								Geometry sketch = await GetCurrentSketchAsync();

								// if sketch is empty, also remove selection and return to selection phase
								if (sketch is { IsEmpty: false })
								{
									await ResetSketchAsync();
								}
								else
								{
									ClearSelection();
								}
							}
						}
						else
						{
							await ClearSketchAsync();
							ClearSelection();
						}
					}), _msg);
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
			else
			{
				StartSketchPhase();
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

		protected virtual Task<bool> OnSketchModifiedAsyncCore()
		{
			return Task.FromResult(true);
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

			// todo: daro to Utils?
			if (QueuedTask.OnWorker)
			{
				ResetSketchVertexSymbolOptions();
			}
			else
			{
				QueuedTask.Run(ResetSketchVertexSymbolOptions);
			}

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

		private async Task<bool> CanStartSketchPhaseAsync(IList<Feature> selectedFeatures)
		{
			if (KeyboardUtils.IsModifierDown(Key.LeftShift, exclusive: true) ||
			    KeyboardUtils.IsModifierDown(Key.RightShift, exclusive: true))
			{
				return false;
			}

			if (! await ViewUtils.TryAsync(QueuedTask.Run(() => CanUseSelection(ActiveMapView)), _msg,
			                               suppressErrorMessageBox: true))
			{
				return false;
			}

			return CanStartSketchPhaseCore(selectedFeatures);
		}

		private async Task ResetSketchAsync()
		{
			Geometry currentSketch = await GetCurrentSketchAsync();

			if (currentSketch is { IsEmpty: false })
			{
				RememberSketch();

				await ClearSketchAsync();

				await OnSketchModifiedAsyncCore();
			}

			OnSketchResetCore();

			await StartSketchAsync();
		}

		protected void RememberSketch(Geometry knownSketch = null)
		{
			if (! SupportRestoreLastSketch)
			{
				return;
			}

			var sketch = knownSketch ?? GetCurrentSketchAsync().Result;

			if (sketch is { IsEmpty: false })
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

		private async Task LogLastSketchVertexZ()
		{
			Geometry sketch = await GetCurrentSketchAsync();

			if (! sketch.HasZ)
			{
				return;
			}

			await QueuedTaskUtils.Run(() =>
			{
				MapPoint lastPoint = GetLastPoint(sketch);

				if (lastPoint != null)
				{
					_msg.InfoFormat("Vertex added, Z={0:N2}", lastPoint.Z);
				}
			});
		}

		private static MapPoint GetLastPoint(Geometry sketch)
		{
			MapPoint lastPoint = null;

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

		private class IntermittentSelectionPhase
		{
			public bool Running { get; private set; }

			public void Start()
			{
				Assert.False(Running, "intermittent selection phase is already running.");
				Running = true;
			}

			public void Stop()
			{
				Assert.True(Running, "intermittent selection phase has not been started.");
				Running = false;
			}

			public void Reset()
			{
				Running = false;
			}
		}
	}
}
