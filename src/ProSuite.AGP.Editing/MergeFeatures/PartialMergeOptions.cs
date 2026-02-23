using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.MergeFeatures;

public class PartialMergeOptions : PartialOptionsBase
{
	[CanBeNull]
	public OverridableSetting<bool> UseMergeResultForNextMerge { get; set; }

	[CanBeNull]
	public OverridableSetting<MergeOperationSurvivor> MergeOperationSurvivor { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> TransferRelationships { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> PreventInconsistentMerge { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> PreventMultipartResult { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> PreventInconsistentClasses { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> PreventInconsistentAttributes { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> PreventInconsistentRelationships { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> PreventLoops { get; set; }

	[CanBeNull]
	public OverridableSetting<bool> PreventLineFlip { get; set; }

	#region Overrides of PartialOptionsBase

	public override PartialOptionsBase Clone()
	{
		var result = new PartialMergeOptions();

		result.UseMergeResultForNextMerge = TryClone(UseMergeResultForNextMerge);
		result.MergeOperationSurvivor = MergeOperationSurvivor;
		result.TransferRelationships = TryClone(TransferRelationships);
		result.PreventInconsistentMerge = TryClone(PreventInconsistentMerge);
		result.PreventMultipartResult = TryClone(PreventMultipartResult);
		result.PreventInconsistentClasses = TryClone(PreventInconsistentClasses);
		result.PreventInconsistentAttributes = TryClone(PreventInconsistentAttributes);
		result.PreventInconsistentRelationships = TryClone(PreventInconsistentRelationships);
		result.PreventLoops = TryClone(PreventLoops);
		result.PreventLineFlip = TryClone(PreventLineFlip);
		return result;
	}

	#endregion
}
