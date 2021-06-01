using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Keyboard;
using ProSuite.Microservices.Client.AGP;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedReshape;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong;
using Cursor = System.Windows.Input.Cursor;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class ChangeGeometryAlongToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private ChangeAlongCurves _changeAlongCurves;

		private ChangeAlongFeedback _feedback;

		protected ChangeGeometryAlongToolBase()
		{
			IsSketchTool = true;

			GeomIsSimpleAsFeature = false;

			PolygonSketchCursor = ToolUtils.GetCursor(Resources.PolygonDrawerCursor);
		}

		protected Cursor TargetSelectionCursor { get; set; }
		protected Cursor TargetSelectionCursorShift { get; set; }

		protected Cursor PolygonSketchCursor { get; set; }

		protected abstract string EditOperationDescription { get; }
		protected abstract GeometryProcessingClient MicroserviceClient { get; }

		protected override bool HandleEscape()
		{
			QueuedTaskUtils.Run(
				() =>
				{
					if (IsInSubcurveSelectionPhase())
					{
						ResetDerivedGeometries();
					}
					else
					{
						SelectionUtils.ClearSelection(ActiveMapView.Map);
						StartSelectionPhase();
					}

					return true;
				});

			return true;
		}
		
		protected override void OnToolActivatingCore()
		{
			_feedback = new ChangeAlongFeedback();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_feedback?.DisposeOverlays();
			_feedback = null;
		}

		protected override bool OnMapSelectionChangedCore(MapSelectionChangedEventArgs args)
		{
			if (args.Selection.Count == 0)
			{
				ResetDerivedGeometries();
				StartSelectionPhase();
			}

			return true;
		}

		protected override Task OnEditCompletedCore(EditCompletedEventArgs args)
		{
			bool requiresRecalculate = args.CompletedType == EditCompletedType.Discard ||
			                           args.CompletedType == EditCompletedType.Reconcile ||
			                           args.CompletedType == EditCompletedType.Redo ||
			                           args.CompletedType == EditCompletedType.Undo;

			if (requiresRecalculate)
			{
				QueuedTask.Run(
					() =>
					{
						var selectedFeatures =
							SelectionUtils.GetSelectedFeatures(ActiveMapView).ToList();

						RefreshCutSubcurves(selectedFeatures, GetCancelableProgressor());

						return true;
					});
			}

			return base.OnEditCompletedCore(args);
		}

		protected override void AfterSelection(IList<Feature> selectedFeatures,
		                                       CancelableProgressor progressor)
		{
			if (! CanUseSelection(selectedFeatures))
			{
				StartSelectionPhase();

				return;
			}

			StartTargetSelectionPhase();
		}

		protected override async Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			var result = await QueuedTask.Run(async () =>
			{
				List<Feature> selection = GetApplicableSelectedFeatures(ActiveMapView).ToList();

				if (! IsInSubcurveSelectionPhase())
				{
					// 2. Phase: target selection:
					return SelectTargets(selection, sketchGeometry, progressor);
				}

				// 3. Phase: reshape/cut line selection:
				List<CutSubcurve> cutSubcurves = GetSelectedCutSubcurves(sketchGeometry);

				if (cutSubcurves.Count > 0)
				{
					return await UpdateFeatures(selection, cutSubcurves, progressor);
				}

				// No subcurve hit, try target selection instead
				return SelectTargets(selection, sketchGeometry, progressor);
			});

			return result;
		}

		protected override void OnKeyDownCore(MapViewKeyEventArgs k)
		{
			if (k.Key == Key.P)
			{
				SketchType = SketchGeometryType.Polygon;

				SetCursor(PolygonSketchCursor);
			}
		}

		protected override void OnKeyUpCore(MapViewKeyEventArgs k)
		{
			if (k.Key == Key.P)
			{
				SketchType = SketchGeometryType.Rectangle;

				if (! IsInSelectionPhase())
				{
					SetCursor(TargetSelectionCursor);
				}
				else
				{
					SetCursor(SelectionCursor);
				}
			}
		}

		protected override void ShiftPressedCore()
		{
			if (SelectionCursorShift != null && HasReshapeCurves())
			{
				SetCursor(TargetSelectionCursorShift);
			}
			else
			{
				base.ShiftPressedCore();
			}
		}

		protected override void ShiftReleasedCore()
		{
			// From the subclass' point of view SHIFT is still pressed:
			if (!IsInSelectionPhase())
			{
				SetCursor(TargetSelectionCursor);
			}
			else
			{
				base.ShiftReleasedCore();
			}
		}


		protected override bool IsInSelectionPhase(bool shiftIsPressed)
		{
			if (HasReshapeCurves())
			{
				return false;
			}

			// First or second phase:
			if (shiftIsPressed)
			{
				// With reshape curves and shift it would mean we're in the targest selection phase
				return ! HasReshapeCurves();
			}

			var task = QueuedTask.Run(
				() =>
				{
					IList<Feature> selection =
						SelectionUtils.GetSelectedFeatures(ActiveMapView).ToList();

					return ! CanUseSelection(selection);
				});

			return task.Result;
		}

		private bool HasReshapeCurves()
		{
			return _changeAlongCurves != null && _changeAlongCurves.HasSelectableCurves;
		}

		protected bool IsInSubcurveSelectionPhase()
		{
			return HasReshapeCurves() &&
			       ! KeyboardUtils.IsModifierPressed(Keys.Shift, true);
		}

		protected virtual bool CanUseAsTargetLayer(FeatureLayer featureLayer)
		{
			return featureLayer.ShapeType == esriGeometryType.esriGeometryPolyline ||
			       featureLayer.ShapeType == esriGeometryType.esriGeometryPolygon;
		}

		protected virtual bool CanUseAsTargetFeature([NotNull] IList<Feature> selection,
		                                             [NotNull] Feature testFeature)
		{
			foreach (Feature selectedFeature in selection)
			{
				if (selectedFeature.GetObjectID() == testFeature.GetObjectID() &&
				    selectedFeature.GetTable().Handle == testFeature.GetTable().Handle)
				{
					// already selected
					return false;
				}
			}

			return true;
		}

		private void StartTargetSelectionPhase()
		{
			Cursor = TargetSelectionCursor;

			SketchOutputMode = SketchOutputMode.Map;

			// NOTE: CompleteSketchOnMouseUp must be set before the sketch geometry type,
			// otherwise it has no effect!
			CompleteSketchOnMouseUp = true;

			SketchType = SketchGeometryType.Rectangle;

			UseSnapping = false;

			GeomIsSimpleAsFeature = false;
		}

		private bool SelectTargets(List<Feature> selectedFeatures, Geometry sketch,
		                         CancelableProgressor progressor)
		{
			// TODO: maintain target selection to allow add/remove to
			const TargetFeatureSelection targetFeatureSelection =
				TargetFeatureSelection.VisibleFeatures;

			sketch = ToolUtils.SketchToSearchGeometry(sketch, GetSelectionTolerancePixels(), out bool _);

			Predicate<Feature> canUseAsTargetFeature =
				t => CanUseAsTargetFeature(selectedFeatures, t);

			SpatialRelationship spatialRel =
				SketchType == SketchGeometryType.Polygon
					? SpatialRelationship.Contains
					: SpatialRelationship.Intersects;

			var foundOidsByLayer =
				MapUtils.FindFeatures(ActiveMapView, sketch, spatialRel,
				                      targetFeatureSelection, CanUseAsTargetLayer,
				                      canUseAsTargetFeature, selectedFeatures, progressor);

			// TODO: Picker if single click and several found

			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of reshape lines was cancelled.");
				return false;
			}

			IList<Feature> allTargetFeatures =
				GetDistinctSelectedFeatures(foundOidsByLayer, _changeAlongCurves?.TargetFeatures,
				                    KeyboardUtils.IsModifierPressed(Keys.Shift));

			_changeAlongCurves =
				allTargetFeatures.Count > 0
					? CalculateReshapeCurves(selectedFeatures, allTargetFeatures, progressor)
					: new ChangeAlongCurves(new List<CutSubcurve>(),
					                        ReshapeAlongCurveUsability.NoTarget);

			_feedback.Update(_changeAlongCurves);

			return true;
		}

		private static IList<Feature> GetDistinctSelectedFeatures(
			[NotNull] IEnumerable<KeyValuePair<FeatureClass, List<Feature>>> foundFeaturesByClass,
			[CanBeNull] IList<Feature> existingSelection,
			bool xor)
		{
			var resultDictionary = new Dictionary<GdbObjectReference, Feature>();

			if (existingSelection != null)
			{
				AddRange(existingSelection, resultDictionary);
			}

			foreach (var keyValuePair in foundFeaturesByClass)
			{
				if (xor)
				{
					foreach (Feature selected in keyValuePair.Value)
					{
						var selectedObjRef = new GdbObjectReference(
							selected.GetTable().Handle.ToInt64(),
							selected.GetObjectID());

						if (resultDictionary.ContainsKey(selectedObjRef))
						{
							resultDictionary.Remove(selectedObjRef);
						}
						else
						{
							resultDictionary.Add(selectedObjRef, selected);
						}
					}
				}
				else
				{
					AddRange(keyValuePair.Value, resultDictionary);
				}
			}

			IList<Feature> allTargetFeatures = resultDictionary.Values.ToList();

			return allTargetFeatures;
		}

		private static void AddRange(
			[NotNull] IList<Feature> features,
			[NotNull] IDictionary<GdbObjectReference, Feature> resultDictionary)
		{
			foreach (Feature target in features)
			{
				var objRef = new GdbObjectReference(target.GetTable().Handle.ToInt64(),
				                                    target.GetObjectID());

				if (! resultDictionary.ContainsKey(objRef))
				{
					resultDictionary.Add(objRef, target);
				}
			}
		}

		private List<CutSubcurve> GetSelectedCutSubcurves(Geometry sketch)
		{
			sketch = ToolUtils.SketchToSearchGeometry(sketch, GetSelectionTolerancePixels(),
			                                   out bool singlePick);

			Predicate<CutSubcurve> canReshapePredicate =
				cutSubcurve => ToolUtils.IsSelected(sketch, cutSubcurve.Path, singlePick);

			var cutSubcurves = _changeAlongCurves.ReshapeCutSubcurves
			                                     .Where(c => canReshapePredicate(c))
			                                     .ToList();
			return cutSubcurves;
		}


		protected void ResetDerivedGeometries()
		{
			_feedback.DisposeOverlays();
			_changeAlongCurves = null;
		}

		private ChangeAlongCurves CalculateReshapeCurves(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> targetFeatures,
			[CanBeNull] CancelableProgressor progressor)
		{
			ChangeAlongCurves result;

			CancellationToken cancellationToken;

			if (progressor != null)
			{
				cancellationToken = progressor.CancellationToken;
			}
			else
			{
				var cancellationTokenSource = new CancellationTokenSource();
				cancellationToken = cancellationTokenSource.Token;
			}

			if (MicroserviceClient != null)
			{
				result =
					MicroserviceClient.CalculateReshapeLines(
						selectedFeatures, targetFeatures, cancellationToken);
				result.TargetFeatures = targetFeatures;
			}
			else
			{
				throw new InvalidConfigurationException("Microservice has not been started.");
			}

			return result;
		}

		private void RefreshCutSubcurves([NotNull] IList<Feature> selectedFeatures,
		                                 [CanBeNull] CancelableProgressor progressor = null)
		{
			if (_changeAlongCurves == null ||
			    _changeAlongCurves.TargetFeatures == null ||
			    _changeAlongCurves.TargetFeatures.Count == 0)
			{
				return;
			}

			ChangeAlongCurves newState =
				CalculateReshapeCurves(selectedFeatures, _changeAlongCurves.TargetFeatures,
				                       progressor);

			_changeAlongCurves.Update(newState);

			_feedback.Update(_changeAlongCurves);
		}

		private async Task<bool> UpdateFeatures(List<Feature> selectedFeatures,
		                                        List<CutSubcurve> cutSubcurves,
		                                        CancelableProgressor progressor)
		{
			CancellationToken cancellationToken =
				progressor?.CancellationToken ?? new CancellationTokenSource().Token;

			ChangeAlongCurves newChangeAlongCurves;
			var updatedFeatures = MicroserviceClient.ApplyReshapeLines(
				selectedFeatures, _changeAlongCurves.TargetFeatures, cutSubcurves,
				cancellationToken, out newChangeAlongCurves);

			_changeAlongCurves = newChangeAlongCurves;

			_feedback.Update(_changeAlongCurves);

			Dictionary<Feature, Geometry> resultFeatures =
				updatedFeatures.ToDictionary(r => r.Feature,
				                             r => r.UpdatedGeometry);

			// TODO
			//LogReshapeResults(result, selection.Count);

			var success = await GdbPersistenceUtils.SaveInOperationAsync(
				              EditOperationDescription, resultFeatures);

			return success;

			//var sketchPolyline = cutSubcurves[0].Path;

			//// create an edit operation
			//var editOperation = new EditOperation();

			//editOperation.Name = "Reshape along";
			//editOperation.ProgressMessage = "Working...";
			//editOperation.CancelMessage = "Operation canceled";
			//editOperation.ErrorMessage = "Error creating points";

			//editOperation.Callback(editContext => UpdateFeatures(editContext, updatedFeatures),
			//                       selectedFeatures.Select(f => f.GetTable()).Distinct());

			//// synchronous execution ok, already on a queued task?!:
			//var editOperationResult = editOperation.ExecuteAsync();

			//_changeAlongCurves = newChangeAlongCurves;

			//_feedback.Update(_changeAlongCurves);

			//return editOperationResult;
		}

		private static void UpdateFeatures(
			EditOperation.IEditContext editContext,
			List<ReshapeResultFeature> resultFeatures)
		{
			foreach (var reshapeResultFeature in resultFeatures)
			{
				var feature = reshapeResultFeature.Feature;

				editContext.Invalidate(feature);

				feature.SetShape(reshapeResultFeature.UpdatedGeometry);
				feature.Store();

				editContext.Invalidate(feature);
			}
		}
	}
}
