using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;

namespace ProSuite.AGP.Editing.OneClick
{
	public abstract class TwoPhaseEditToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private SelectionCursors _secondPhaseCursors;

		protected TwoPhaseEditToolBase()
		{
			IsSketchTool = true;
		}

		protected override void OnToolActivatingCore()
		{
			base.OnToolActivatingCore();
			_secondPhaseCursors = GetSecondPhaseCursors();
		}

		protected override async Task<bool> OnMapSelectionChangedCoreAsync(
			MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug(() => "OnMapSelectionChangedCoreAsync");

			var selection = args.Selection;

			if (selection.Count == 0)
			{
				ResetDerivedGeometries();
				await StartSelectionPhaseAsync();
			}

			// E.g. a part of the selection has been removed (e.g. using 'clear selection' on a layer)
			Dictionary<MapMember, List<long>> selectionByLayer = selection.ToDictionary();

			var applicableSelection =
				GetDistinctApplicableSelectedFeatures(selectionByLayer, UnJoinedSelection).ToList();

			if (applicableSelection.Count > 0)
			{
				using var source = GetProgressorSource();
				var progressor = source?.Progressor;
				await AfterSelectionAsync(applicableSelection, progressor);
			}

			return true;
		}

		protected override async Task OnEditCompletedAsyncCore(EditCompletedEventArgs args)
		{
			bool requiresRecalculate = args.CompletedType == EditCompletedType.Discard ||
			                           args.CompletedType == EditCompletedType.Reconcile ||
			                           args.CompletedType == EditCompletedType.Redo ||
			                           args.CompletedType == EditCompletedType.Undo;

			if (requiresRecalculate)
			{
				await QueuedTask.Run(
					async () =>
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
								await StartSelectionPhaseAsync();
								return;
							}

							using var source = GetProgressorSource();
							var progressor = source?.Progressor;

							CalculateDerivedGeometries(selectedFeatures, progressor);
							await DerivedGeometriesCalculated(null, true);
						}
						catch (Exception e)
						{
							// Do not re-throw or the application could crash (e.g. in undo)
							_msg.Error($"Error calculating reshape curves: {e.Message}", e);
						}
					});
			}

			await base.OnEditCompletedAsyncCore(args);
		}

		protected override async Task AfterSelectionAsync(IList<Feature> selectedFeatures,
		                                                  CancelableProgressor progressor)
		{
			CalculateDerivedGeometries(selectedFeatures, progressor);

			await DerivedGeometriesCalculated(progressor);
		}

		protected override async Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			bool result = await QueuedTask.Run(() =>
			{
				var selection = SelectionUtils.GetSelection(ActiveMapView.Map);

				Geometry simpleGeometry = GeometryUtils.Simplify(sketchGeometry);
				Assert.NotNull(simpleGeometry, "Geometry is null");

				return SelectAndProcessDerivedGeometry(selection, simpleGeometry, progressor);
			});

			await StartSecondPhaseAsync();

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

					StartSelectionPhaseAsync();
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
				SetToolCursor(_secondPhaseCursors.GetCursor(GetSketchType(), shiftDown: false));
			}
		}

		protected override async Task ToggleSelectionSketchGeometryType(
			SketchGeometryType toggleSketchType,
			SelectionCursors selectionCursors = null)
		{
			if (await IsInSelectionPhaseAsync())
			{
				await base.ToggleSelectionSketchGeometryType(toggleSketchType, selectionCursors);
			}
			else
			{
				await base.ToggleSelectionSketchGeometryType(toggleSketchType, _secondPhaseCursors);
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

		protected abstract SelectionCursors GetSecondPhaseCursors();

		protected abstract void CalculateDerivedGeometries(
			[NotNull] IList<Feature> selectedFeatures,
			[CanBeNull] CancelableProgressor cancelableProgressor);

		protected abstract bool CanUseDerivedGeometries();

		protected abstract Task<bool> SelectAndProcessDerivedGeometry(
			[NotNull] Dictionary<MapMember, List<long>> selection, [NotNull] Geometry sketch,
			[CanBeNull] CancelableProgressor progressor);

		protected abstract void ResetDerivedGeometries();

		protected abstract void LogDerivedGeometriesCalculated(CancelableProgressor progressor);

		private async Task DerivedGeometriesCalculated([CanBeNull] CancelableProgressor progressor)
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
				await StartSecondPhaseAsync();
			}
			else
			{
				// In case it has not yet been started (e.g. on tool activation with selection)
				await StartSelectionPhaseAsync();
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

		private async Task StartSecondPhaseAsync()
		{
			SetupSketch();

			await ResetSelectionSketchType(_secondPhaseCursors);
		}
	}
}
