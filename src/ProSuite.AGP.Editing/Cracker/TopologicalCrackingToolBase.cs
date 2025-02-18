using System;
using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.GeometryProcessing.Cracker;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;

namespace ProSuite.AGP.Editing.Cracker;

public abstract class TopologicalCrackingToolBase : TwoPhaseEditToolBase
{
	protected abstract ICrackerService MicroserviceClient { get; }

	protected CrackerResult CalculateCrackPoints(IList<Feature> selectedFeatures,
	                                             IList<Feature> intersectingFeatures,
	                                             ICrackerToolOptions crackerToolOptions,
	                                             IntersectionPointOptions intersectionPointOptions,
	                                             bool addCrackPointsOnExistingVertices,
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
