using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.RemoveOverlaps;

public class RemoveOverlapsResult
{
	#region Result objects produced when storing features

	public IList<OverlapResultGeometries> ResultsByFeature { get; } =
		new List<OverlapResultGeometries>();

	[CanBeNull]
	public IDictionary<Feature, Geometry> TargetFeaturesToUpdate { get; set; }

	public bool ResultHasMultiparts { get; set; }

	#endregion

	#region Result objects produced when storing features

	public IList<Feature> NewOverlapFeatures { get; } = new List<Feature>();

	public IList<Feature> AllResultFeatures { get; } = new List<Feature>();

	[NotNull]
	public IList<string> NonStorableMessages { get; } = new List<string>(0);

	#endregion
}
