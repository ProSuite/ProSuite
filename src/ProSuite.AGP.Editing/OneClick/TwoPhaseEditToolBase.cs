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

namespace ProSuite.AGP.Editing.OneClick
{
	public abstract class TwoPhaseEditToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected TwoPhaseEditToolBase()
		{
			IsSketchTool = true;
		}

		protected Cursor SecondPhaseCursor { get; set; }

		protected override bool OnMapSelectionChangedCore(MapSelectionChangedEventArgs args)
		{
			if (args.Selection.Count == 0)
			{
				ResetDerivedGeometries();
				StartSelectionPhase();
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
							var selectedFeatures =
								GetApplicableSelectedFeatures(ActiveMapView).ToList();

							CalculateDerivedGeometries(selectedFeatures, GetCancelableProgressor());

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

			return result;
		}

		protected override bool IsInSelectionPhase(bool shiftIsPressed)
		{
			if (shiftIsPressed)
			{
				return true;
			}

			var task = QueuedTask.Run(IsInSelectionPhaseQueued);

			// This can dead-lock! Remove everywhere, use async overload
			return task.Result;
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

		protected override bool HandleEscape()
		{
			QueuedTaskUtils.Run(
				() =>
				{
					SelectionUtils.ClearSelection();

					ResetDerivedGeometries();

					StartSelectionPhase();

					return true;
				});

			return true;
		}

		protected override void OnKeyUpCore(MapViewKeyEventArgs k)
		{
			if (IsShiftKey(k.Key))
			{
				Cursor = IsInSelectionPhase(true) ? SelectionCursor : SecondPhaseCursor;
			}
		}

		protected override void LogUsingCurrentSelection()
		{
			// using method LogDerivedGeometriesCalculated() for feedback
		}

		protected abstract void CalculateDerivedGeometries(
			[NotNull] IList<Feature> selectedFeatures,
			[CanBeNull] CancelableProgressor cancelableProgressor);

		protected abstract bool CanUseDerivedGeometries();

		protected abstract bool SelectAndProcessDerivedGeometry(
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
			Cursor = SecondPhaseCursor;
			
			SetupSketch(SketchGeometryType.Rectangle);
		}
	}
}
