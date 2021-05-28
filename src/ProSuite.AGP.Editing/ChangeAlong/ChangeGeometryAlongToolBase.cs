using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
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
		}

		protected Cursor TargetSelectionCursor { get; set; }

		protected abstract GeometryProcessingClient MicroserviceClient { get; }

		protected virtual bool CanUseAsTargetLayer(FeatureLayer featureLayer)
		{
			return featureLayer.ShapeType == esriGeometryType.esriGeometryPolyline ||
			       featureLayer.ShapeType == esriGeometryType.esriGeometryPolygon;
		}

		protected override bool HandleEscape()
		{
			QueuedTaskUtils.Run(
				() =>
				{
					if (IsInSubcurvePickingMode())
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

		protected override void OnSelectionPhaseStarted()
		{
			// Already set in StartSelectionPhase()
			//Cursor = SelectionCursor;
		}

		protected override void OnToolActivatingCore()
		{
			_feedback = new ChangeAlongFeedback();

			// Some of the sketch properties have to be re-set

			// TODO: Test with and without:
			//SketchOutputMode = SketchOutputMode.Map;
			//SketchType = SketchGeometryType.Rectangle;

			//GeomIsSimpleAsFeature = false;
			//CompleteSketchOnMouseUp = true;
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

			StartSecondPhase();
		}

		protected override async Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			var result = await QueuedTask.Run(async () =>
			{
				List<Feature> selectedFeatures =
					SelectionUtils.GetSelectedFeatures(ActiveMapView).ToList();

				if (IsInSubcurvePickingMode())
				{
					Predicate<CutSubcurve> canReshapePredicate =
						cutSubcurve => GeometryUtils.Contains(sketchGeometry, cutSubcurve.Path);

					var cutSubcurves = _changeAlongCurves.ReshapeCutSubcurves
					                                     .Where(c => canReshapePredicate(c))
					                                     .ToList();

					if (cutSubcurves.Count == 0)
						// TODO: if nothing hit at all, select target
						return false;

					return await UpdateFeatures(selectedFeatures, cutSubcurves, progressor);
				}
				else
				{
					// TODO: maintain target selection to allow add/remove to
					const TargetFeatureSelection targetFeatureSelection =
						TargetFeatureSelection.VisibleFeatures;

					//var mapSketch = MapUtils.ToMapGeometry(MapView.Active, (Polygon)sketchGeometry);
					var mapSketch = sketchGeometry;

					var foundOidsByLayer =
						MapUtils.FindFeatures(ActiveMapView, mapSketch,
						                      targetFeatureSelection, CanUseAsTargetLayer,
						                      selectedFeatures,
						                      progressor);

					if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
					{
						_msg.Warn("Calculation of reshape lines was cancelled.");
						return false;
					}

					var targetFeatures = new List<Feature>();

					foreach (var keyValuePair in foundOidsByLayer)
						targetFeatures.AddRange(keyValuePair.Value);

					_changeAlongCurves =
						targetFeatures.Count > 0
							? CalculateReshapeCurves(selectedFeatures, targetFeatures, progressor)
							: new ChangeAlongCurves(new List<CutSubcurve>(),
							                        ReshapeAlongCurveUsability.NoTarget);

					if (_changeAlongCurves.HasSelectableCurves)
					{
						//Cursor = 
					}

					_feedback.Update(_changeAlongCurves);

					return true;
				}

				//return SelectAndProcessCutSubcurves(selection, sketch, progressor);
				return true;
			});

			return result;
		}

		protected override bool IsInSelectionPhase()
		{
			if (IsInSubcurvePickingMode())
			{
				return false;
			}

			// First or second phase:
			if (KeyboardUtils.IsModifierPressed(Keys.Shift, true))
			{
				return true;
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

		private void StartSecondPhase()
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

		protected void ResetDerivedGeometries()
		{
			_feedback.DisposeOverlays();
			_changeAlongCurves = null;
		}

		protected bool IsInSubcurvePickingMode()
		{
			return _changeAlongCurves != null && _changeAlongCurves.HasSelectableCurves &&
			       ! KeyboardUtils.IsModifierPressed(Keys.Shift, true);
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
				              "Advanced reshape", resultFeatures);

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
