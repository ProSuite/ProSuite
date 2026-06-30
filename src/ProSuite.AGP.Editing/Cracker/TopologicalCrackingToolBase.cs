using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Cracker;

public abstract class TopologicalCrackingToolBase : TwoPhaseEditToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	// Cancellation source for the in-flight crack/chop point calculation.
	// It is cancelled on the UI thread when the user presses Escape (HandleEscapeAsync) or
	// starts a new recalculation (RequestRecalculation);
	private CancellationTokenSource _calculationCancellationSource;

	// Monotonic "generation" of recalculation requests (e.g. one per tool-option spinner click).
	// A queued request runs only if it is still the latest generation; superseded requests skip
	// themselves instead of piling up extra service calls. Written on the UI thread only.
	private int _recalculationGeneration;

	protected abstract ICrackerService MicroserviceClient { get; }

	/// <summary>
	/// Begins a new calculation: creates a fresh, non-cancelled cancellation source and returns
	/// its token. Call this once at the very start of a calculation, before the (potentially
	/// slow) target-feature search and the service call, so that Escape or a newer recalculation
	/// request cancels the whole calculation. Creating the token up front means a superseded
	/// calculation is cancelled before <c>GrpcClientUtils.Try</c> is reached, so the service
	/// round-trip is skipped entirely instead of running redundantly.
	/// </summary>
	protected CancellationToken BeginCalculation()
	{
		// Just in case there is still one running: Otherwise the running request can never be cancelled.
		_calculationCancellationSource?.Cancel();

		var source = new CancellationTokenSource();
		_calculationCancellationSource = source;
		return source.Token;
	}

	/// <summary>
	/// Requests a recalculation of the derived geometries after a tool option changed (e.g. a
	/// spinner click). Cancels the running calculation and supersedes any earlier pending
	/// request, so rapid successive requests do not pile up: only the most recent one runs.
	/// </summary>
	protected void RequestRecalculation()
	{
		// Abort the in-flight calculation and become the latest generation.
		_calculationCancellationSource?.Cancel();

		int generation = ++_recalculationGeneration;

		QueuedTaskUtils.Run(async () =>
		{
			// A newer request arrived before this one started: let that one do the work.
			if (generation != _recalculationGeneration)
			{
				return;
			}

			try
			{
				await ProcessSelectionAsync();
			}
			catch (Exception e)
			{
				_msg.Error($"Error re-calculating crack/chop points: {e.Message}", e);
			}
		});
	}

	protected override async Task HandleEscapeAsync()
	{
		// Cancel a running calculation: its token is in the gRPC CallOptions, so this aborts the
		// in-flight service call.
		_calculationCancellationSource?.Cancel();

		await base.HandleEscapeAsync();
	}

	protected CrackerResult CalculateCrackPoints(IList<Feature> selectedFeatures,
	                                             IList<Feature> intersectingFeatures,
	                                             ICrackerToolOptions crackerToolOptions,
	                                             IntersectionPointOptions intersectionPointOptions,
	                                             bool addCrackPointsOnExistingVertices,
	                                             CancelableProgressor progressor)
	{
		CrackerResult resultCrackPoints;

		CancellationToken cancellationToken =
			_calculationCancellationSource?.Token ?? CancellationToken.None;

		if (MicroserviceClient != null)
		{
			resultCrackPoints =
				MicroserviceClient.CalculateCrackPoints(selectedFeatures, intersectingFeatures,
				                                        crackerToolOptions,
				                                        intersectionPointOptions,
				                                        addCrackPointsOnExistingVertices,
				                                        cancellationToken);
		}
		else
		{
			throw new InvalidConfigurationException("Microservice has not been started.");
		}

		return resultCrackPoints;
	}

	#region Search target features

	[NotNull]
	protected IList<Feature> GetIntersectingFeatures(
		[NotNull] ICollection<Feature> selectedFeatures,
		[NotNull] ICrackerToolOptions crackerToolOptions,
		[CanBeNull] CancelableProgressor cancellabelProgressor)
	{
		TargetFeatureSelection targetFeatureSelection =
			crackerToolOptions.TargetFeatureSelection;

		// Snap crack points within tolerance to target vertices: enlarge search envelope.
		double extraSearchTolerance = 0.0;
		if (crackerToolOptions.SnapToTargetVertices)
		{
			extraSearchTolerance = crackerToolOptions.SnapTolerance;
		}

		Dictionary<MapMember, List<long>> selection =
			SelectionUtils.GetSelection(ActiveMapView.Map);

		return ToolUtils.GetIntersectingFeatures(selection, ActiveMapView, targetFeatureSelection,
		                                         extraSearchTolerance,
		                                         GetTargetFeatureClassPredicate(),
		                                         cancellabelProgressor);
	}

	protected virtual Predicate<FeatureClass> GetTargetFeatureClassPredicate()
	{
		return null;
	}

	#endregion

	protected CrackerResult SelectCrackPointsToApply(CrackerResult crackerResultPoints,
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
}
