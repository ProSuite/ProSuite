using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;
using ProSuite.Commons.UI.Input;
using ProSuite.Microservices.Client.AGP;
using ProSuite.Microservices.Client.AGP.GeometryProcessing;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.ChangeAlong;

namespace ProSuite.AGP.Editing.ChangeAlong
{
	public abstract class ChangeGeometryAlongToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected ChangeAlongCurves ChangeAlongCurves { get; private set; }

		private ChangeAlongFeedback _feedback;

		private const Key _keyPolygonDraw = Key.P;

		protected ChangeGeometryAlongToolBase()
		{
			IsSketchTool = true;

			GeomIsSimpleAsFeature = false;

			PolygonSketchCursor = ToolUtils.GetCursor(Resources.PolygonDrawerCursor);

			HandledKeys.Add(_keyPolygonDraw);
		}

		protected Cursor TargetSelectionCursor { get; set; }
		protected Cursor TargetSelectionCursorShift { get; set; }

		protected bool DisplayTargetLines { get; set; }
		protected Cursor PolygonSketchCursor { get; set; }

		protected abstract string EditOperationDescription { get; }

		protected abstract GeometryProcessingClient MicroserviceClient { get; }

		protected override void OnUpdate()
		{
			Enabled = MicroserviceClient != null;

			if (MicroserviceClient == null)
				DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
		}

		protected override async Task HandleEscapeAsync()
		{
			Task task = QueuedTask.Run(
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
				});

