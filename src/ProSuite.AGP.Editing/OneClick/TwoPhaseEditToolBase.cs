using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
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
	public abstract class TwoPhaseEditToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

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
				var selection = ActiveMapView.Map.GetSelection();

				return SelectAndProcessDerivedGeometry(selection, sketchGeometry, progressor);
			});

			return result;
		}

		protected override bool IsInSelectionPhase()
		{
			if (KeyboardUtils.IsModifierPressed(Keys.Shift, true))
			{
				return true;
			}

			var task = QueuedTask.Run(
				() =>
				{
					bool result;

					IList<Feature> selection =
						SelectionUtils.GetSelectedFeatures(ActiveMapView).ToList();

					if (! CanUseSelection(selection))
					{
						result = true;
					}
					else
					{
						result = ! CanUseDerivedGeometries();
					}

					return result;
				});

			return task.Result;
		}

		protected override bool HandleEscape()
		{
			SelectionUtils.ClearSelection(ActiveMapView.Map);

			ResetDerivedGeometries();

			StartSelectionPhase();

			return true;
		}

		protected override void OnKeyUpCore(MapViewKeyEventArgs k)
		{
			if (IsShiftKey(k.Key))
			{
				Cursor = IsInSelectionPhase() ? SelectionCursor : SecondPhaseCursor;
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
			if (progressor == null || ! progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.DebugFormat("{0}: Derived geometries calculated.", Caption);
			}
			else
			{
				_msg.DebugFormat("{0}: Derived geometry calculation was cancelled.", Caption);
			}

			if (CanUseDerivedGeometries())
			{
				StartSecondPhase();
			}

			LogDerivedGeometriesCalculated(progressor);
		}

		private void StartSecondPhase()
		{
			Cursor = SecondPhaseCursor;

			SketchOutputMode = SketchOutputMode.Map;

			// NOTE: CompleteSketchOnMouseUp must be set before the sketch geometry type,
			// otherwise it has no effect!
			CompleteSketchOnMouseUp = true;

			SketchType = SketchGeometryType.Rectangle;

			UseSnapping = false;

			GeomIsSimpleAsFeature = false;
		}
	}
}
