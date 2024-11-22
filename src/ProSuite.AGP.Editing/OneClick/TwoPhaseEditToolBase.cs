using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.OneClick
{
	public abstract class TwoPhaseEditToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private SketchAndCursorSetter _secondPhaseSketchCursor;

		protected TwoPhaseEditToolBase()
		{
			IsSketchTool = true;
		}

		protected override async Task OnToolActivatingCoreAsync()
		{
			await QueuedTaskUtils.Run(() =>
			{
				_secondPhaseSketchCursor =
					SketchAndCursorSetter.Create(this,
					                             GetSecondPhaseCursor(),
					                             GetSecondPhaseCursorLasso(),
					                             GetSecondPhaseCursorPolygon(),
					                             GetSelectionSketchGeometryType(),
					                             DefaultSketchTypeOnFinishSketch);

				// NOTE daro: no shift cursors for second phase.
			});
		}

		protected abstract Cursor GetSecondPhaseCursor();

		protected abstract Cursor GetSecondPhaseCursorLasso();

		protected abstract Cursor GetSecondPhaseCursorPolygon();

		protected override bool OnMapSelectionChangedCore(MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug(() => "OnMapSelectionChangedCore");

			var selection = args.Selection;

			if (selection.Count == 0)
			{
				ResetDerivedGeometries();
				StartSelectionPhase();
			}

			// E.g. a part of the selection has been removed (e.g. using 'clear selection' on a layer)
			Dictionary<MapMember, List<long>> selectionByLayer = selection.ToDictionary();

			var applicableSelection =
				GetDistinctApplicableSelectedFeatures(selectionByLayer, UnJoinedSelection).ToList();

			if (applicableSelection.Count > 0)
			{
				using var source = GetProgressorSource();
				var progressor = source?.Progressor;
				AfterSelection(applicableSelection, progressor);
			}

			return true;
		}

		protected override Task OnEditCompletedAsyncCore(EditCompletedEventArgs args)
		{
			bool requiresRecalculate = args.CompletedType == EditCompletedType.Discard ||
			                           args.CompletedType == EditCompletedType.Reconcile ||
			                           args.CompletedType == EditCompletedType.Redo ||
			                           args.CompletedType == EditCompletedType.Undo;

			if (requiresRecalculate)
			{
				return QueuedTask.Run(
					() =>
					{
						try
						{
							Dictionary<MapMember, List<long>> selectionByLayer =
								SelectionUtils.GetSelection(ActiveMapView.Map);

							var selectedFeatures =
								GetDistinctApplicableSelectedFeatures(selectionByLayer).ToList();

							if (selectedFeatures.Count == 0)
							{
								ResetDerivedGeometries();
								StartSelectionPhase();
								return true;
							}

							using var source = GetProgressorSource();
							var progressor = source?.Progressor;

							CalculateDerivedGeometries(selectedFeatures, progressor);

							return true;
						}
						catch (Exception e)
						{
							// Do not re-throw or the application could crash (e.g. in undo)
							_msg.Error($"Error calculating reshape curves: {e.Message}", e);
							return false;
						}
					});
			}

			return base.OnEditCompletedAsyncCore(args);
		}

		protected override void AfterSelection(IList<Feature> selectedFeatures,
		                                       CancelableProgressor progressor)
		{
			CalculateDerivedGeometries(selectedFeatures, progressor);

			DerivedGeometriesCalculated(progressor);
		}

		protected override async Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			var result = await QueuedTask.Run(() =>
			{
				var selection = SelectionUtils.GetSelection(ActiveMapView.Map);

				return SelectAndProcessDerivedGeometry(selection, sketchGeometry, progressor);
			});

			StartSecondPhase();

			return result;
		}

		protected override async Task<bool> IsInSelectionPhaseCoreAsync(bool shiftDown)
		{
			if (shiftDown)
			{
				return true;
			}

			bool result = await QueuedTask.Run(IsInSelectionPhaseQueued);

			return result;
		}

		protected override async Task HandleEscapeAsync()
		{
			// Do not reset feedback in polygon sketch mode: Esc
			// should only clear sketch not the feedback.
			if (await NonEmptyPolygonSketchAsync() &&
			    ! await IsInSelectionPhaseAsync())
			{
				await ClearSketchAsync();
				return;
			}

			Task task = QueuedTask.Run(
				() =>
				{
					ClearSelection();

					ResetDerivedGeometries();

					StartSelectionPhase();
				});

			await ViewUtils.TryAsync(task, _msg);
		}

		protected override async Task ShiftReleasedCoreAsync()
		{
			if (await IsInSelectionPhaseAsync())
			{
				await base.ShiftReleasedCoreAsync();
			}
			else
			{
				_secondPhaseSketchCursor.SetCursor(GetSketchType(), shiftDown: false);
			}
		}

		protected override async Task SetupLassoSketchAsync()
		{
			if (await IsInSelectionPhaseAsync())
			{
				await base.SetupLassoSketchAsync();
			}
			else
			{
				_secondPhaseSketchCursor.Toggle(SketchGeometryType.Lasso);
			}
		}

		protected override async Task SetupPolygonSketchAsync()
		{
			if (await IsInSelectionPhaseAsync())
			{
				await base.SetupPolygonSketchAsync();
			}
			else
			{
				_secondPhaseSketchCursor.Toggle(SketchGeometryType.Polygon);
			}
		}

		protected override void LogUsingCurrentSelection()
		{
			// using method LogDerivedGeometriesCalculated() for feedback
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
		}

		protected abstract void CalculateDerivedGeometries(
			[NotNull] IList<Feature> selectedFeatures,
			[CanBeNull] CancelableProgressor cancelableProgressor);

		protected abstract bool CanUseDerivedGeometries();

		protected abstract Task<bool> SelectAndProcessDerivedGeometry(
			[NotNull] Dictionary<MapMember, List<long>> selection, [NotNull] Geometry sketch,
			[CanBeNull] CancelableProgressor progressor);

		protected abstract void ResetDerivedGeometries();

		protected abstract void LogDerivedGeometriesCalculated(CancelableProgressor progressor);

		private void DerivedGeometriesCalculated([CanBeNull] CancelableProgressor progressor)
		{
			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.DebugFormat("{0}: Derived geometry calculation was cancelled.", Caption);
			}
			else
			{
				_msg.DebugFormat("{0}: Derived geometries calculated.", Caption);
			}

			if (CanUseDerivedGeometries())
			{
				StartSecondPhase();
			}
			else
			{
				// In case it has not yet been started (e.g. on tool activation with selection)
				StartSelectionPhase();
			}

			LogDerivedGeometriesCalculated(progressor);
		}

		private bool IsInSelectionPhaseQueued()
		{
			bool result;

			if (! CanUseSelection(ActiveMapView))
			{
				result = true;
			}
			else
			{
				result = ! CanUseDerivedGeometries();
			}

			return result;
		}

		private void StartSecondPhase()
		{
			SetupSketch();

			_secondPhaseSketchCursor.ResetOrDefault();
		}
	}
}
