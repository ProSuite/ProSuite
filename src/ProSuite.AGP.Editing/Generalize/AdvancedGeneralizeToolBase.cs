using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Generalize;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;
using Resources = ProSuite.AGP.Editing.Properties.Resources;

namespace ProSuite.AGP.Editing.Generalize
{
	public abstract class AdvancedGeneralizeToolBase : TwoPhaseEditToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private AdvancedGeneralizeToolOptions _generalizeToolOptions;
		private OverridableSettingsProvider<PartialAdvancedGeneralizeOptions> _settingsProvider;

		protected abstract IAdvancedGeneralizeService MicroserviceClient { get; }

		private GeneralizeResult _generalizeResult;
		private GeneralizeFeedback _feedback;

		protected AdvancedGeneralizeToolBase()
		{
			GeomIsSimpleAsFeature = false;
		}

		protected string OptionsFileName => "AdvancedGeneralizeToolOptions.xml";

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

		protected override void OnUpdateCore()
		{
			Enabled = MicroserviceClient != null;

			if (MicroserviceClient == null)
				DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
		}

		protected override SelectionCursors GetSelectionCursors()
		{
			return SelectionCursors.CreateArrowCursors(Resources.AdvancedGeneralizeOverlay);
		}

		protected override SelectionCursors GetSecondPhaseCursors()
		{
			return SelectionCursors.CreateCrossCursors(Resources.AdvancedGeneralizeOverlay);
		}

		protected override Task OnToolActivatingCoreAsync()
		{
			_generalizeToolOptions = InitializeOptions();

			_feedback = new GeneralizeFeedback();

			return base.OnToolActivatingCoreAsync();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_settingsProvider?.StoreLocalConfiguration(_generalizeToolOptions.LocalOptions);

			_feedback?.DisposeOverlays();
			_feedback = null;

			HideOptionsPane();
		}

		protected override void LogPromptForSelection()
		{
			_msg.Info(
				$"Select one or more line or polygon features to be generalized. " +
				$"{Environment.NewLine}- Press and hold SHIFT to add or remove features from the existing selection." +
				$"{Environment.NewLine}- Press and hold P to draw a polygon that completely contains the features to be selected. " +
				$"Finish the polygon with double-click.");
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polyline || geometryType == GeometryType.Polygon;
		}

		protected override void CalculateDerivedGeometries(IList<Feature> selectedFeatures,
		                                                   CancelableProgressor progressor)
		{
			IList<Feature> intersectingFeatures = GetTargetFeatures(progressor);

			if (progressor?.CancellationToken.IsCancellationRequested == true)
			{
				_msg.Warn("Calculation of removable segments was cancelled.");
				return;
			}

			_generalizeResult =
				CalculateRemovableSegments(selectedFeatures, intersectingFeatures,
				                           _generalizeToolOptions, progressor);

			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of removable segments was cancelled.");
				return;
			}