			await ViewUtils.TryAsync(task, _msg);
		}

		protected override void OnToolActivatingCore()
		{
			_feedback = new ChangeAlongFeedback()
			            {
				            ShowTargetLines = DisplayTargetLines
			            };
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
			else
			{
				// E.g. a part of the selection has been removed (e.g. using 'clear selection' on a layer)
				Dictionary<MapMember, List<long>> selectionByLayer = args.Selection.ToDictionary();
				IList<Feature> applicableSelection =
					GetApplicableSelectedFeatures(selectionByLayer).ToList();

				RefreshExistingChangeAlongCurves(applicableSelection, GetCancelableProgressor());
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

							RefreshExistingChangeAlongCurves(selectedFeatures,
							                                 GetCancelableProgressor());

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
			if (k.Key == _keyPolygonDraw)
			{
				SetupSketch(SketchGeometryType.Polygon);

				SetCursor(PolygonSketchCursor);
			}
		}

		protected override void OnKeyUpCore(MapViewKeyEventArgs k)
		{
			if (k.Key == _keyPolygonDraw)
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
				// With reshape curves and shift it would mean we're in the target selection phase
				return ! HasReshapeCurves();
			}

			var task = QueuedTask.Run(() => ! CanUseSelection(ActiveMapView));

			// NOTE: In rare situations this can result in a dead-lock / hang of the application.
			return task.Result;
		}

		protected override async Task<bool> IsInSelectionPhaseCoreAsync(bool shiftDown)
		{
			if (HasReshapeCurves())
			{
				return false;
			}

			// First or second phase:
			if (shiftDown)
			{
				// With reshape curves and shift it would mean we're in the target selection phase
				return ! HasReshapeCurves();
			}

			return await QueuedTask.Run(() => ! CanUseSelection(ActiveMapView));
		}

		private bool HasReshapeCurves()
		{
			// Test for target features because in cut along the curves are not provided until
			// there is a cut (but the targets get symbolized).
			return ChangeAlongCurves != null && ChangeAlongCurves.TargetFeatures?.Count > 0;
		}

		protected bool IsInSubcurveSelectionPhase()
		{
			bool shiftDown = KeyboardUtils.IsModifierDown(Key.LeftShift, exclusive: true) ||
			                 KeyboardUtils.IsModifierDown(Key.RightShift, exclusive: true);

			return HasReshapeCurves() && ! shiftDown;
		}

		protected virtual bool CanUseAsTargetLayer(Layer layer)
		{
			if (layer is FeatureLayer featureLayer)
			{
				return featureLayer.ShapeType == esriGeometryType.esriGeometryPolyline ||
				       featureLayer.ShapeType == esriGeometryType.esriGeometryPolygon;
			}

			return false;
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

			SetupSketch(SketchGeometryType.Rectangle);
		}

		private async Task<bool> SelectTargetsAsync(
			[NotNull] List<Feature> selectedFeatures,
			[NotNull] Geometry sketch,
			[CanBeNull] CancelableProgressor progressor)
		{
			const TargetFeatureSelection targetFeatureSelection =
				TargetFeatureSelection.VisibleSelectableFeatures;

			bool isSingleClick = false;
			Point pickerLocation = new Point();
			List<FeatureSelectionBase> selectionByClass =
				await QueuedTaskUtils.Run(() =>
				{
					sketch = ToolUtils.SketchToSearchGeometry(
						sketch, GetSelectionTolerancePixels(), out isSingleClick);

					pickerLocation = MapView.Active.MapToScreen(sketch.Extent.Center);

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
			    SelectionUtils.GetFeatureCount(selectionByClass) > 1)
			{
				IEnumerable<IPickableItem> items =
					await QueuedTask.Run(
						() => PickableItemsFactory.CreateFeatureItems(
							PickerUtils.OrderByGeometryDimension(selectionByClass)));

				PickerPrecedence.SelectionGeometry = sketch;

				IPickableFeatureItem pickedItem =
					await ShowPickerAsync<IPickableFeatureItem>(
						items, PickerPrecedence, pickerLocation);

				if (pickedItem == null)
				{
					return false;
				}

				targetFeatures = new[] { pickedItem.Feature };
			}
			else
			{
				targetFeatures = selectionByClass.SelectMany(fcs => fcs.GetFeatures());
			}

			ChangeAlongCurves =
				await QueuedTaskUtils.Run(
					() => RefreshChangeAlongCurves(selectedFeatures, targetFeatures, progressor));

			return true;
		}

		private List<FeatureSelectionBase> FindTargetFeatureCandidates(
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

			FeatureFinder featureFinder = new FeatureFinder(ActiveMapView, targetFeatureSelection)
			                              {
				                              SelectedFeatures = selectedFeatures,
				                              SpatialRelationship = spatialRel
			                              };

			var selectionByClass =
				featureFinder.FindFeaturesByFeatureClass(sketch, CanUseAsTargetLayer,
				                                         canUseAsTargetFeature, progressor)
				             .ToList();

			return selectionByClass;
		}

		private ChangeAlongCurves RefreshChangeAlongCurves(
			[NotNull] IList<Feature> selectedFeatures,
			[NotNull] IEnumerable<Feature> targetFeatures,
			[CanBeNull] CancelableProgressor progressor)
		{
			bool shiftDown = KeyboardUtils.IsShiftDown();

			IList<Feature> actualTargetFeatures = GetDistinctTargetFeatures(
				targetFeatures, ChangeAlongCurves?.TargetFeatures, shiftDown);

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
			_feedback?.DisposeOverlays();
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

			SpatialReference mapSr = MapView.Active.Map.SpatialReference;

			// After undo/redo the shape's spatial reference could have been changed.
			ChangeAlongCurves.TargetFeatures =
				ReRead(ChangeAlongCurves.TargetFeatures, mapSr).ToList();

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

			MapView activeMap = MapView.Active;

			IList<Feature> targetFeatures = Assert.NotNull(ChangeAlongCurves.TargetFeatures);

			List<ResultFeature> updatedFeatures = ChangeFeaturesAlong(
				selectedFeatures, targetFeatures, cutSubcurves, cancellationToken,
				out ChangeAlongCurves newChangeAlongCurves);

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

			HashSet<long> editableClassHandles = ToolUtils.GetEditableClassHandles(activeMap);

			// Updates:
			Dictionary<Feature, Geometry> resultFeatures =
				updatedFeatures
					.Where(f => IsStoreRequired(
						       f, editableClassHandles, RowChangeType.Update))
					.ToDictionary(r => r.OriginalFeature, r => r.NewGeometry);

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
				               GdbPersistenceUtils.GetDatasetsNonEmpty(resultFeatures.Keys));

			LogReshapeResults(updatedFeatures, resultFeatures);

			ToolUtils.SelectNewFeatures(newFeatures, activeMap);

			SpatialReference outputSpatialReference = activeMap.Map.SpatialReference;

			if (ChangeAlongCurves.TargetFeatures != null)
			{
				ChangeAlongCurves.TargetFeatures =
					ReRead(ChangeAlongCurves.TargetFeatures, outputSpatialReference).ToList();
			}

			return success;
		}

		private static IEnumerable<Feature> ReRead([NotNull] IList<Feature> features,
		                                           [CanBeNull]
		                                           SpatialReference outputSpatialReference)
		{
			var groupedByClass = features.GroupBy(f => f.GetTable().GetID());

			foreach (IGrouping<long, Feature> grouping in groupedByClass)
			{
				FeatureClass featureClass = grouping.FirstOrDefault()?.GetTable();

				if (featureClass == null)
				{
					continue;
				}

				foreach (Feature feature in GdbQueryUtils.GetFeatures(
					         featureClass, grouping.Select(f => f.GetObjectID()),
					         outputSpatialReference, false))
				{
					yield return feature;
				}
			}
		}

		private static bool IsStoreRequired([NotNull] ResultFeature resultFeature,
		                                    [NotNull] HashSet<long> editableClassHandles,
		                                    RowChangeType changeType)
		{
			if (! GdbPersistenceUtils.CanChange(resultFeature, editableClassHandles, changeType))
			{
				return false;
			}

			Feature feature = resultFeature.OriginalFeature;

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
				if (savedUpdates.ContainsKey(resultFeature.OriginalFeature) &&
				    resultFeature.Messages.Count == 1)
				{
					_msg.Info(resultFeature.Messages[0]);
				}
			}
		}
	}
}
