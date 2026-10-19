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

	private CancellationTokenSource _calculationCancellationSource;
	private int _recalculationGeneration;

	protected abstract ICrackerService MicroserviceClient { get; }

	protected CancellationToken CreateCancellationToken()
	{
		_calculationCancellationSource?.Cancel();

		var source = new CancellationTokenSource();
		_calculationCancellationSource = source;
		return source.Token;
	}

	/// <summary>
	/// Call after a tool option changed. Only the most recent request runs.
	/// </summary>
	protected void RequestRecalculation()
	{
		_calculationCancellationSource?.Cancel();

		int generation = ++_recalculationGeneration;

		QueuedTaskUtils.Run(async () =>
		{
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
