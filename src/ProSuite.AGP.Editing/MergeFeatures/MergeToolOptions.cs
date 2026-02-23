using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.MergeFeatures;

public class MergeToolOptions : OptionsBase<PartialMergeOptions>
{
	public CentralizableSetting<bool> CentralizableUseMergeResultForNextMerge { get; }

	public CentralizableSetting<MergeOperationSurvivor> CentralizableMergeOperationSurvivor { get; }

	public CentralizableSetting<bool> CentralizableTransferRelationships { get; }

	public CentralizableSetting<bool> CentralizablePreventInconsistentMerge { get; }

	public CentralizableSetting<bool> CentralizablePreventMultipartResult { get; }

	public CentralizableSetting<bool> CentralizablePreventInconsistentClasses { get; }

	public CentralizableSetting<bool> CentralizablePreventInconsistentAttributes { get; }

	public CentralizableSetting<bool> CentralizablePreventInconsistentRelationships { get; }

	public CentralizableSetting<bool> CentralizablePreventLoops { get; }

	public CentralizableSetting<bool> CentralizablePreventLineFlip { get; }

	public MergeToolOptions(
		[CanBeNull] PartialMergeOptions centralOptions,
		[CanBeNull] PartialMergeOptions localOptions)
	{
		CentralOptions = centralOptions;

		LocalOptions = localOptions ?? new PartialMergeOptions();

		CentralizableUseMergeResultForNextMerge = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.UseMergeResultForNextMerge),
			false);

		CentralizableMergeOperationSurvivor = InitializeSetting
			<MergeOperationSurvivor>(
				ReflectionUtils.GetProperty(() => LocalOptions.MergeOperationSurvivor),
				MergeOperationSurvivor.LargerObject);

		CentralizableTransferRelationships = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.TransferRelationships),
			false);

		CentralizablePreventInconsistentMerge = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.PreventInconsistentMerge),
			false);

		CentralizablePreventMultipartResult = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.PreventMultipartResult),
			true);

		CentralizablePreventInconsistentClasses = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.PreventInconsistentClasses),
			true);

		CentralizablePreventInconsistentAttributes = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.PreventInconsistentAttributes),
			true);

		CentralizablePreventInconsistentRelationships = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.PreventInconsistentRelationships),
			true);

		CentralizablePreventLoops = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.PreventLoops),
			true);

		CentralizablePreventLineFlip = InitializeSetting<bool>(
			ReflectionUtils.GetProperty(() => LocalOptions.PreventLineFlip),
			false);
	}

	public bool UseMergeResultForNextMerge
	{
		get { return CentralizableUseMergeResultForNextMerge.CurrentValue; }
		set { CentralizableUseMergeResultForNextMerge.CurrentValue = value; }
	}

	public MergeOperationSurvivor MergeSurvivor
	{
		get { return CentralizableMergeOperationSurvivor.CurrentValue; }
		set { CentralizableMergeOperationSurvivor.CurrentValue = value; }
	}

	public bool TransferRelationships
	{
		get { return CentralizableTransferRelationships.CurrentValue; }
		set { CentralizableTransferRelationships.CurrentValue = value; }
	}

	public bool PreventInconsistentMerge
	{
		get { return CentralizablePreventInconsistentMerge.CurrentValue; }
		set { CentralizablePreventInconsistentMerge.CurrentValue = value; }
	}

	public bool PreventMultipartResult
	{
		get { return CentralizablePreventMultipartResult.CurrentValue; }
		set { CentralizablePreventMultipartResult.CurrentValue = value; }
	}

	public bool PreventInconsistentClasses
	{
		get { return CentralizablePreventInconsistentClasses.CurrentValue; }
		set { CentralizablePreventInconsistentClasses.CurrentValue = value; }
	}

	public bool PreventInconsistentAttributes
	{
		get { return CentralizablePreventInconsistentAttributes.CurrentValue; }
		set { CentralizablePreventInconsistentAttributes.CurrentValue = value; }
	}

	public bool PreventInconsistentRelationships
	{
		get { return CentralizablePreventInconsistentRelationships.CurrentValue; }
		set { CentralizablePreventInconsistentRelationships.CurrentValue = value; }
	}

	public bool PreventLoops
	{
		get { return CentralizablePreventLoops.CurrentValue; }
		set { CentralizablePreventLoops.CurrentValue = value; }
	}

	public bool PreventLineFlip
	{
		get { return CentralizablePreventLineFlip.CurrentValue; }
		set { CentralizablePreventLineFlip.CurrentValue = value; }
	}

	#region Overrides of OptionsBase<PartialMergeOptions>

	public override void RevertToDefaults()
	{
		CentralizableUseMergeResultForNextMerge.RevertToDefault();

		CentralizableMergeOperationSurvivor.RevertToDefault();

		CentralizableTransferRelationships.RevertToDefault();

		CentralizablePreventInconsistentMerge.RevertToDefault();

		CentralizablePreventMultipartResult.RevertToDefault();

		CentralizablePreventInconsistentClasses.RevertToDefault();

		CentralizablePreventInconsistentAttributes.RevertToDefault();

		CentralizablePreventInconsistentRelationships.RevertToDefault();

		CentralizablePreventLoops.RevertToDefault();

		CentralizablePreventLineFlip.RevertToDefault();
	}

	public override bool HasLocalOverrides(NotificationCollection notifications)
	{
		bool result = false;

		if (HasLocalOverride(CentralizableUseMergeResultForNextMerge,
		                     "Directly use merge result as first feature for subsequent merge operation",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableMergeOperationSurvivor,
		                     "Attribute values and identity to preserve",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizableTransferRelationships,
		                     "Transfer Relationships from deleted features, if possible",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizablePreventInconsistentMerge,
		                     "Prevent inconsistent feature merge",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizablePreventMultipartResult,
		                     "Prevent multi-part result geometry",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizablePreventInconsistentClasses,
		                     "Features must be from the same feature class",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizablePreventInconsistentAttributes,
		                     "Object-defining attributes must be equal",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizablePreventInconsistentRelationships,
		                     "Relationships must be equal",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizablePreventLoops,
		                     "Prevent closed line loops",
		                     notifications))
		{
			result = true;
		}

		if (HasLocalOverride(CentralizablePreventLineFlip,
		                     "Lines must have the same direction",
		                     notifications))
		{
			result = true;
		}

		return result;
	}

	public override string GetLocalOverridesMessage()
	{
		const string optionsName = "Merge Options";

		return GetLocalOverridesMessage(optionsName);
	}

	#endregion

	#region Overrides of Object

	public override string ToString()
	{
		return $"Merge options: {Environment.NewLine}" +
		       $"Directly use merge result as first feature for subsequent merge operation: {UseMergeResultForNextMerge}{Environment.NewLine}" +
		       $"Preserve attribute values and identity of: {MergeSurvivor}{Environment.NewLine}" +
		       $"Transfer relationships from deleted feature, if possible: {TransferRelationships}{Environment.NewLine}" +
		       $"Prevent inconsistent feature merge: {PreventInconsistentMerge}{Environment.NewLine}" +
		       $"  Prevent multi-part result geometry: {PreventMultipartResult}{Environment.NewLine}" +
		       $"  Features must be from the same feature class: {PreventInconsistentClasses}{Environment.NewLine}" +
		       $"    Object-defining attributes must be equal: {PreventInconsistentAttributes}{Environment.NewLine}" +
		       $"    Relationships must be equal: {PreventInconsistentRelationships}{Environment.NewLine}" +
		       $"  Prevent closed line loops: {PreventLoops}{Environment.NewLine}" +
		       $"  Lines must have the same direction: {PreventLineFlip}{Environment.NewLine}";
	}

	#endregion
}
