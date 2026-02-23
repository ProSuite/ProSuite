using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

namespace ProSuite.AGP.Editing.MergeFeatures;

public interface IMergeConditionEvaluator
{
	bool PreventMultipartResult { get; set; }
	bool PreventInconsistentClasses { get; set; }
	bool PreventInconsistentAttributes { get; set; }
	bool PreventInconsistentRelationships { get; set; }
	bool PreventLoops { get; set; }
	bool PreventLineFlip { get; set; }

	/// <summary>
	/// Hard structural check (e.g. network topology). If this returns false the merge must
	/// be aborted, regardless of user options.
	/// </summary>
	bool CanMerge(
		[NotNull] Feature firstEdge,
		[NotNull] Feature secondEdge,
		[NotNull] ICollection<MergeFailInfo> failingReasons,
		[CanBeNull] MapPoint pointBetweenEdges = null);

	/// <summary>
	/// Desired consistency conditions (attributes, relationships, direction, loops, classes).
	/// Controlled by the individual Prevent* option properties.
	/// </summary>
	bool EvaluateInconsistencies(
		[NotNull] Feature firstEdge,
		[NotNull] Feature secondEdge,
		[NotNull] ICollection<MergeFailInfo> failingReasons,
		bool collectAllFailingReasons = false);

	/// <summary>
	/// Validates the merge result geometry (e.g. rejects multi-part results when configured).
	/// </summary>
	bool IsValidMergeResult(
		[NotNull] Geometry mergeResult,
		[NotNull] NotificationCollection notifications);
}