			_feedback.Update(_generalizeResult, selectedFeatures);
		}

		protected override bool CanUseDerivedGeometries()
		{
			return _generalizeResult != null && _generalizeResult.ResultsByFeature.Count > 0;
		}

		protected override async Task<bool> SelectAndProcessDerivedGeometry(
			Dictionary<MapMember, List<long>> selection,
			Geometry sketch,
			CancelableProgressor progressor)
		{
			Assert.NotNull(_generalizeResult);

			List<GeneralizedFeature> shortSegmentsToApply =
				SelectRemovableSegmentsToApply(_generalizeResult, sketch);

			if (shortSegmentsToApply.Count == 0)
			{
				return false;
			}

			MapView activeMapView = MapView.Active;

			var distinctSelectionByFeatureClass =
				MapUtils.GetDistinctSelectionByTable(selection)
				        .ToDictionary(kvp => (FeatureClass) kvp.Key,
				                      kvp => kvp.Value);

			var selectedFeatures = MapUtils.GetFeatures(
				distinctSelectionByFeatureClass, true, activeMapView.Map.SpatialReference).ToList();

			AdvancedGeneralizeToolOptions generalizeOptions = _generalizeToolOptions;

			double? weedTolerance =
				generalizeOptions.Weed ? generalizeOptions.WeedTolerance : null;
			bool weedNonLinearSegments = generalizeOptions.WeedNonLinearSegments;

			double? minimumSegmentLength =
				generalizeOptions.EnforceMinimumSegmentLength
					? generalizeOptions.MinimumSegmentLength
					: null;
			bool use2DLength = generalizeOptions.Only2D;

			Geometry perimeter = GetPerimeter(generalizeOptions);

			IList<ResultFeature> result =
				MicroserviceClient.ApplySegmentRemoval(
					selectedFeatures, shortSegmentsToApply, weedTolerance, weedNonLinearSegments,
					minimumSegmentLength, use2DLength, perimeter,
					progressor?.CancellationToken ?? new CancellationTokenSource().Token);

			var updates = new Dictionary<Feature, Geometry>();

			if (result == null)
			{
				return false;
			}

			HashSet<long> editableClassHandles = ToolUtils.GetEditableClassHandles(activeMapView);

			foreach (ResultFeature resultFeature in result)
			{
				Feature originalFeature = resultFeature.OriginalFeature;
				Geometry newGeometry = resultFeature.NewGeometry;

				if (! IsStoreRequired(originalFeature, newGeometry, editableClassHandles))
				{
					continue;
				}

				Assert.AreEqual(RowChangeType.Update, resultFeature.ChangeType,
				                $"Unexpected type of change: {resultFeature.ChangeType}");

				updates.Add(originalFeature, newGeometry);
			}

			IEnumerable<Dataset> datasets =
				GdbPersistenceUtils.GetDatasetsNonEmpty(updates.Keys);

			bool saved = await GdbPersistenceUtils.ExecuteInTransactionAsync(
				             editContext =>
				             {
					             _msg.DebugFormat("Saving {0} updates...", updates.Count);

					             GdbPersistenceUtils.UpdateTx(editContext, updates);

					             return true;
				             },
				             "Advanced generalize: Remove segments", datasets);

			var currentSelection = GetApplicableSelectedFeatures(activeMapView).ToList();

			CalculateDerivedGeometries(currentSelection, progressor);

			return saved;
		}

		protected override void ResetDerivedGeometries()
		{
			_generalizeResult = null;
			_feedback.DisposeOverlays();
		}

		protected override void LogDerivedGeometriesCalculated(CancelableProgressor progressor)
		{
			if (_generalizeResult == null || ! _generalizeResult.HasRemovableSegments)
			{
				_msg.Info("No segments found that can be removed.");
				return;
			}

			if (_generalizeResult != null && _generalizeResult.HasRemovableSegments)
			{
				string infoMessage =
					string.Format(
						"Select an unnecessary vertex (red cross) or short segment (red line) to remove it by clicking it" +
						Environment.NewLine +
						"Alternatively drag a box or press [{0}] and draw a polygon to select vertices and segments completely within the box or polygon" +
						Environment.NewLine +
						"Press [ESC] to select different features to generalize.", Key.P);

				_msg.Info(infoMessage);
			}
		}

		private static bool IsStoreRequired(Feature originalFeature, Geometry updatedGeometry,
		                                    HashSet<long> editableClassHandles)
		{
			if (! GdbPersistenceUtils.CanChange(originalFeature,
			                                    editableClassHandles, out string warning))
			{
				_msg.DebugFormat("{0}: {1}",
				                 GdbObjectUtils.ToString(originalFeature),
				                 warning);
				return false;
			}

			Geometry originalGeometry = originalFeature.GetShape();

			if (originalGeometry != null &&
			    originalGeometry.IsEqual(updatedGeometry))
			{
				_msg.DebugFormat("The geometry of feature {0} is unchanged. It will not be stored",
				                 GdbObjectUtils.ToString(originalFeature));

				return false;
			}

			return true;
		}

		private AdvancedGeneralizeToolOptions InitializeOptions()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			// NOTE: by only reading the file locations we can save a couple of 100ms
			string currentCentralConfigDir = CentralConfigDir;
			string currentLocalConfigDir = LocalConfigDir;

			// For the time being, we always reload the options because they could have been updated in ArcMap
			_settingsProvider =
				new OverridableSettingsProvider<PartialAdvancedGeneralizeOptions>(
					currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

			PartialAdvancedGeneralizeOptions localConfiguration, centralConfiguration;

			_settingsProvider.GetConfigurations(out localConfiguration,
			                                    out centralConfiguration);

			var result =
				new AdvancedGeneralizeToolOptions(centralConfiguration, localConfiguration);

			result.PropertyChanged -= OptionsPropertyChanged;
			result.PropertyChanged += OptionsPropertyChanged;

			_msg.DebugStopTiming(watch, "Advanced Generalize Tool Options validated / initialized");

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
				_msg.Error($"Error re-calculating removable segments : {e.Message}", e);
			}
		}

		[CanBeNull]
		private IList<Feature> GetTargetFeatures(CancelableProgressor progressor)
		{
			if (! _generalizeToolOptions.ProtectTopologicalVertices)
			{
				return null;
			}

			TargetFeatureSelection targetFeatureSelection =
				_generalizeToolOptions.VertexProtectingFeatureSelection;

			Dictionary<MapMember, List<long>> selection =
				SelectionUtils.GetSelection(ActiveMapView.Map);

			IList<Feature> intersectingFeatures =
				ToolUtils.GetIntersectingFeatures(selection, MapView.Active, targetFeatureSelection,
				                                  0, GetTargetFeatureClassPredicate(), progressor);
			return intersectingFeatures;
		}

		protected virtual Predicate<FeatureClass> GetTargetFeatureClassPredicate()
		{
			return null;
		}

		private GeneralizeResult CalculateRemovableSegments(
			[NotNull] IList<Feature> selectedFeatures,
			[CanBeNull] IList<Feature> intersectingFeatures,
			AdvancedGeneralizeToolOptions generalizeOptions,
			CancelableProgressor progressor)
		{
			GeneralizeResult result;

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

			_msg.DebugFormat("Calculating removable segments with the following options: {0}",
			                 generalizeOptions);

			if (MicroserviceClient != null)
			{
				double? weedTolerance =
					generalizeOptions.Weed ? generalizeOptions.WeedTolerance : null;
				bool weedNonLinearSegments = generalizeOptions.WeedNonLinearSegments;

				double? minimumSegmentLength =
					generalizeOptions.EnforceMinimumSegmentLength
						? generalizeOptions.MinimumSegmentLength
						: null;
				bool use2DLength = generalizeOptions.Only2D;

				bool protectVerticesInSameClassOnly =
					generalizeOptions.VertexProtectingFeatureSelection ==
					TargetFeatureSelection.SameClass;

				Geometry perimeter = GetPerimeter(generalizeOptions);

				result = MicroserviceClient.CalculateRemovableSegments(
					selectedFeatures, intersectingFeatures, protectVerticesInSameClassOnly,
					weedTolerance, weedNonLinearSegments, minimumSegmentLength, use2DLength,
					perimeter, cancellationToken);
			}
			else
			{
				throw new InvalidConfigurationException("Microservice has not been started.");
			}

			return result;
		}

		private static Geometry GetPerimeter(AdvancedGeneralizeToolOptions generalizeOptions)
		{
			// TODO: Intersect with work perimeter
			Geometry perimeter = generalizeOptions.LimitToVisibleExtent
				                     ? MapView.Active.Extent
				                     : null;
			return perimeter;
		}

		private List<GeneralizedFeature> SelectRemovableSegmentsToApply(
			[CanBeNull] GeneralizeResult generalizeResult,
			[NotNull] Geometry sketch)
		{
			var result = new List<GeneralizedFeature>();

			if (generalizeResult == null)
			{
				return result;
			}

			sketch = ToolUtils.SketchToSearchGeometry(sketch, GetSelectionTolerancePixels(),
			                                          out bool singlePick);

			foreach (GeneralizedFeature generalizedFeature in generalizeResult.ResultsByFeature)
			{
				GeneralizedFeature selectedPointsByFeature =
					new GeneralizedFeature(generalizedFeature.Feature)
					{
						ProtectedPoints = generalizedFeature.ProtectedPoints
					};

				Multipoint deletablePoints = generalizedFeature.DeletablePoints;

				if (deletablePoints != null)
				{
					var selectedPoints = new List<MapPoint>();
					foreach (MapPoint point in deletablePoints.Points)
					{
						if (ToolUtils.IsSelected(sketch, point, singlePick))
						{
							selectedPoints.Add(point);
						}
					}

					if (selectedPoints.Count > 0)
					{
						selectedPointsByFeature.DeletablePoints =
							MultipointBuilderEx.CreateMultipoint(
								selectedPoints, deletablePoints.SpatialReference);
					}
				}

				foreach (SegmentInfo segmentInfo in generalizedFeature.RemovableSegments)
				{
					Segment segment = segmentInfo.Segment;

					// It would probably be more efficient to check manually (both end-points, envelope)
					// or get the sketch-intersecting segment indexes.
					Polyline polyline =
						PolylineBuilderEx.CreatePolyline(segment, segment.SpatialReference);

					if (ToolUtils.IsSelected(sketch, polyline, singlePick))
					{
						selectedPointsByFeature.RemovableSegments.Add(segmentInfo);
					}
				}

				if (selectedPointsByFeature.DeletablePoints != null ||
				    selectedPointsByFeature.RemovableSegments.Count > 0)
				{
					result.Add(selectedPointsByFeature);
				}
			}

			return result;
		}

		#region Tool Options Dockpane

		[CanBeNull]
		private DockPaneGeneralizeViewModelBase GetOptionsViewModel()
		{
			if (OptionsDockPaneID == null)
			{
				return null;
			}

			var viewModel =
				FrameworkApplication.DockPaneManager.Find(OptionsDockPaneID) as
					DockPaneGeneralizeViewModelBase;

			return Assert.NotNull(viewModel, "Options DockPane with ID '{0}' not found",
			                      OptionsDockPaneID);
		}

		protected override void ShowOptionsPane()
		{
			DockPaneGeneralizeViewModelBase viewModel = GetOptionsViewModel();

			Assert.NotNull(viewModel);

			viewModel.Options = _generalizeToolOptions;

			viewModel.Activate(true);
		}

		protected override void HideOptionsPane()
		{
			DockPaneGeneralizeViewModelBase viewModel = GetOptionsViewModel();

			viewModel?.Hide();
		}

		#endregion

	}
}
