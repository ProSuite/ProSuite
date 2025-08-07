using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.MergeFeatures
{
	public enum MergeAction
	{
		MergeWithLargestFeature,
		MergeWithClickedFeature
	}

	public abstract class MergeFeaturesToolBase : OneClickToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private MergeToolOptions _mergeToolOptions;
		private OverridableSettingsProvider<PartialMergeOptions> _settingsProvider;

		private const Key _immediateMergeKey = Key.Enter;
		private Feature _firstFeature;

		private SelectionCursors _secondPhaseCursors;

		public Feature ContextClickedFeature { get; set; }

		protected abstract ContextMenu GetContextMenu(Point screenLocation);

		protected MergeFeaturesToolBase()
		{
			IsSketchTool = true;

			GeomIsSimpleAsFeature = false;

			HandledKeys.Add(_immediateMergeKey);
		}

		protected virtual string OptionsFileName => "MergeToolOptions.xml";

		[CanBeNull]
		protected virtual string OptionsDockPaneID => null;

		[CanBeNull]
		protected virtual string CentralConfigDir => null;

		/// <summary>
		/// By default, the local configuration directory shall be in
		/// %APPDATA%\Roaming\<organization>\<product>\ToolDefaults.
		/// </summary>
		protected virtual string LocalConfigDir
			=> EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(
				AppDataFolder.Roaming, "ToolDefaults");

		protected MergeToolOptions MergeOptions => _mergeToolOptions;

		protected virtual MergeOperationSurvivor MergeOperationSurvivor =>
			_mergeToolOptions.MergeSurvivor;

		// First selected feature

		/// <summary>
		/// An optional merge condition evaluator that currently only results in warnings
		/// if some condition is violated.
		/// </summary>
		public IMergeConditionEvaluator MergeConditionEvaluator { get; protected set; }

		#region MapToolBase and OneClickToolBase overrides

		protected bool AllowMultiSelection =>
			_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.LargerObject;

		protected bool AllowSelectByPolygon =>
			_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.LargerObject;

		//ToDo?
		//protected bool IgnoreSelectionOutsideVisibleExtents => true;

		protected override SelectionCursors GetSelectionCursors()
		{
			return SelectionCursors.CreateArrowCursors(Resources.MergeFeaturesOverlay1);
		}

		private SelectionCursors GetSecondPhaseCursors()
		{
			return SelectionCursors.CreateArrowCursors(Resources.MergeFeaturesOverlay2);
		}

		protected override async Task HandleEscapeAsync()
		{
			_firstFeature = null;

			try
			{
				await QueuedTask.Run(async () =>
				{
					ClearSelection();

					LogPromptForSelection();

					await SetupSelectionSketchAsync();
				});
			}
			catch (Exception e)
			{
				ErrorHandler.HandleError(e, _msg);
			}
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

		protected override async Task ToggleSelectionSketchGeometryTypeAsync(
			SketchGeometryType toggleSketchType,
			SelectionCursors selectionCursors = null)
		{
			if (await IsInSelectionPhaseAsync())
			{
				await base.ToggleSelectionSketchGeometryTypeAsync(
					toggleSketchType, selectionCursors);
			}
			else
			{
				await base.ToggleSelectionSketchGeometryTypeAsync(
					toggleSketchType, _secondPhaseCursors);
			}
		}

		protected override async void OnToolMouseDown(MapViewMouseButtonEventArgs args)
		{
			if (args.ChangedButton == MouseButton.Right)
			{
				args.Handled = true;

				await DetectFeaturesAtRightClick(args.ClientPoint);

				Point screenPosition = ActiveMapView.ClientToScreen(args.ClientPoint);
				ShowContextMenu(screenPosition);
			}
		}

		private async Task DetectFeaturesAtRightClick(Point clientPoint)
		{
			_msg.Debug("Detecting features at right-click point...");

			ContextClickedFeature = null;

			await QueuedTask.Run(() =>
			{
				try
				{
					IList<Feature> selectedFeatures =
						GetApplicableSelectedFeatures(ActiveMapView).ToList();

					if (selectedFeatures.Count < 2)
					{
						_msg.Debug($"Not enough features selected: {selectedFeatures.Count}");
						return;
					}

					MapPoint mapPoint = ActiveMapView.ClientToMap(clientPoint);

					Geometry searchGeometry =
						GeometryEngine.Instance.Buffer(mapPoint, GetSelectionTolerancePixels());

					IList<Feature> featuresAtClick = selectedFeatures
					                                 .Where(f => GeometryEngine.Instance.Intersects(
						                                        f.GetShape(), searchGeometry))
					                                 .ToList();

					if (featuresAtClick.Count == 1)
					{
						ContextClickedFeature = featuresAtClick[0];
						_msg.Debug(
							$"Found feature at click point: {ContextClickedFeature.GetObjectID()}");
					}
					else if (featuresAtClick.Count > 1)
					{
						_msg.Debug("Multiple features found at click point");
					}
					else
					{
						_msg.Debug("No features found at click point");
					}
				}
				catch (Exception ex)
				{
					_msg.Warn("Error detecting features at click point", ex);
				}
			});
		}

		protected override async Task HandleKeyDownAsync(MapViewKeyEventArgs args)
		{
			try
			{
				await base.HandleKeyDownAsync(args);

				if (args.Key == _immediateMergeKey)
				{
					Feature survivingFeature = null;

					await QueuedTask.Run(async () =>
					{
						try
						{
							survivingFeature = await MergeFeaturesUsingLargestFeatureCoreAsync();
						}
						catch (Exception e)
						{
							_msg.Warn("Error merging immediatly", e);
						}
					});

					if (survivingFeature != null)
					{
						await SelectResultAndSetupNextStep(survivingFeature);
					}
				}
			}
			catch (Exception e)
			{
				_msg.Warn("Error handling key press", e);
			}
		}

		protected override Task OnToolActivatingCoreAsync()
		{
			_mergeToolOptions = InitializeOptions();

			_secondPhaseCursors = GetSecondPhaseCursors();

			return base.OnToolActivatingCoreAsync();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_settingsProvider?.StoreLocalConfiguration(_mergeToolOptions.LocalOptions);

			_firstFeature = null;

			HideOptionsPane();
		}

		protected override async Task AfterSelectionAsync(IList<Feature> selectedFeatures,
		                                                  CancelableProgressor progressor)
		{
			_firstFeature = selectedFeatures[0];

			await StartSecondPhaseAsync();
		}

		protected override Task<bool> IsInSelectionPhaseCoreAsync(bool shiftDown)
		{
			bool isInSelectionPhase = _firstFeature == null || shiftDown;

			return Task.FromResult(isInSelectionPhase);
		}

		private async Task StartSecondPhaseAsync()
		{
			await QueuedTask.Run(() => { SetupSketch(); });
			await QueuedTask.Run(async () =>
			{
				await ResetSelectionSketchTypeAsync(_secondPhaseCursors);
			});
		}

		protected override void LogUsingCurrentSelection()
		{
			_msg.Info(LocalizableStrings.MergeFeaturesTool_LogUsingCurrentSelection);
		}

		protected override void LogPromptForSelection()
		{
			string message;

			string survivorText =
				_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.FirstObject
					? LocalizableStrings.MergeFeaturesTool_SurvivorFirstObject
					: LocalizableStrings.MergeFeaturesTool_SurvivorLargerObject;

			int selectedCount = ActiveMapView.Map.SelectionCount;

			if (selectedCount > 1)
			{
				message =
					"Press Enter to merge all selected features. The largest feature will survive after the merge." +
					Environment.NewLine +
					$"Alternatively, select a new first feature to merge{survivorText}";
			}
			else
			{
				message = string.Format(
					LocalizableStrings.MergeFeaturesTool_LogPromptForSelection,
					survivorText);
			}

			_msg.Info(message);
		}

		protected override async Task<bool> OnSketchCompleteCoreAsync(
			Geometry sketchGeometry, CancelableProgressor progressor)
		{
			bool isInFirstPhase = await IsInSelectionPhaseCoreAsync(KeyboardUtils.IsShiftDown());

			Assert.False(isInFirstPhase, "Unexpected tool phase");

			//Hopefully we can delete this..... 
			// if (isInFirstPhase)
			// {
			// 	return await base.OnSketchCompleteCoreAsync(sketchGeometry, progressor);
			// }
			//
			// if (!await CanStillUseSelection(sketchGeometry, progressor))
			// {
			// 	_msg.InfoFormat(
			// 		"The current selection cannot be used. Re-selecting the first feature...");
			// 	return await base.OnSketchCompleteCoreAsync(sketchGeometry, progressor);
			// }
			// else
			{
				await PickSecondFeatureAndMerge(sketchGeometry, progressor);
			}

			return await base.OnSketchCompleteCoreAsync(sketchGeometry, progressor);
		}

		protected override async Task<bool> OnMapSelectionChangedCoreAsync(
			MapSelectionChangedEventArgs args)
		{
			_msg.VerboseDebug(() => "OnMapSelectionChangedCoreAsync");

			if (ActiveMapView == null)
			{
				return false;
			}

			if (! CanUseSelection(ActiveMapView))
			{
				_firstFeature = null;
				await SetupSelectionSketchAsync();
			}
			else
			{
				Dictionary<MapMember, List<long>> selectionByLayer =
					SelectionUtils.GetSelection(ActiveMapView.Map);

				List<Feature> selection =
					GetDistinctApplicableSelectedFeatures(selectionByLayer, UnJoinedSelection)
						.ToList();

				_firstFeature = selection[0];
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
			       geometryType == GeometryType.Multipatch ||
			       geometryType == GeometryType.Multipoint;
		}

		#endregion

		#region Tool Options DockPane

		[CanBeNull]
		private DockPaneMergeFeaturesViewModelBase GetDockPaneMergeFeaturesViewModel()
		{
			if (OptionsDockPaneID == null)
			{
				return null;
			}

			var viewModel =
				FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
					DockPaneMergeFeaturesViewModelBase;

			return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
			                      OptionsDockPaneID);
		}

		protected override void ShowOptionsPane()
		{
			var viewModel = GetDockPaneMergeFeaturesViewModel();

			if (viewModel == null)
			{
				return;
			}

			viewModel.Options = _mergeToolOptions;

			viewModel.Activate(true);
		}

		protected override void HideOptionsPane()
		{
			var viewModel = GetDockPaneMergeFeaturesViewModel();
			viewModel?.Hide();
		}

		#endregion

		#region Context menu

		private void ShowContextMenu(Point screenLocation)
		{
			ContextMenu contextMenu = GetContextMenu(screenLocation);

			if (contextMenu is not null)
			{
				contextMenu.IsOpen = true;
			}
		}

		#endregion

		#region Non-public members

		[NotNull]
		protected abstract MergerBase GetMerger();

		[CanBeNull]
		protected virtual Type MergeWithSelectedAsPrimaryCmd()
		{
			return null;
		}

		[CanBeNull]
		protected virtual Type MergeWithLargestAsPrimaryCmd()
		{
			return null;
		}

		private MergeToolOptions InitializeOptions()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			// NOTE: by only reading the file locations we can save a couple of 100ms
			string currentCentralConfigDir = CentralConfigDir;
			string currentLocalConfigDir = LocalConfigDir;

			_settingsProvider =
				new OverridableSettingsProvider<PartialMergeOptions>(
					currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

			PartialMergeOptions localConfiguration, centralConfiguration;

			_settingsProvider.GetConfigurations(out localConfiguration,
			                                    out centralConfiguration);

			var result =
				new MergeToolOptions(centralConfiguration, localConfiguration);

			result.PropertyChanged -= OptionsPropertyChanged;
			result.PropertyChanged += OptionsPropertyChanged;

			_msg.DebugStopTiming(watch, "Merge Features Options validated / initialized");

			string optionsMessage = result.GetLocalOverridesMessage();

			if (! string.IsNullOrEmpty(optionsMessage))
			{
				_msg.Info(optionsMessage);
			}

			return result;
		}

		private void OptionsPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			try
			{
				QueuedTaskUtils.Run(() => ProcessSelectionAsync());
			}
			catch (Exception e)
			{
				_msg.Error($"Error re-calculating merge features : {e.Message}", e);
			}
		}

		//Hopefully we can delete this.....
		//private async Task<bool> CanStillUseSelection(Geometry sketchGeometry,
		//                                              CancelableProgressor progressor)
		//{
		//	return await QueuedTask.Run(() =>
		//	{
		//return true;
		//List<Feature> selectedFeatures =
		//	GetApplicableSelectedFeatures(ActiveMapView).ToList();
		////TODO: Kratzt mich diese Unterwellelung?
		//if (selectedFeatures == null || selectedFeatures.Count != 1)
		//{
		//	return false;
		//}

		//Feature selectedFeature = selectedFeatures[0];
		//return selectedFeature.GetObjectID() == sketchGeometry.GetObjectID() &&
		//	   selectedFeature.GetTable().GetID() == sketchGeometry.GetTable().GetID();
		//	});
		//}

		///// <summary>
		///// Check if the given feature belongs to the originclass of the given
		///// relationship class.
		///// If the class of the feature and the originfeatureclass do not share
		///// the same name, then false is returned, there is no check, if the class
		///// of the given features does belong the relationshipClass
		///// </summary>
		///// <param name="feature">Feature to check</param>
		///// <param name="relationshipClass">RelationshipClass used to get the originClass</param>
		///// <returns>TRUE if the feature is from the originClass, FALSE otherwise</returns>
		//private static bool IsFeatureFromOriginClass(
		//	[NotNull] Feature feature,
		//	[NotNull] RelationshipClass relationshipClass)
		//{
		//	string featureClassName = ((IDataset)feature.Class).Name;
		//	string originClassName = ((IDataset)relationshipClass.OriginClass).Name;

		//	return featureClassName.Equals(originClassName);
		//}

		///// <summary>
		///// Gets the list of relationships where the given object is one part of.
		///// </summary>
		///// <param name="gdbObject">Feature that must belong to the returned relationship</param>
		///// <param name="relationshipClass">RelationshipClass that holds the information
		///// about the relationships with the given object</param>
		///// <returns>List with IRelationship instances, could be empty</returns>
		//[NotNull]
		//private static IList<Relationship> GetRelationships(
		//	[NotNull] Object gdbObject,
		//	[NotNull] RelationshipClass relationshipClass)
		//{
		//	var result = new List<Relationship>();

		//	//IEnumRelationship relations = relationshipClass.GetRelationshipsForObject(gdbObject);

		//	if (relations != null)
		//	{
		//		relations.Reset();
		//		Relationship relationship;
		//		while ((relationship = relations.Next()) != null)
		//		{
		//			result.Add(relationship);
		//		}
		//	}

		//	return result;
		//}

		private async Task<bool> PickSecondFeatureAndMerge(Geometry sketchGeometry,
		                                                   CancelableProgressor progressor)
		{
			Feature secondFeature =
				await QueuedTask.Run(() => PickSecondFeature(sketchGeometry, progressor));

			if (secondFeature == null)
			{
				return false;
			}

			MergerBase merger = await QueuedTask.Run(() => GetMerger());

			IList<Feature> features = new List<Feature> { _firstFeature, secondFeature };

			bool canMerge = await QueuedTask.Run(() => merger.CanMerge(features));

			if (! canMerge)
			{
				return false;
			}

			bool flipFeatures =
				await QueuedTask.Run(() => DetermineSecondFeatureIsUpdate(
					                     _firstFeature, secondFeature));

			Feature updateFeature = flipFeatures ? secondFeature : _firstFeature;

			Feature survivingFeature =
				await QueuedTask.Run(() => merger.MergeFeatures(
					                     new List<Feature> { _firstFeature, secondFeature },
					                     updateFeature));

			if (survivingFeature != null)
			{
				await SelectResultAndSetupNextStep(survivingFeature);
			}

			return true;
		}

		/// <summary>
		/// IMPORTANT: This method must not be called from within a QueuedTask!
		/// </summary>
		/// <param name="survivingFeature"></param>
		/// <returns></returns>
		private async Task SelectResultAndSetupNextStep(Feature survivingFeature)
		{
			await QueuedTask.Run(() => ToolUtils.SelectNewFeatures(
				                     new[] { survivingFeature }, MapView.Active, true));

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
			if (_mergeToolOptions.UseMergeResultForNextMerge)
			{
				_firstFeature = survivingFeature;
				LogUsingCurrentSelection();
				await StartSecondPhaseAsync();
			}
			else
			{
				_firstFeature = null;
				LogPromptForSelection();
				await QueuedTask.Run(async () => { await SetupSelectionSketchAsync(); });
			}
		}

		protected virtual bool IsPickableTargetFeature([NotNull] Feature feature)
		{
			// assumes that editability is already checked at layer level

			if (_firstFeature == null)
			{
				return true;
			}

			return _firstFeature.GetObjectID() != feature.GetObjectID() ||
			       _firstFeature.GetTable().GetID() != feature.GetTable().GetID();
		}

		[CanBeNull]
		private async Task<Feature> PickSecondFeature(Geometry sketchGeometry,
		                                              CancelableProgressor cancellabelProgressor)
		{
			IPickerPrecedence precedence = CreatePickerPrecedence(sketchGeometry);

			var featureFinder = new FeatureFinder(ActiveMapView,
			                                      TargetFeatureSelection
				                                      .VisibleSelectableEditableFeatures)
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

			return selectedItem.Feature;
		}

		private bool CanLayerContainSecondFeature(BasicFeatureLayer layer)
		{
			Assert.NotNull(_firstFeature, "No first feature selected");

			GeometryType geometryType = _firstFeature.GetShape().GeometryType;

			FeatureClass featureClass = layer.GetFeatureClass();

			return featureClass != null && featureClass.GetShapeType() == geometryType;
		}

		/// <summary>
		/// Determines whether the second feature is the update (and the first shall be deleted) or not.
		/// </summary>
		/// <param name="firstFeature"></param>
		/// <param name="secondFeature"></param>
		/// <returns></returns>
		private bool DetermineSecondFeatureIsUpdate(Feature firstFeature,
		                                            Feature secondFeature)
		{
			bool result;

			// TODO: Use MergeOperationSurvivor to allow subclasses to modify using modifier key...
			if (_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.FirstObject)
			{
				result = false;
			}
			else
			{
				Assert.True(_mergeToolOptions.MergeSurvivor == MergeOperationSurvivor.LargerObject,
				            "Unsupported MergeOperationSurvivor.");

				Geometry firstShape = firstFeature.GetShape();
				Geometry secondShape = secondFeature.GetShape();

				Geometry larger =
					GeometryUtils.GetLargestGeometry(new List<Geometry>
					                                 {
						                                 firstShape,
						                                 secondShape
					                                 });

				bool firstFeatureIsLarger = firstShape == larger;

				result = ! firstFeatureIsLarger;
			}

			return result;
		}

		#endregion

		// Add this method to check if a specific merge action can be executed
		public bool CanExecuteMergeAction(MergeAction action)
		{
			try
			{
				return QueuedTask.Run(() =>
				{
					IList<Feature> selectedFeatures =
						GetApplicableSelectedFeatures(ActiveMapView).ToList();

					MergerBase merger = GetMerger();

					bool canMerge = merger.CanMerge(selectedFeatures);

					switch (action)
					{
						case MergeAction.MergeWithLargestFeature:
							return canMerge;

						case MergeAction.MergeWithClickedFeature:
							return canMerge && ContextClickedFeature != null;

						default:
							throw new NotSupportedException($"Unsupported merge action: {action}");
					}
				}).Result;
			}
			catch (Exception ex)
			{
				_msg.Warn("Error checking if merge action can execute", ex);
				return false;
			}
		}

		// Add this method to execute a specific merge action
		public async Task ExecuteMergeActionAsync(MergeAction action)
		{
			switch (action)
			{
				case MergeAction.MergeWithLargestFeature:
					await MergeFeaturesUsingLargestFeatureAsync();
					break;

				case MergeAction.MergeWithClickedFeature:
					await MergeFeaturesUsingClickedFeatureAsync();
					break;

				default:
					throw new NotSupportedException($"Unsupported merge action: {action}");
			}
		}

		private async Task MergeFeaturesUsingLargestFeatureAsync()
		{
			try
			{
				Feature survivingFeature = null;

				await QueuedTask.Run(async () =>
				{
					survivingFeature =
						await MergeFeaturesUsingLargestFeatureCoreAsync();
				});

				if (survivingFeature != null)
				{
					await SelectResultAndSetupNextStep(survivingFeature);
				}
			}
			catch (Exception e)
			{
				_msg.Warn("Error merging features using largest feature", e);
			}
		}

		private async Task<Feature> MergeFeaturesUsingLargestFeatureCoreAsync()
		{
			IList<Feature> selectedFeatures =
				GetApplicableSelectedFeatures(ActiveMapView).ToList();

			MergerBase merger = GetMerger();

			if (! merger.CanMerge(selectedFeatures))
			{
				_msg.Info("The selected features cannot be merged.");
				return null;
			}

			Feature largestFeature = GeometryUtils.GetLargestFeature(selectedFeatures);
			Assert.NotNull(largestFeature, "No largest feature identified.");

			return await merger.MergeFeatures(selectedFeatures, largestFeature);
		}

		private async Task MergeFeaturesUsingClickedFeatureAsync()
		{
			try
			{
				Feature survivingFeature = null;

				await QueuedTask.Run(async () =>
				{
					if (ContextClickedFeature == null)
					{
						_msg.Info(
							"No feature was clicked. Please click on a feature to use as primary.");
						return;
					}

					IList<Feature> selectedFeatures =
						GetApplicableSelectedFeatures(ActiveMapView).ToList();

					MergerBase merger = GetMerger();

					if (! merger.CanMerge(selectedFeatures))
					{
						_msg.Info("The selected features cannot be merged.");
						return;
					}

					survivingFeature =
						await merger.MergeFeatures(selectedFeatures, ContextClickedFeature);

					ContextClickedFeature = null;
				});

				if (survivingFeature != null)
				{
					await SelectResultAndSetupNextStep(survivingFeature);
				}
			}
			catch (Exception e)
			{
				_msg.Warn("Error merging features using clicked feature", e);
			}
		}
	}
}
