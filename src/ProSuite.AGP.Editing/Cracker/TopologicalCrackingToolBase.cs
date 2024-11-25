using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.Commons.AGP.Carto;
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
				                                        crackerToolOptions, intersectionPointOptions,
				                                        addCrackPointsOnExistingVertices, cancellationToken);
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
		Dictionary<MapMember, List<long>> selection =
			SelectionUtils.GetSelection(ActiveMapView.Map);

		Envelope inExtent = ActiveMapView.Extent;

		TargetFeatureSelection targetFeatureSelection =
			crackerToolOptions.TargetFeatureSelection;

		if (targetFeatureSelection == TargetFeatureSelection.SelectedFeatures)
		{
			// NOTE: cracking within selection is signalled to the server by an empty target list.
			return new List<Feature>();
		}

		var featureFinder = new FeatureFinder(ActiveMapView, targetFeatureSelection);

		// They might be stored (insert target vertices):
		featureFinder.ReturnUnJoinedFeatures = true;

		// Snap crack points within tolerance to target vertices: enlarge search envelope.
		if (crackerToolOptions.SnapToTargetVertices)
		{
			featureFinder.ExtraSearchTolerance = crackerToolOptions.SnapTolerance;
		}

		// Set the feature classes to ignore
		IEnumerable<FeatureSelectionBase> featureClassSelections =
			featureFinder.FindIntersectingFeaturesByFeatureClass(
				selection, null, inExtent, cancellabelProgressor);

		if (cancellabelProgressor != null &&
		    cancellabelProgressor.CancellationToken.IsCancellationRequested)
		{
			return new List<Feature>();
		}

		var foundFeatures = new List<Feature>();

		foreach (FeatureSelectionBase selectionBase in featureClassSelections)
		{
			foundFeatures.AddRange(selectionBase.GetFeatures());
		}

		return foundFeatures;
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
