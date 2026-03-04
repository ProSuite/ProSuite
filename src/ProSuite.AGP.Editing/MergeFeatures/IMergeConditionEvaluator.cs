using System.Collections.Generic;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Notifications;

namespace ProSuite.AGP.Editing.MergeFeatures;

public interface IMergeConditionEvaluator
{
	/// <summary>
	/// The merge tool options that govern which consistency conditions are checked.
	/// When null, all checks are skipped.
	/// </summary>
	[CanBeNull]
	MergeToolOptions Options { get; set; }

	/// <summary>
	/// Hard structural check (e.g. network topology). If this returns false the merge must
	/// be aborted, regardless of user options.
	/// </summary>
	bool CanMerge(
		[NotNull] Feature referenceFeature,
		[NotNull] IEnumerable<Feature> otherFeatures,
		[NotNull] ICollection<MergeFailInfo> failingReasons);

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
