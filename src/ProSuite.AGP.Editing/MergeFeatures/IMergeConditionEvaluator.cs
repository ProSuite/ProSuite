using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.MergeFeatures
{
	public interface IMergeConditionEvaluator
	{
		bool AllowLoops { get; }

		bool IsMergeAllowed(
			[NotNull] Feature previousEdge,
			[NotNull] Feature nextEdge,
			[NotNull] ICollection<MergeFailInfo> failingReasons,
			[CanBeNull] MapPoint pointBetweenEdges = null,
			bool collectAllFailingReasons = false);
	}
}
