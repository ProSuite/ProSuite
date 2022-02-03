using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
using ProSuite.AGP.Editing.Picker;
using ProSuite.AGP.Editing.Properties;
using ProSuite.AGP.Editing.Selection;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Keyboard;
using ProSuite.Microservices.Client.AGP;
using ProSuite.Microservices.Client.AGP.GeometryProcessing;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong;
using Cursor = System.Windows.Input.Cursor;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class ChangeGeometryAlongToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected ChangeAlongCurves ChangeAlongCurves { get; private set; }

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
						SelectionUtils.ClearSelection();
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
			ResetDerivedGeometries();
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
							GetApplicableSelectedFeatures(ActiveMapView).ToList();

						RefreshExistingChangeAlongCurves(selectedFeatures,
						                                 GetCancelableProgressor());

						return true;
					});
			}

			return base.OnEditCompletedCore(args);
		}

		protected override void AfterSelection(IList<Feature> selectedFeatures,
		                                       CancelableProgressor progressor)
		{
			StartTargetSelectionPhase();
		}

		protected override async Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			CancelableProgressor progressor)
		{
			List<Feature> selection =
				await QueuedTask.Run(
					() => GetApplicableSelectedFeatures(ActiveMapView).ToList());

			if (! IsInSubcurveSelectionPhase())
			{
				// 2. Phase: target selection:
				return await SelectTargetsAsync(selection, sketchGeometry, progressor);
			}

			// 3. Phase: reshape/cut line selection:
			List<CutSubcurve> cutSubcurves =
				await QueuedTask.Run(() => GetSelectedCutSubcurves(sketchGeometry));

			if (cutSubcurves.Count == 0)
			{
				// No subcurve hit, try target selection instead
				return await SelectTargetsAsync(selection, sketchGeometry, progressor);
			}

			if (selection.Count == 0)
			{
				_msg.Warn("No usable selected features.");
				return false;
			}

			return await QueuedTask.Run(() => UpdateFeatures(selection, cutSubcurves, progressor));
		}

		protected override void OnKeyDownCore(MapViewKeyEventArgs k)
		{
			if (k.Key == Key.P)
			{
				SetupSketch(SketchGeometryType.Polygon);

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
			if (! IsInSelectionPhase())
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

			var task = QueuedTask.Run(() => ! CanUseSelection(ActiveMapView));

			return task.Result;
		}

		private bool HasReshapeCurves()
		{
			return ChangeAlongCurves != null && ChangeAlongCurves.HasSelectableCurves;
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

		protected abstract void LogAfterPickTarget(
			ReshapeAlongCurveUsability reshapeCurveUsability);

		protected abstract ChangeAlongCurves CalculateChangeAlongCurves(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IList<Feature> targetFeatures,
			CancellationToken cancellationToken);

		protected abstract List<ResultFeature> ChangeFeaturesAlong(
			List<Feature> selectedFeatures, [NotNull] IList<Feature> targetFeatures,
			[NotNull] List<CutSubcurve> cutSubcurves,
			CancellationToken cancellationToken,
			out ChangeAlongCurves newChangeAlongCurves);

		private void StartTargetSelectionPhase()
		{
			Cursor = TargetSelectionCursor;

			SetupRectangleSketch();
		}

		private async Task<bool> SelectTargetsAsync(
			[NotNull] List<Feature> selectedFeatures,
			[NotNull] Geometry sketch,
			[CanBeNull] CancelableProgressor progressor)
		{
			const TargetFeatureSelection targetFeatureSelection =
				TargetFeatureSelection.VisibleSelectableFeatures;

			bool isSingleClick = false;
			Point pickerWindowLocation = new Point();
			List<FeatureClassSelection> selectionByClass =
				await QueuedTaskUtils.Run(() =>
				{
					DisposeOverlays();

					sketch = ToolUtils.SketchToSearchGeometry(
						sketch, GetSelectionTolerancePixels(), out isSingleClick);

					pickerWindowLocation = MapView.Active.MapToScreen(sketch.Extent.Center);

					return FindTargetFeatureCandidates(sketch, targetFeatureSelection,
					                                   selectedFeatures,
					                                   progressor);
				});

			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of reshape lines was cancelled.");
				return false;
			}

			IEnumerable<Feature> targetFeatures;

			if (isSingleClick &&
			    selectionByClass.Sum(s => s.FeatureCount) > 1)
			{
				Feature feature = await PickSingleFeature(selectionByClass, pickerWindowLocation);

				if (feature == null)
				{
					return false;
				}

				targetFeatures = new[] {feature};
			}
			else
			{
				targetFeatures = selectionByClass.SelectMany(fcs => fcs.Features);
			}

			ChangeAlongCurves =
				await QueuedTaskUtils.Run(
					() => RefreshChangeAlongCurves(selectedFeatures, targetFeatures, progressor));

			return true;
		}

		private List<FeatureClassSelection> FindTargetFeatureCandidates(
			[NotNull] Geometry sketch,
			TargetFeatureSelection targetFeatureSelection,
			[NotNull] List<Feature> selectedFeatures,
			CancelableProgressor progressor)
		{
			Predicate<Feature> canUseAsTargetFeature =
				t => CanUseAsTargetFeature(selectedFeatures, t);

			SpatialRelationship spatialRel =
				SketchType == SketchGeometryType.Polygon
					? SpatialRelationship.Contains
					: SpatialRelationship.Intersects;

			var selectionByClass =
				MapUtils.FindFeatures(ActiveMapView, sketch, spatialRel,
				                      targetFeatureSelection, CanUseAsTargetLayer,
				                      canUseAsTargetFeature, selectedFeatures, progressor).ToList();
			return selectionByClass;
		}

		private static async Task<Feature> PickSingleFeature(
			[NotNull] List<FeatureClassSelection> selectionByClass,
			Point pickerWindowLocation)
		{
			List<IPickableItem> pickables =
				await QueuedTaskUtils.Run(
					delegate
					{
						selectionByClass =
							GeometryReducer.ReduceByGeometryDimension(selectionByClass)
							               .ToList();

						return PickerUI.Picker.CreatePickableFeatureItems(selectionByClass);
					});

			PickerUI.Picker picker = new PickerUI.Picker(pickables, pickerWindowLocation);

			// Must not be called from a background Task!
			PickableFeatureItem item = await picker.PickSingle() as PickableFeatureItem;

			return item?.Feature;
		}

		private ChangeAlongCurves RefreshChangeAlongCurves(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IEnumerable<Feature> targetFeatures,
			[CanBeNull] CancelableProgressor progressor)
		{
			bool shiftPressed = KeyboardUtils.IsModifierPressed(Keys.Shift);

			IList<Feature> actualTargetFeatures = GetDistinctTargetFeatures(
				targetFeatures, ChangeAlongCurves?.TargetFeatures, shiftPressed);

			if (actualTargetFeatures.Count == 0)
			{
				_msg.Info("No target feature selected. Select one or more target features " +
				          "to align with. Press [ESC] to select a different feature.");

				return new ChangeAlongCurves(new List<CutSubcurve>(),
				                             ReshapeAlongCurveUsability.NoTarget);
			}

			ChangeAlongCurves =
				RefreshChangeAlongCurves(selectedFeatures, actualTargetFeatures, progressor);

			ChangeAlongCurves.LogTargetSelection();

			LogAfterPickTarget(ChangeAlongCurves.CurveUsability);

			_feedback.Update(ChangeAlongCurves);

			return ChangeAlongCurves;
		}

		private static IList<Feature> GetDistinctTargetFeatures(
			[NotNull] IEnumerable<Feature> foundFeatures,
			[CanBeNull] IList<Feature> existingTargetSelection,
			bool xor)
		{
			var resultDictionary = new Dictionary<GdbObjectReference, Feature>();

			if (xor && existingTargetSelection != null)
			{
				AddRange(existingTargetSelection, resultDictionary);
			}

			if (xor)
			{
				foreach (Feature selected in foundFeatures)
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
				AddRange(foundFeatures, resultDictionary);
			}

			IList<Feature> allTargetFeatures = resultDictionary.Values.ToList();

			return allTargetFeatures;
		}

		private static void AddRange(
			[NotNull] IEnumerable<Feature> features,
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

		private List<CutSubcurve> GetSelectedCutSubcurves([NotNull] Geometry sketch)
		{
			sketch = ToolUtils.SketchToSearchGeometry(sketch, GetSelectionTolerancePixels(),
			                                          out bool singlePick);

			Predicate<CutSubcurve> canReshapePredicate =
				cutSubcurve => ToolUtils.IsSelected(sketch, cutSubcurve.Path, singlePick);

			ChangeAlongCurves.PreSelectCurves(canReshapePredicate);

			var cutSubcurves =
				ChangeAlongCurves.GetSelectedReshapeCurves(canReshapePredicate, true);

			return cutSubcurves;
		}

		protected void ResetDerivedGeometries()
		{
			_feedback.DisposeOverlays();
			ChangeAlongCurves = null;
		}

		private ChangeAlongCurves RefreshChangeAlongCurves(
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
				result = CalculateChangeAlongCurves(selectedFeatures, targetFeatures,
				                                    cancellationToken);

				result.TargetFeatures = targetFeatures;
			}
			else
			{
				throw new InvalidConfigurationException("Microservice has not been started.");
			}

			return result;
		}

		private void RefreshExistingChangeAlongCurves(
			[NotNull] IList<Feature> selectedFeatures,
			[CanBeNull] CancelableProgressor progressor = null)
		{
			if (ChangeAlongCurves == null ||
			    ChangeAlongCurves.TargetFeatures == null ||
			    ChangeAlongCurves.TargetFeatures.Count == 0)
			{
				return;
			}

			ChangeAlongCurves newState =
				RefreshChangeAlongCurves(selectedFeatures, ChangeAlongCurves.TargetFeatures,
				                         progressor);

			ChangeAlongCurves.Update(newState);

			_feedback.Update(ChangeAlongCurves);
		}

		private async Task<bool> UpdateFeatures(List<Feature> selectedFeatures,
		                                        List<CutSubcurve> cutSubcurves,
		                                        CancelableProgressor progressor)
		{
			CancellationToken cancellationToken =
				progressor?.CancellationToken ?? new CancellationTokenSource().Token;

			ChangeAlongCurves newChangeAlongCurves;

			IList<Feature> targetFeatures = Assert.NotNull(ChangeAlongCurves.TargetFeatures);

			List<ResultFeature> updatedFeatures = ChangeFeaturesAlong(
				selectedFeatures, targetFeatures, cutSubcurves, cancellationToken,
				out newChangeAlongCurves);

			if (updatedFeatures.Count > 0)
			{
				// This also clears the PreSelected reshape curves
				ChangeAlongCurves = newChangeAlongCurves;
			}

			_feedback.Update(ChangeAlongCurves);

			if (updatedFeatures.Count == 0)
			{
				// Probably an additional yellow line needs to be selected
				return false;
			}

			HashSet<long> editableClassHandles =
				MapUtils.GetLayers<BasicFeatureLayer>(bfl => bfl.IsEditable)
				        .Select(l => l.GetTable().Handle.ToInt64()).ToHashSet();

			// Updates:
			Dictionary<Feature, Geometry> resultFeatures =
				updatedFeatures
					.Where(f => IsStoreRequired(
						       f, editableClassHandles, RowChangeType.Update))
					.ToDictionary(r => r.Feature, r => r.NewGeometry);

			// Inserts (in case of cut), grouped by original feature:
			var inserts = updatedFeatures
			              .Where(
				              f => IsStoreRequired(f, editableClassHandles, RowChangeType.Insert))
			              .ToList();

			List<Feature> newFeatures = new List<Feature>();

			bool success = await GdbPersistenceUtils.ExecuteInTransactionAsync(
				               delegate(EditOperation.IEditContext editContext)
				               {
					               GdbPersistenceUtils.UpdateTx(editContext, resultFeatures);

					               newFeatures.AddRange(
						               GdbPersistenceUtils.InsertTx(editContext, inserts));

					               return true;
				               },
				               EditOperationDescription,
				               GdbPersistenceUtils.GetDatasets(resultFeatures.Keys));

			LogReshapeResults(updatedFeatures, resultFeatures);

			ToolUtils.SelectNewFeatures(newFeatures, MapView.Active);

			return success;
		}

		private static bool IsStoreRequired([NotNull] ResultFeature resultFeature,
		                                    [NotNull] HashSet<long> editableClassHandles,
		                                    RowChangeType changeType)
		{
			if (! GdbPersistenceUtils.CanChange(resultFeature, editableClassHandles, changeType))
			{
				return false;
			}

			Feature feature = resultFeature.Feature;

			Geometry originalGeometry = feature.GetShape();

			if (changeType == RowChangeType.Update &&
			    originalGeometry != null &&
			    originalGeometry.IsEqual(resultFeature.NewGeometry))
			{
				_msg.DebugFormat("The geometry of feature {0} is unchanged. It will not be stored",
				                 GdbObjectUtils.ToString(feature));

				return false;
			}

			return true;
		}

		private void LogReshapeResults(List<ResultFeature> updatedFeatures,
		                               Dictionary<Feature, Geometry> savedUpdates)
		{
			foreach (ResultFeature resultFeature in updatedFeatures)
			{
				if (savedUpdates.ContainsKey(resultFeature.Feature) &&
				    resultFeature.Messages.Count == 1)
				{
					_msg.Info(resultFeature.Messages[0]);
				}
			}
		}
	}
}
