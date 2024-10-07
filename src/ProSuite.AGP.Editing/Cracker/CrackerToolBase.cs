using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.Cracker
{
	public abstract class CrackerToolBase : TwoPhaseEditToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private CrackerToolOptions _crackerToolOptions;
		private OverridableSettingsProvider<PartialCrackerToolOptions> _settingsProvider;

		private CrackerResult _resultCrackPoints;
		private CrackerFeedback _feedback;
		private IList<Feature> _overlappingFeatures;

		protected CrackerToolBase()
		{
			GeomIsSimpleAsFeature = false;

			SelectionCursor = ToolUtils.GetCursor(Resources.CrackerToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.CrackerToolCursorShift);
			SecondPhaseCursor = ToolUtils.GetCursor(Resources.CrackerToolCursorProcess);
		}

		protected string OptionsFileName => "CrackerToolOptions.xml";

		[CanBeNull]
		protected virtual string CentralConfigDir => null;

		protected virtual string LocalConfigDir =>
			EnvironmentUtils.ConfigurationDirectoryProvider.GetDirectory(AppDataFolder.Roaming);

		protected abstract ICrackerService MicroserviceClient { get; }

		protected override void OnUpdateCore()
		{
			Enabled = MicroserviceClient != null;

			if (MicroserviceClient == null)
				DisabledTooltip = ToolUtils.GetDisabledReasonNoGeometryMicroservice();
		}

		protected override void OnToolActivatingCore()
		{
			InitializeOptions();

			_feedback = new CrackerFeedback();
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

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polyline ||
			       geometryType == GeometryType.Polygon ||
			       geometryType == GeometryType.Multipatch;
		}

		protected override void CalculateDerivedGeometries(IList<Feature> selectedFeatures,
		                                                   CancelableProgressor progressor)
		{
			IList<Feature> intersectingFeatures =
				GetIntersectingFeatures(selectedFeatures, progressor);

			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of crack points was cancelled.");
				return;
			}

			_resultCrackPoints =
				CalculateCrackPoints(selectedFeatures, intersectingFeatures, progressor);

			if (progressor != null && progressor.CancellationToken.IsCancellationRequested)
			{
				_msg.Warn("Calculation of crack points was cancelled.");
				return;
			}

			//// TODO: Options
			//bool insertVerticesInTarget = true;
			//_overlappingFeatures = insertVerticesInTarget
			//	                       ? intersectingFeatures
			//	                       : null;

			_feedback.Update(_resultCrackPoints);
		}

		protected override bool CanUseDerivedGeometries()
		{
			return _resultCrackPoints != null && _resultCrackPoints.ResultsByFeature.Count > 0;
		}

		protected override async Task<bool> SelectAndProcessDerivedGeometry(
			Dictionary<MapMember, List<long>> selection,
			Geometry sketch,
			CancelableProgressor progressor)
		{
			Assert.NotNull(_resultCrackPoints);

			CrackerResult crackPointsToApply = SelectCrackPointsToApply(_resultCrackPoints, sketch);

			if (! crackPointsToApply.HasCrackPoints)
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

			IList<Feature> intersectingFeatures =
				GetIntersectingFeatures(selectedFeatures, progressor);

			var result =
				MicroserviceClient.ApplyCrackPoints(
					selectedFeatures, crackPointsToApply, intersectingFeatures,
					_crackerToolOptions,
					progressor?.CancellationToken ?? new CancellationTokenSource().Token);

			var updates = new Dictionary<Feature, Geometry>();

			HashSet<long> editableClassHandles = ToolUtils.GetEditableClassHandles(activeMapView);

			foreach (ResultFeature resultFeature in result)
			{
				Feature originalFeature = resultFeature.OriginalFeature;
				Geometry updatedGeometry = resultFeature.NewGeometry;

				if (! IsStoreRequired(originalFeature, updatedGeometry, editableClassHandles))
				{
					continue;
				}

				updates.Add(originalFeature, updatedGeometry);
			}

			IEnumerable<Dataset> datasets =
				GdbPersistenceUtils.GetDatasetsNonEmpty(updates.Keys);

			bool saved = await GdbPersistenceUtils.ExecuteInTransactionAsync(
				             editContext =>
				             {
					             _msg.DebugFormat("Saving {0} updates...",
					                              updates.Count);

					             GdbPersistenceUtils.UpdateTx(editContext, updates);

					             return true;
				             },
				             "Crack feature(s)", datasets);

			var currentSelection = GetApplicableSelectedFeatures(activeMapView).ToList();

			CalculateDerivedGeometries(currentSelection, progressor);

			return saved;
		}

		protected override void ResetDerivedGeometries()
		{
			_resultCrackPoints = null;
			_feedback.DisposeOverlays();
		}

		protected override void LogDerivedGeometriesCalculated(CancelableProgressor progressor)
		{
			if (_resultCrackPoints == null || ! _resultCrackPoints.HasCrackPoints)
			{
				_msg.Info(
					"No intersections with other geometries found. Please select several features to calculate crack points.");
			}

			if (_resultCrackPoints != null && _resultCrackPoints.HasCrackPoints)
			{
				string msg = _resultCrackPoints.ResultsByFeature.Count == 1
					             ? "Select the crack points to apply."
					             : $"Crack points have been found in {_resultCrackPoints.ResultsByFeature.Count} features. Select one or more crack points. Draw a box to select targets completely within the box.";

				_msg.InfoFormat(LocalizableStrings.RemoveOverlapsTool_AfterSelection, msg);
			}
		}

		private CrackerResult CalculateCrackPoints(IList<Feature> selectedFeatures,
		                                           IList<Feature> intersectingFeatures,
		                                           CancelableProgressor progressor)
		{
			CrackerResult resultCrackPoints;

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
				resultCrackPoints =
					MicroserviceClient.CalculateCrackPoints(selectedFeatures, intersectingFeatures,
					                                        _crackerToolOptions, cancellationToken);
			}
			else
			{
				throw new InvalidConfigurationException("Microservice has not been started.");
			}

			return resultCrackPoints;
		}

		private CrackerResult SelectCrackPointsToApply(CrackerResult crackerResultPoints,
		                                               Geometry sketch)
		{
			CrackerResult result = new CrackerResult();

			if (crackerResultPoints == null)
			{
				return result;
			}

			sketch = ToolUtils.SketchToSearchGeometry(sketch, GetSelectionTolerancePixels(),
			                                          out bool singlePick);

			foreach (CrackedFeature crackedFeature in crackerResultPoints.ResultsByFeature)
			{
				CrackedFeature selectedPointsByFeature = new CrackedFeature(crackedFeature.Feature);

				foreach (CrackPoint crackPoint in crackedFeature.CrackPoints)
				{
					if (ToolUtils.IsSelected(sketch, crackPoint.Point, singlePick))
					{
						selectedPointsByFeature.CrackPoints.Add(crackPoint);
					}
				}

				if (selectedPointsByFeature.CrackPoints.Count > 0)
				{
					result.ResultsByFeature.Add(selectedPointsByFeature);
				}
			}

			return result;
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

		private CrackerToolOptions InitializeOptions()
		{
			Stopwatch watch = _msg.DebugStartTiming();

			// NOTE: by only reading the file locations we can save a couple of 100ms
			string currentCentralConfigDir = CentralConfigDir;
			string currentLocalConfigDir = LocalConfigDir;

			// For the time being, we always reload the options because they could have been updated in ArcMap
			_settingsProvider =
				new OverridableSettingsProvider<PartialCrackerToolOptions>(
					currentCentralConfigDir, currentLocalConfigDir, OptionsFileName);

			PartialCrackerToolOptions localConfiguration, centralConfiguration;

			_settingsProvider.GetConfigurations(out localConfiguration,
			                                    out centralConfiguration);

			_crackerToolOptions = new CrackerToolOptions(centralConfiguration,
			                                             localConfiguration);

			_msg.DebugStopTiming(watch, "Cracker Tool Options validated / initialized");

			string optionsMessage = _crackerToolOptions.GetLocalOverridesMessage();

			if (! string.IsNullOrEmpty(optionsMessage))
			{
				_msg.Info(optionsMessage);
			}

			return _crackerToolOptions;
		}

		#region Search target features

		[NotNull]
		private IList<Feature> GetIntersectingFeatures(
			[NotNull] ICollection<Feature> selectedFeatures,
			[CanBeNull] CancelableProgressor cancellabelProgressor)
		{
			Dictionary<MapMember, List<long>> selection =
				SelectionUtils.GetSelection(ActiveMapView.Map);

			Envelope inExtent = ActiveMapView.Extent;

			// todo daro To tool Options? See ChangeGeometryAlongToolBase.SelectTargetsAsync() as well.
			TargetFeatureSelection targetFeatureSelection =
				_crackerToolOptions.TargetFeatureSelection;

			var featureFinder = new FeatureFinder(ActiveMapView, targetFeatureSelection);

			// They might be stored (insert target vertices):
			featureFinder.ReturnUnJoinedFeatures = true;

			// Options which are not directly passed to the Microservice via _crackerToolOptions
			// Snap crack points within tolerance to target vertices
			if (_crackerToolOptions.SnapToTargetVertices)
			{
				featureFinder.ExtraSearchTolerance = _crackerToolOptions.SnapTolerance;
			}

			// Set the feature classes to ignore
			IEnumerable<FeatureSelectionBase> featureClassSelections =
				featureFinder.FindIntersectingFeaturesByFeatureClass(
					selection, CanOverlapLayer, inExtent, cancellabelProgressor);

			if (cancellabelProgressor != null &&
			    cancellabelProgressor.CancellationToken.IsCancellationRequested)
			{
				return new List<Feature>();
			}

			var foundFeatures = new List<Feature>();

			foreach (var classSelection in featureClassSelections)
			{
				using Table table = classSelection.Table;
				IEnumerable<Feature> features =
					GdbQueryUtils.GetFeatures(table, classSelection.GetOids(), null, false);

				foundFeatures.AddRange(features);
			}

			// Remove the selected features from the set of overlapping features.
			// This is also important to make sure the geometries don't get mixed up / reset 
			// by inserting target vertices
			foundFeatures.RemoveAll(
				f => selectedFeatures.Any(s => GdbObjectUtils.IsSameFeature(f, s)));

			return foundFeatures;
		}

		private bool CanOverlapLayer(Layer layer)
		{
			var featureLayer = layer as FeatureLayer;

			List<string>
				ignoredClasses = new List<string>(); // RemoveOverlapsOptions.IgnoreFeatureClasses;

			return CanOverlapGeometryType(featureLayer) &&
			       (ignoredClasses == null || ! IgnoreLayer(layer, ignoredClasses));
		}

		private static bool CanOverlapGeometryType([CanBeNull] FeatureLayer featureLayer)
		{
			if (featureLayer?.GetFeatureClass() == null)
			{
				return false;
			}

			esriGeometryType shapeType = featureLayer.ShapeType;

			return shapeType == esriGeometryType.esriGeometryPolygon ||
			       shapeType == esriGeometryType.esriGeometryPolyline ||
			       shapeType == esriGeometryType.esriGeometryMultiPatch;
		}

		private static bool IgnoreLayer(Layer layer, IEnumerable<string> ignoredClasses)
		{
			FeatureClass featureClass = (layer as FeatureLayer)?.GetTable() as FeatureClass;

			if (featureClass == null)
			{
				return true;
			}

			string className = featureClass.GetName();

			foreach (string ignoredClass in ignoredClasses)
			{
				if (className.EndsWith(ignoredClass, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		#endregion
	}
}
