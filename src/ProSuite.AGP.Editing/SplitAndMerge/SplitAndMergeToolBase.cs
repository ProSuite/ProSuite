using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.MergeFeatures;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProSuite.AGP.Editing.SplitAndMerge
{
	public enum MergeAction
	{
		MergeIntoFirstFeature,
		MergeIntoSecondFeature
	}

	public abstract class SplitAndMergeToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[CanBeNull] private SplitAndMergeFeedback _feedback;

		MergeAction _mergeAction = MergeAction.MergeIntoFirstFeature;

		//private const Key _alternateSplitAndMergeKey = Key.LeftCtrl;
		private const Key _alternateSplitAndMergeKey = Key.A;
		public bool AlternateSplitAndMerge { get; private set; } = false;

		public bool SecondFeatureIsEditable { get; private set; }

		private Feature _firstFeature;
		private Feature _secondFeature;

		private SelectionCursors FirstPhaseCursors { get; } =
			SelectionCursors.CreateArrowCursors(Resources.MergeFeaturesOverlay1);

		private SelectionCursors SecondPhaseCursors { get; } =
			SelectionCursors.CreateArrowCursors(Resources.MergeFeaturesOverlay2);

		protected abstract Polygon GetPerimeter();

		protected SplitAndMergeToolBase()
		{
			//SelectOnlyEditFeatures = false;
			//SelectOnlySelectableFeatures = false;
			IsSketchTool = true;

			GeomIsSimpleAsFeature = false;

			HandledKeys.Add(_alternateSplitAndMergeKey);
		}

		protected virtual string OptionsFileName => "MergeToolOptions.xml";

		[CanBeNull]
		protected virtual string OptionsDockPaneID => null;

		[CanBeNull]
		protected virtual string CentralConfigDir => null;

		// ReSharper disable twice InvalidXmlDocComment
		/// <summary>
		/// By default, the local configuration directory shall be in
		/// %APPDATA%\Roaming\<organization>\<product>\ToolDefaults.
		/// </summary>
		protected virtual string LocalConfigDir
			=> EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(
				AppDataFolder.Roaming, "ToolDefaults");

		protected bool IsMerging { get; set; }


		#region MapToolBase and OneClickToolBase overrides


		protected override async Task HandleEscapeAsync()
		{
			try
			{
				await ClearSelectionAsync();

				_feedback?.Clear();
				_feedback?.ClearSelection();

				LogPromptForSelection();

				await StartSelectionPhaseAsync();
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
		}

		protected override Task OnSelectionPhaseStartedAsync()
		{
			_firstFeature = null;
			_secondFeature = null;
			AlternateSplitAndMerge = false;
			SecondFeatureIsEditable = false;

			//_feedback?.Clear();
			//_feedback?.ClearSelection();

			SelectionCursors = FirstPhaseCursors;
			SetToolCursor(SelectionCursors?.GetCursor(GetSketchType(), false));
			return base.OnSelectionPhaseStartedAsync();
		}

		protected override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
		{
			try
			{
				await base.HandleKeyDownAsync(args);

				if (args.Key == _alternateSplitAndMergeKey)
				{
					await QueuedTask.Run(async () =>
					{
						if (_firstFeature != null && _secondFeature != null)
						{
							AlternateSplitAndMerge = !AlternateSplitAndMerge;

							Polygon perimeter = GetPerimeter();
							if (perimeter == null)
							{
								_msg.Error("Unable to merge: No Perimeter avaiavle.");
								return;
							}
							Geometry firstGeometry = _firstFeature.GetShape();
							Geometry secondGeometry = _secondFeature.GetShape();

							var splitAndMerge = new SplitAndMergeGeometries(secondGeometry, firstGeometry, perimeter);
							//var splitAndMerge = new SplitAndMergeGeometries(firstGeometry, secondGeometry, perimeter);
							Geometry _mergeResult = splitAndMerge.MergeGeometriesOnPerimeter(AlternateSplitAndMerge);
							PressedKeys.Remove(_alternateSplitAndMergeKey);
							//show Overlay
							_ = ExecuteMerge(_mergeResult);

							_msg.InfoFormat($"Split and Merge use alternate: {AlternateSplitAndMerge}");

						}
					});
				}
			}
			catch (Exception e)
			{
				_msg.Warn("Error handling key press", e);
			}
		}

		protected override Task OnToolActivatingCoreAsync()
		{
			//_mergeToolOptions = InitializeOptions();

			_feedback = new SplitAndMergeFeedback();

			QueuedTask.Run(() => { _feedback.InitializeSymbolsQueued(); });
			return base.OnToolActivatingCoreAsync();

			//await QueuedTask.Run(() => { _feedback.InitializeSymbolsQueued(); });
			//await base.OnToolActivatingCoreAsync();
		}

		protected override Task OnToolDeactivateCore(bool hasMapViewChanged)
		{
			//_settingsProvider?.StoreLocalConfiguration(_mergeToolOptions.LocalOptions);

			_firstFeature = null;
			_secondFeature = null;
			//HideOptionsPane();

			_feedback?.Clear();
			_feedback?.ClearSelection();
			_feedback = null;

			return base.OnToolDeactivateCore(hasMapViewChanged);
		}

		protected override async Task AfterSelectionAsync(IList<Feature> selectedFeatures,
		                                                  CancelableProgressor progressor)
		{
			if (_firstFeature == null || !KeyboardUtils.IsShiftDown())
			{
				// If shift is pressed and the first has already been selected, do not overwrite it
				_firstFeature = selectedFeatures[0];
			}

			await StartSecondPhaseAsync();
		}

		protected override Task<bool> IsInSelectionPhaseCoreAsync(bool shiftDown)
		{
			bool isInSelectionPhase = _firstFeature == null || shiftDown;

			return Task.FromResult(isInSelectionPhase);
		}

		private async Task StartSecondPhaseAsync()
		{
			SelectionCursors = SecondPhaseCursors;

			await QueuedTask.Run(() => { SetupSketch(); });
			await QueuedTask.Run(async () => { await ResetSelectionSketchTypeAsync(); });
		}


		protected override void LogUsingCurrentSelection()
		{
			//_msg.Info(LocalizableStrings.MergeFeaturesTool_LogUsingCurrentSelection);
			_msg.InfoFormat(
				"Select the second feature for split and merge with the first.{0}" +
				"- Press A to use alternate Split And Merge logic.{0}" +
				"- Hit [ESC] to reselect the first feature.",
				Environment.NewLine);

		}

		protected override void LogPromptForSelection()
		{
			string message;

			//string survivorText = _mergeAction == MergeAction.MergeIntoFirstFeature
			//		? LocalizableStrings.MergeFeaturesTool_SurvivorFirstObject
			//		: LocalizableStrings.MergeFeaturesTool_SurvivorLargerObject;
			////string survivorText =
			////	_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.FirstObject
			////		? LocalizableStrings.MergeFeaturesTool_SurvivorFirstObject
			////		: LocalizableStrings.MergeFeaturesTool_SurvivorLargerObject;

			//int selectedCount = ActiveMapView.Map.SelectionCount;

			//if (selectedCount > 1)
			//{
			//	message =
			//		"Press Enter to merge all selected features. The largest feature will survive after the merge." +
			//		Environment.NewLine +
			//		$"Alternatively, select a new first feature to merge{survivorText}";
			//}
			//else
			//{
			//	message = string.Format(
			//		LocalizableStrings.MergeFeaturesTool_LogPromptForSelection,
			//		survivorText);
			//}

			message = string.Format("Select the first, editable, feature for split and merge.");
			_msg.Info(message);
		}

		protected override async Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry, CancelableProgressor progressor)
		{
			bool isInFirstPhase = await IsInSelectionPhaseCoreAsync(KeyboardUtils.IsShiftDown());

			Assert.False(isInFirstPhase, "Unexpected tool phase");

			try
			{
				IsMerging = true;

				await PickLastFeatureAndMerge(sketchGeometry, progressor);
			}
			finally
			{
				IsMerging = false;
			}

			return await base.OnSketchCompleteCoreAsync(sketchGeometry, progressor);
		}

		protected override async Task<bool> OnMapSelectionChangedCoreAsync(
			MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug(() => nameof(OnMapSelectionChangedCoreAsync));

			if (ActiveMapView == null)
			{
				return false;
			}

			if (IsMerging)
			{
				// While storing the selection is changed and managed by the respective method.
				return false;
			}

			if (args.Selection.IsEmpty)
			{
				await StartSelectionPhaseAsync();

				return true;
			}

			Dictionary<BasicFeatureLayer, List<long>> selectionByLayer =
				SelectionUtils.GetSelection<BasicFeatureLayer>(args.Selection);

			// TODO: Try to make CanUseSelection run outside QueuedTask.Run (as far as possible)
			bool canUseSelection = await QueuedTask.Run(() => CanUseSelection(selectionByLayer));

			if (!canUseSelection)
			{
				//_firstFeature = null;
				await StartSelectionPhaseAsync();
			}
			else
			{
				Dictionary<MapMember, List<long>> mapMemberSelection =
					selectionByLayer.ToDictionary(MapMember (kvp) => kvp.Key, kvp => kvp.Value);

				await QueuedTask.Run(() =>
				{
					List<Feature> selection =
						GetDistinctApplicableSelectedFeatures(mapMemberSelection, UnJoinedSelection)
							.ToList();

					_firstFeature = selection[0];
				});
			}

			return true;
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polygon ||
			       geometryType == GeometryType.Polyline ||
			       //geometryType == GeometryType.Multipatch ||
			       geometryType == GeometryType.Multipoint;
		}


		#endregion MapToolBase and OneClickToolBase overrides


		#region Non-public members

		private async Task<bool> PickLastFeatureAndMerge(Geometry sketchGeometry,
		                                                 CancelableProgressor progressor)
		{
			//_secondFeature = await QueuedTask.Run(() => PickLastFeature(sketchGeometry, progressor));
			IPickableFeatureItem selectedItem = await QueuedTask.Run(() => PickLastFeature(sketchGeometry, progressor));
			_secondFeature = selectedItem?.Feature;

			AlternateSplitAndMerge = PressedKeys.Contains(_alternateSplitAndMergeKey);
			PressedKeys.Remove(_alternateSplitAndMergeKey);

			if (_secondFeature == null)
			{
				return false;
			}

			//SecondFeatureIsEditable = selectedItem.Layer.CanEditData();
			SecondFeatureIsEditable = selectedItem.Layer.IsEditable;

			// Get all features in a single QueuedTask call to ensure thread consistency
			Feature survivingFeature = await QueuedTask.Run(async () =>
			{
				Feature updatedFeature = null;
				Polygon perimeter = GetPerimeter();
				if (perimeter == null)
				{
					_msg.Error("Unable to merge: No Perimeter avaiavle.");
					return updatedFeature;
				}

				bool requiresSplitAndMerge = RequiresSplitAndMerge(_firstFeature, _secondFeature, perimeter);

				Geometry firstGeometry = _firstFeature.GetShape();
				Geometry secondGeometry = _secondFeature.GetShape();

				//TODO also display MergeDiscontinuities
				var splitAndMerge = new SplitAndMergeGeometries(secondGeometry, firstGeometry, perimeter);
				//var splitAndMerge = new SplitAndMergeGeometries(firstGeometry, secondGeometry, perimeter);
				Geometry _mergeResult = splitAndMerge.MergeGeometriesOnPerimeter(AlternateSplitAndMerge);

				//show Overlay
				updatedFeature = ExecuteMerge(_mergeResult);

				return updatedFeature;


				//MergerBase merger = GetMerger();

				//List<Feature> featuresToMerge =
				//	GetApplicableSelectedFeatures(ActiveMapView).ToList();

				//featuresToMerge.Add(lastFeature);

				//if (!EvaluateCanMerge(merger, featuresToMerge))
				//{
				//	return null;
				//}

				//Feature updateFeature;

				//if (MergeOperationSurvivor == MergeOperationSurvivor.FirstObject)
				//{
				//	updateFeature = _firstFeature;
				//}
				//else
				//{
				//	updateFeature = DetermineSurvivingLargestFeature(featuresToMerge);
				//}

				//return await merger.MergeFeatures(featuresToMerge, updateFeature);
			});

			//if (survivingFeature != null)
			{
				//await SelectResultAndSetupNextStep(survivingFeature);
				await QueuedTask.Run(async () =>
				{
					await ClearSelectionAsync();
					//await StartSelectionPhaseAsync();
				});

			}

			return true;
		}

		private static bool RequiresSplitAndMerge([NotNull] Feature incrementFeature,
		                                          [NotNull] Feature cartoFeature,
		                                          [NotNull] Geometry perimeter)
		{
			Geometry incrementGeometry = incrementFeature.GetShape();
			Geometry cartoGeometry = cartoFeature.GetShape();
			Assert.NotNull(incrementGeometry, "Increment feature shape is null");
			Assert.NotNull(cartoGeometry, "Current feature shape is null");

			Geometry additions = GeometryEngine.Instance.Difference(incrementGeometry, cartoGeometry);
			Geometry removals = GeometryEngine.Instance.Difference(cartoGeometry, incrementGeometry);

			if (additions == null || additions.IsEmpty ||
			    removals == null || removals.IsEmpty)
			{
				return false;
			}

			if (GeometryEngine.Instance.Contains(perimeter, additions) &&
			    GeometryEngine.Instance.Contains(perimeter, removals))
			{
				return false;
			}

			return true;
		}

		private Feature ExecuteMerge(Geometry mergeResult)
		{

			QueuedTaskUtils.Run(() => _feedback?.UpdatePreview(
				                          mergeResult));

			if (_mergeAction == MergeAction.MergeIntoFirstFeature)
			{
				//QueuedTaskUtils.Run(() => _feedback?.UpdatePreviewFeatureToBeUpdated(
				//	                    _firstFeature));
				//SaveMergedFeatures(_firstFeature, _secondFeature, mergeResult,
				//                   SecondFeatureIsEditable);
			}
			else if (SecondFeatureIsEditable)
			{
				//QueuedTaskUtils.Run(() => _feedback?.UpdatePreviewFeatureToBeUpdated(
				//	                    _secondFeature));
				//SaveMergedFeatures(_secondFeature, _firstFeature, mergeResult, true);
			}
			else
			{
				_msg.Warn("Unable to merge into second feature: It is not editable.");
			}

			return null;
		}

		private async Task SelectResultAndSetupNextStep(Feature survivingFeature)
		{
			if (survivingFeature != null)
			{
				await QueuedTask.Run(() =>
				{
					long selectedCount = ToolUtils.SelectNewFeatures(
						new[] { survivingFeature }, ActiveMapView, true);

					if (selectedCount == 0)
					{
						// Larger feature wins but the smaller was selected -> Just try any matching layer
						SelectionUtils.SelectFeature(ActiveMapView, survivingFeature);
					}
				});
			}

			// NOTE: Even though SetupNextStepAfterMerge can theoretically run without a QueuedTask,
			//       we need to ensure it is called sequentially (i.e. queued) after the above method,
			//       because the SelectionChanged events are only fired at the end of the queued task.
			//       Within the SelectionChanged events, the _firstFeature can be changed which would
			//       defy setting the first feature in the following method to null in case
			//       UseMergeResultForNextMerge is false.
			await QueuedTask.Run(() => SetupNextStepAfterMerge(survivingFeature));
		}


		private async Task SetupNextStepAfterMerge(Feature survivingFeature)
		{
			//if (_mergeToolOptions.UseMergeResultForNextMerge)
			//{
			//	_firstFeature = survivingFeature;
			//	LogUsingCurrentSelection();
			//	await StartSecondPhaseAsync();
			//}
			//else
			{
				LogPromptForSelection();
				await QueuedTask.Run(async () =>
				{
					await ClearSelectionAsync();
					await StartSelectionPhaseAsync();
				});
			}
		}

		protected bool IsPickableTargetFeature([NotNull] Feature feature)
		{
			// assumes that editability is already checked at layer level

			if (_mergeAction == MergeAction.MergeIntoFirstFeature)
			//if (_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.FirstObject)
			{
				if (_firstFeature == null)
				{
					return true;
				}

				return _firstFeature.GetObjectID() != feature.GetObjectID() ||
				       _firstFeature.GetTable().GetID() != feature.GetTable().GetID();
			}

			// MergeSurvivor == LargerObject
			IList<Feature> selectedFeatures = GetApplicableSelectedFeatures(ActiveMapView).ToList();

			bool alreadySelected = selectedFeatures.Any(selectedFeature =>
				                                            selectedFeature.GetObjectID() ==
				                                            feature.GetObjectID() &&
				                                            selectedFeature.GetTable().GetID() ==
				                                            feature.GetTable().GetID());

			return !alreadySelected;
		}


		[CanBeNull]
		private async Task<IPickableFeatureItem> PickLastFeature(Geometry sketchGeometry,
		                                                         CancelableProgressor cancellabelProgressor)
		{
			IPickerPrecedence precedence = await CreatePickerPrecedenceAsync(sketchGeometry);

			var featureFinder = new FeatureFinder(ActiveMapView,
			                                      TargetFeatureSelection.VisibleFeatures)// .VisibleSelectableEditableFeatures)
			                    {
				                    ReturnUnJoinedFeatures = true
			                    };

			List<FeatureSelectionBase> selectionByLayer =
				featureFinder.FindFeaturesByFeatureClass(precedence.GetSelectionGeometry(),
				                                         CanLayerContainSecondFeature,
				                                         IsPickableTargetFeature).ToList();

			if (selectionByLayer.Count == 0)
			{
				_msg.Info("No valid second feature found in any layer.");
				return null;
			}

			if (cancellabelProgressor?.CancellationToken.IsCancellationRequested == true)
			{
				return null;
			}

			IPickableFeatureItem selectedItem =
				await PickerUtils.PickSingleAsync(selectionByLayer, precedence);

			if (selectedItem == null)
			{
				_msg.Info("No valid second has been selected.");
				return null;
			}

			//return selectedItem.Feature;
			return selectedItem;
		}

		private bool CanLayerContainSecondFeature(BasicFeatureLayer layer)
		{
			Assert.NotNull(_firstFeature, "No first feature selected");

			GeometryType geometryType = _firstFeature.GetShape().GeometryType;

			FeatureClass featureClass = layer.GetFeatureClass();

			return featureClass != null && featureClass.GetShapeType() == geometryType;
		}


		#endregion Non-public members
	}
}
