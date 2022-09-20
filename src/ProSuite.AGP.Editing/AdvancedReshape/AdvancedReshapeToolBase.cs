using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Microservices.Client.AGP;
using ProSuite.Microservices.Client.AGP.GeometryProcessing;
using ProSuite.Microservices.Client.AGP.GeometryProcessing.AdvancedReshape;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public abstract class AdvancedReshapeToolBase : ConstructionToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		// TODO:
		// - Performance improvements for large objects (clip extent)
		// - Performance improvement for (large) polygon feedback (no simplify etc.)
		// - Make sure feedback microservices are called only once
		// - Move to OnKeyUp in reshape side toggle
		// - Options
		// - Circumcision dialog
		// - Reshape size change logging - use abbreviations for display units
		// - R(estore) sketch
		// - Connected lines reshape
		// - Update feedback on toggle layer visibility

		private AdvancedReshapeFeedback _feedback;

		private Task<bool> _updateFeedbackTask;

		private bool _nonDefaultSideMode;
		private CancellationTokenSource _cancellationTokenSource;
		private const Key _keyToggleNonDefaultSide = Key.S;

		protected AdvancedReshapeToolBase()
		{
			// This is our property:
			RequiresSelection = true;

			SelectionCursor = ToolUtils.GetCursor(Resources.AdvancedReshapeToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.AdvancedReshapeToolCursorShift);

			HandledKeys.Add(_keyToggleNonDefaultSide);
		}

		protected abstract GeometryProcessingClient MicroserviceClient { get; }

		protected override void OnUpdate()
		{
			Enabled = MicroserviceClient != null;

			if (MicroserviceClient == null)
				DisabledTooltip =
					"Microservice not found or not started. Please make sure the latest ProSuite Extension is installed.";
		}

		protected override bool HandleEscape()
		{
			_cancellationTokenSource?.Cancel();

			return base.HandleEscape();
		}

		protected override void LogPromptForSelection()
		{
			// TODO: Only usable selection
			bool hasSelection = ActiveMapView.Map.SelectionCount > 0;

			// If the last reshape was just finished and the feature is still selected, Enter is entering the sketch mode again using the same selection
			string enterMsg =
				hasSelection
					? LocalizableStrings.AdvancedReshapeTool_LogPromptForSelection_Enter
					: string.Empty;

			_msg.InfoFormat(LocalizableStrings.AdvancedReshapeTool_LogPromptForSelection,
			                enterMsg);
		}

		protected override void LogEnteringSketchMode()
		{
			string logText = LocalizableStrings.AdvancedReshapeTool_LogEnteringSketchMode;
			//	"Sketch the reshape line to change the selection.<br>- Press R to restore the sketch from the previous reshape operation.<br>- Press S to toggle the non-default reshape side of the geometry.<br>- Press ESC to select different features.";

			int selectionCount = ActiveMapView.Map.SelectionCount;

			if (selectionCount > 1)
			{
				// What about lines? Idea: hyperlinks that open the relevant page in the help
				logText +=
					string.Format(
						"{0}- Press N to define the target connection point for the extended shared boundary when reshaping two polygons",
						Environment.NewLine);
			}

			_msg.Info(logText);
		}

		protected override bool CanSelectGeometryType(GeometryType geometryType)
		{
			return geometryType == GeometryType.Polyline ||
			       geometryType == GeometryType.Polygon;
		}

		protected override void OnToolActivatingCore()
		{
			_feedback = new AdvancedReshapeFeedback();

			base.OnToolActivatingCore();
		}

		protected override void OnToolDeactivateCore(bool hasMapViewChanged)
		{
			_feedback?.Clear();
			_feedback = null;

			base.OnToolDeactivateCore(hasMapViewChanged);
		}

		protected override SketchGeometryType GetSketchGeometryType()
		{
			return SketchGeometryType.Line;
		}

		protected override async Task<bool> OnSketchModifiedAsync()
		{
			_msg.VerboseDebug(() => "OnSketchModifiedAsync");

			if (_updateFeedbackTask != null)
			{
				// Still working on the previous update (large poylgons!)
				// -> Consider using latch (but ensure that the overlay must probably be scheduled properly)
				return false;
			}

			bool nonDefaultSide =
				_nonDefaultSideMode || PressedKeys.Contains(_keyToggleNonDefaultSide);

			bool updated = false;
			try
			{
				if (! PressedKeys.Contains(Key.Space))
				{
					// TODO: Exclude finish sketch by double clicking -> should not calculate preview
					//       E.g. wait for SystemInformation.DoubleClickTime for the second click
					//       and only start if it has not ocvurred
					_updateFeedbackTask = UpdateFeedbackAsync(nonDefaultSide);
					updated = await _updateFeedbackTask;
				}
			}
			catch (Exception e)
			{
				_msg.Warn("Error generating preview", e);
				return false;
			}
			finally
			{
				_updateFeedbackTask = null;
			}

			// Does it make any difference what the return value is?
			return updated;
		}

		protected override async Task HandleKeyDownAsync(MapViewKeyEventArgs k)
		{
			try
			{
				if (k.Key == _keyToggleNonDefaultSide)
				{
					_nonDefaultSideMode = ! _nonDefaultSideMode;

					_msg.Info(_nonDefaultSideMode
						          ? "Enabled non-default reshape mode. The next reshape to the inside of a polygon will remove the larger area. The next Y-Reshape will use the farther end-point."
						          : "Disabled non-default reshape mode");

					if (_updateFeedbackTask != null)
					{
						// Still working on the previous update (large poylgons!)
						return;
					}

					_updateFeedbackTask = UpdateFeedbackAsync(_nonDefaultSideMode);

					await _updateFeedbackTask;
				}
			}
			catch (Exception e)
			{
				_msg.Warn("Error generating preview", e);
			}
			finally
			{
				_updateFeedbackTask = null;
			}
		}

		//protected override void OnKeyUpCore(MapViewKeyEventArgs k)
		//{
		//	_msg.VerboseDebug(() => "OnKeyUpCore");

		//	if (k.Key == _keyToggleNonDefaultSide)
		//	{
		//		_nonDefaultSideMode = ! _nonDefaultSideMode;

		//		k.Handled = true;
		//	}
		//	else if (k.Key == Key.Space)
		//	{
		//		k.Handled = true;
		//	}

		//	base.OnKeyUpCore(k);
		//}

		//protected override async Task HandleKeyUpAsync(MapViewKeyEventArgs k)
		//{
		//	// At 2.5 this is never called (despite setting k.Handled = true above).
		// TODO: Test in 2.6/2.7
		//	try
		//	{
		//		if (k.Key == _keyToggleNonDefaultSide ||
		//		    k.Key == Key.Space)
		//		{
		//			_updateFeedbackTask = UpdateFeedbackAsync(_nonDefaultSideMode);

		//			await _updateFeedbackTask;
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		_msg.Warn("Error generating preview", e);
		//	}
		//	finally
		//	{
		//		_updateFeedbackTask = null;
		//	}
		//}

		protected override async Task<bool> OnEditSketchCompleteCoreAsync(
			Geometry sketchGeometry, EditingTemplate editTemplate, MapView activeView,
			CancelableProgressor cancelableProgressor = null)
		{
			_feedback.Clear();

			// TODO: cancel all running background tasks...

			var polyline = (Polyline) sketchGeometry;

			List<Feature> selection;

			bool success = await QueuedTaskUtils.Run(async () =>
			{
				try
				{
					SetCursor(Cursors.Wait);

					selection = GetApplicableSelectedFeatures(activeView).ToList();

					var potentiallyAffectedFeatures =
						GetAdjacentFeatures(selection, cancelableProgressor);

					// This timout should be enough even in extreme circumstances:
					int timeout = selection.Count * 10000;
					_cancellationTokenSource = new CancellationTokenSource(timeout);

					ReshapeResult result = MicroserviceClient.Reshape(
						selection, polyline, potentiallyAffectedFeatures, true, true,
						_nonDefaultSideMode, _cancellationTokenSource.Token);

					if (result == null)
					{
						return false;
					}

					HashSet<long> editableClassHandles =
						MapUtils.GetLayers<BasicFeatureLayer>(
							        bfl => bfl.GetTable() != null && bfl.IsEditable)
						        .Select(l => l.GetTable().Handle.ToInt64()).ToHashSet();

					Dictionary<Feature, Geometry> resultFeatures =
						result.ResultFeatures
						      .Where(r => GdbPersistenceUtils.CanChange(
							             r, editableClassHandles, RowChangeType.Update))
						      .ToDictionary(r => r.Feature, r => r.NewGeometry);

					success = await SaveAsync(resultFeatures);

					LogReshapeResults(result, selection.Count);

					// At some point, hopefully, read-only operations on the CIM model can run in parallel
					await ToolUtils.FlashResultPolygonsAsync(activeView, resultFeatures);

					return success;
				}
				finally
				{
					// Anything but the Wait cursor
					SetCursor(Cursors.Arrow);
				}
			});

			_nonDefaultSideMode = false;

			//if (!_advancedReshapeOptions.RemainInSketchMode)
			{
				StartSelectionPhase();
			}

			return success; // taskSave.Result;
		}

		protected override void OnSketchResetCore()
		{
			_feedback.Clear();
			_nonDefaultSideMode = false;
		}

		private async Task<bool> UpdateFeedbackAsync(bool nonDefaultSide)
		{
			var sketchPolyline = await GetCurrentSketchAsync() as Polyline;

			if (sketchPolyline == null || sketchPolyline.IsEmpty || sketchPolyline.PointCount < 2)
			{
				_feedback?.Clear();
				return true;
			}

			// Snapshot:
			MapView activeMapView = ActiveMapView;

			List<Feature> polylineSelection;
			List<Feature> polygonSelection;

			bool result =
				await QueuedTaskUtils.Run(
					async () =>
					{
						List<Feature> selection =
							GetApplicableSelectedFeatures(activeMapView).ToList();

						polylineSelection =
							GdbObjectUtils.Filter(selection, GeometryType.Polyline).ToList();

						polygonSelection =
							GdbObjectUtils.Filter(selection, GeometryType.Polygon).ToList();

						bool updated =
							await UpdateOpenJawReplacedEndpointAsync(nonDefaultSide, sketchPolyline,
								polylineSelection);

						updated |= await UpdatePolygonResultPreviewAsync(
							           nonDefaultSide, sketchPolyline,
							           polygonSelection);

						return updated;
					});

			return result;
		}

		[CanBeNull]
		private IList<Feature> GetAdjacentFeatures(
			[NotNull] ICollection<Feature> selectedFeatures,
			[CanBeNull] CancelableProgressor cancellabelProgressor)
		{
			Dictionary<MapMember, List<long>> selection = ActiveMapView.Map.GetSelection();

			if (! selection.Keys.Any(mm => mm is FeatureLayer fl &&
			                               fl.ShapeType == esriGeometryType.esriGeometryPolyline))
			{
				return null;
			}

			Envelope inExtent = ActiveMapView.Extent;

			// TODO: Use linear network classes as defined in reshape options
			TargetFeatureSelection targetFeatureSelection = TargetFeatureSelection.SameClass;

			var featureFinder = new FeatureFinder(ActiveMapView, targetFeatureSelection);

			IEnumerable<FeatureClassSelection> featureClassSelections =
				featureFinder.FindIntersectingFeaturesByFeatureClass(
					selection, layer => layer.ShapeType == esriGeometryType.esriGeometryPolyline,
					inExtent, cancellabelProgressor);

			if (cancellabelProgressor != null &&
			    cancellabelProgressor.CancellationToken.IsCancellationRequested)
			{
				return new List<Feature>();
			}

			var foundFeatures = new List<Feature>();

			foreach (var keyValuePair in featureClassSelections)
			{
				foundFeatures.AddRange(keyValuePair.GetFeatures());
			}

			foundFeatures.RemoveAll(
				f => selectedFeatures.Any(s => GdbObjectUtils.IsSameFeature(f, s)));

			return foundFeatures;
		}

		private void LogReshapeResults(ReshapeResult reshapeResult,
		                               int applicableSelectionCount)
		{
			var result = new Dictionary<Feature, Geometry>();

			foreach (var resultFeature in reshapeResult.ResultFeatures)
			{
				var feature = resultFeature.Feature;

				result.Add(feature, resultFeature.NewGeometry);

				string message = StringUtils.Concatenate(resultFeature.Messages, ". ");

				if (! string.IsNullOrEmpty(message))
				{
					message =
						$"{DatasetUtils.GetAliasName(feature.GetTable())} <oid> {feature.GetObjectID()}: {message}";

					if (resultFeature.HasWarningMessage)
					{
						_msg.Warn(message);
					}
					else
					{
						_msg.Info(message);
					}
				}
			}

			if (result.Count == 0)
			{
				_msg.Warn(reshapeResult.FailureMessage);
			}
			else
			{
				// Currently the non-reshaped failures (for several features) are in the global message:
				if (applicableSelectionCount > 1 &&
				    ! string.IsNullOrEmpty(reshapeResult.FailureMessage))
				{
					_msg.Warn(reshapeResult.FailureMessage);
				}

				// Log individual reshape messages
				string titleMessage = string.Empty;
				if (applicableSelectionCount > 1)
				{
					titleMessage = $"Reshaped {reshapeResult.ResultFeatures.Count} " +
					               $"of {applicableSelectionCount} selected features:";
				}

				LogSuccessfulReshape(titleMessage, result);
			}
		}

		private static async Task<bool> SaveAsync(IDictionary<Feature, Geometry> resultFeatures)
		{
			return await GdbPersistenceUtils.SaveInOperationAsync(
				       "Advanced reshape", resultFeatures);
		}

		private async Task<bool> UpdateOpenJawReplacedEndpointAsync(
			bool useNonDefaultReshapeSide,
			[NotNull] Polyline sketchLine,
			[NotNull] IList<Feature> polylineSelection)
		{
			// TODO: check options (allow/disallow)

			MapPoint endPoint = null;

			if (polylineSelection.Count == 1)
			{
				endPoint = await MicroserviceClient.GetOpenJawReplacementPointAsync(
					           polylineSelection[0], sketchLine, useNonDefaultReshapeSide);
			}

			_feedback?.UpdateOpenJawReplacedEndPoint(endPoint);

			return true;
		}

		private async Task<bool> UpdatePolygonResultPreviewAsync(
			bool nonDefaultSide,
			[NotNull] Polyline sketchPolyline,
			[NotNull] List<Feature> polygonSelection)
		{
			ReshapeResult reshapeResult = null;

			if (polygonSelection.Count != 0)
			{
				// Idea: ReshapeOperation class that contains the options to build the rpc, cancellation token, time out settings and logging.

				// TODO: Keep this source and cancel in case finish sketch happens
				var cancellationTokenSource = new CancellationTokenSource(3000);

				reshapeResult = MicroserviceClient.TryReshape(
					polygonSelection, sketchPolyline, null, false, true,
					nonDefaultSide, cancellationTokenSource.Token);
			}

			// TODO: discard result if the user has since clicked again or some other event (finish / esc) happened

			// TODO in 2.6 and higher try:
			//FrameworkApplication.QueueIdleAction(
			//	() => _feedback?.UpdatePreview(reshapeResult.ResultFeatures));

			return await QueuedTaskUtils.Run(
				       () => _feedback?.UpdatePreview(reshapeResult?.ResultFeatures));
		}

		private void LogSuccessfulReshape(
			[CanBeNull] string titleMessage,
			[NotNull] Dictionary<Feature, Geometry> reshapedGeometries)
		{
			IEnumerable<string> messages = GetReshapedFeaturesMessages(
				reshapedGeometries);

			string msg = string.Empty;
			if (! string.IsNullOrEmpty(titleMessage))
			{
				msg += titleMessage;
			}

			foreach (string message in messages)
			{
				if (! string.IsNullOrEmpty(msg))
				{
					msg += Environment.NewLine;
				}

				msg += message;
			}

			_msg.Info(msg);
		}

		private IEnumerable<string> GetReshapedFeaturesMessages(
			[NotNull] IDictionary<Feature, Geometry> reshapedFeatures)
		{
			IList<string> result = new List<string>();

			foreach (
				KeyValuePair<Feature, Geometry> keyValuePair in reshapedFeatures)
			{
				Feature feature = keyValuePair.Key;
				Geometry updatedGeometry = keyValuePair.Value;

				double sizeChangeMapUnits =
					GetAreaOrLength(updatedGeometry) -
					GetAreaOrLength(feature.GetShape());

				// TODO: Get actual linear unit and convert (s. AdvancedReshaper)
				Unit mapUnits = ActiveMapView.Map.SpatialReference.Unit;

				bool is2D = updatedGeometry.Dimension == 2;
				string sizeChangeText = GetSizeChangeText(sizeChangeMapUnits, is2D,
				                                          mapUnits);

				// TODO: RowFormat
				string rowDisplayText = GdbObjectUtils.ToString(feature);

				result.Add(string.Format("{0} was reshaped and is now {1}",
				                         rowDisplayText, sizeChangeText));
			}

			return result;
		}

		private static double GetAreaOrLength(Geometry geometry)
		{
			Polygon polygon = geometry as Polygon;

			if (polygon != null)
			{
				return polygon.Area;
			}

			Polyline polyline = geometry as Polyline;

			if (polyline != null)
			{
				return polyline.Length;
			}

			return 0;
		}

		private static string GetSizeChangeText(double sizeDifference,
		                                        bool isArea,
		                                        Unit displayUnits)
		{
			string unitDisplay = $"{displayUnits}{(isArea ? "²" : string.Empty)}";

			bool more = sizeDifference > 0;

			string sizeAdjective = GetSizeAdjective(more, isArea);

			const int significantDigits = 3;

			double displayNumber =
				Math.Abs(
					MathUtils.RoundToSignificantDigits(
						sizeDifference, significantDigits));

			// fix 72000000 mm where it would actually be 721234567
			if (Math.Abs(Math.Truncate(displayNumber) - displayNumber) < double.Epsilon)
			{
				displayNumber = Math.Abs(Math.Truncate(sizeDifference));
			}

			// fix 2.1E-05 nm²
			string formatNonScientific = StringUtils.FormatNonScientific(
				displayNumber, CultureInfo.CurrentCulture);

			string lengthText = string.Format(
				"{0} {1} {2}",
				formatNonScientific,
				unitDisplay, sizeAdjective);

			return lengthText;
		}

		private static string GetSizeAdjective(bool isMore, bool isArea)
		{
			string sizeAdjective;
			if (isMore)
			{
				if (isArea)
				{
					sizeAdjective = "larger";
				}
				else
				{
					sizeAdjective = "longer";
				}
			}
			else
			{
				if (isArea)
				{
					sizeAdjective = "smaller";
				}
				else
				{
					sizeAdjective = "shorter";
				}
			}

			return sizeAdjective;
		}
	}
}
