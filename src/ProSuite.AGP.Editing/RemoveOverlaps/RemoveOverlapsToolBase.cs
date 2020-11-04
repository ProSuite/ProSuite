using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.RemoveOverlaps
{
	public class RemoveOverlapsToolBase : TwoPhaseEditToolBase
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private Overlaps _overlaps;
		private RemoveOverlapsFeedback _feedback;
		private IList<Feature> _overlappingFeatures;

		protected RemoveOverlapsToolBase()
		{
			IsSketchTool = true;
			SketchOutputMode = SketchOutputMode.Screen;
			GeomIsSimpleAsFeature = false;

			SelectionCursor = ToolUtils.GetCursor(Resources.RemoveOverlapsToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.RemoveOverlapsToolCursorShift);
			SecondPhaseCursor = ToolUtils.GetCursor(Resources.RemoveOverlapsToolCursorProcess);
		}

		protected override void OnToolActivatingCore()
		{
			_feedback = new RemoveOverlapsFeedback();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_feedback?.DisposeOverlays();
			_feedback = null;
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info(LocalizableStrings.RemoveOverlapsTool_LogPromptForSelection);
		}

		protected override bool CanUseSelection(IEnumerable<Feature> selectedFeatures)
		{
			IEnumerable<FeatureClass> featureClasses =
				selectedFeatures.Select(f => f.GetTable()).Distinct();

			return featureClasses.Any(fc =>
			{
				GeometryType geometryType = fc.GetDefinition().GetShapeType();

				return geometryType == GeometryType.Polygon ||
				       geometryType == GeometryType.Polyline ||
				       geometryType == GeometryType.Multipatch;
			});
		}

		protected override void CalculateDerivedGeometries(IList<Feature> selectedFeatures,
		                                                   CancelableProgressor progressor)
		{
			// TODO:
			IList<Feature> overlappingFeatures = selectedFeatures;

			if (progressor != null && ! progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of removable overlaps was cancelled.");
				return;
			}

			_overlaps = CalculateOverlaps(selectedFeatures, overlappingFeatures, progressor);

			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of removable overlaps was cancelled.");
				return;
			}

			// TODO: Options
			bool insertVerticesInTarget = false;
			_overlappingFeatures = insertVerticesInTarget
				                       ? overlappingFeatures
				                       : null;

			_feedback.Update(_overlaps);
		}

		protected override bool CanUseDerivedGeometries()
		{
			return _overlaps != null && _overlaps.HasOverlaps();
		}

		protected override bool SelectAndProcessDerivedGeometry(
			Dictionary<MapMember, List<long>> selection, Geometry sketch,
			CancelableProgressor progressor)
		{
			// TODO
			return false;
		}

		protected override void ResetDerivedGeometries()
		{
			_overlaps = null;
			_feedback.DisposeOverlays();
		}

		protected override void LogDerivedGeometriesCalculated(CancelableProgressor progressor)
		{
			if (_overlaps != null && _overlaps.Notifications.Count > 0)
			{
				_msg.Info(_overlaps.Notifications.Concatenate(Environment.NewLine));

				if (! _overlaps.HasOverlaps())
				{
					_msg.InfoFormat("Select one or more different features.");
				}
			}
			else if (_overlaps == null || ! _overlaps.HasOverlaps())
			{
				_msg.Info(
					"No overlap of other polygons with current selection found. Select one or more different features.");
			}

			if (_overlaps != null && _overlaps.HasOverlaps())
			{
				string msg = _overlaps.OverlapGeometries.Count == 1
					             ? "Select the overlap to subtract from the selection"
					             : "Select one or more overlaps to subtract from the selection. Draw a box to select overlaps completely within the box.";

				_msg.InfoFormat(LocalizableStrings.RemoveOverlapsTool_AfterSelection, msg);
			}
		}

		protected override Task OnSelectionChangedAsync(MapSelectionChangedEventArgs e)
		{
			// NOTE: This method is not called when the selection is cleared by another command (e.g. by 'Clear Selection')
			//       Is there another way to get the global selection changed event? What if we need the selection changed in a button?

			//if (_shiftIsPressed) // always false -> toolkeyup is first. This method is apparently scheduled to run after key up
			//{
			//	return Task.FromResult(true);
			//}

			CancelableProgressor progressor = GetOverlapsCalculationProgressor();

			if (IsInSelectionPhase())
			{
				var selectedFeatures = SelectionUtils.GetSelectedFeatures(e.Selection).ToList();

				if (CanUseSelection(selectedFeatures))
				{
					AfterSelection(selectedFeatures, progressor);

					var sketch = GetCurrentSketchAsync().Result;

					SelectAndProcessDerivedGeometry(e.Selection, sketch, progressor);
				}
			}

			return Task.FromResult(true);
		}

		protected CancelableProgressor GetOverlapsCalculationProgressor()
		{
			var overlapsCalculationProgressorSource = new CancelableProgressorSource(
				"Calculating overlaps...", "cancelled", true);

			CancelableProgressor selectionProgressor =
				overlapsCalculationProgressorSource.Progressor;

			return selectionProgressor;
		}

		private Overlaps CalculateOverlaps(IList<Feature> selectedFeatures,
		                                   IList<Feature> overlappingFeatures,
		                                   CancelableProgressor progressor)
		{
			Overlaps overlaps = null;

			// TEST:
			if (selectedFeatures.Count > 0)
			{
				return new Overlaps(new[] {selectedFeatures[0].GetShape()});
			}

			//if (MicroserviceClient != null)
			//{
			//	overlaps =
			//		MicroserviceClient.CalculateOverlaps(selectedFeatures, overlappingFeatures,
			//			progressor);
			//}

			return overlaps;
		}

		//public RemoveOverlapsClient MicroserviceClient { get; set; }
	}
}
