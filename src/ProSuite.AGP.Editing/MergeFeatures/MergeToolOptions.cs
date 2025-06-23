using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Reflection;

namespace ProSuite.AGP.Editing.MergeFeatures
{
	public class MergeToolOptions : OptionsBase<PartialMergeOptions>
	{
		public CentralizableSetting<bool> CentralizableUseMergeResultForNextMerge
		{
			get;
			private set;
		}

		public CentralizableSetting<MergeOperationSurvivor>
			CentralizableMergeOperationSurvivor { get; private set; }

		public CentralizableSetting<bool> CentralizableAllowMultipartResult { get; private set; }

		public CentralizableSetting<bool> CentralizableTransferRelationships { get; private set; }

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

			CentralizableAllowMultipartResult = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.AllowMultipartResult),
				false);

			CentralizableTransferRelationships = InitializeSetting<bool>(
				ReflectionUtils.GetProperty(() => LocalOptions.TransferRelationships),
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

		public bool AllowMultipartResult
		{
			get { return CentralizableAllowMultipartResult.CurrentValue; }
			set { CentralizableAllowMultipartResult.CurrentValue = value; }
		}

		public bool TransferRelationships
		{
			get { return CentralizableTransferRelationships.CurrentValue; }
			set { CentralizableTransferRelationships.CurrentValue = value; }
		}

		#region Overrides of OptionsBase<PartialMergeOptions>

		public override void RevertToDefaults()
		{
			CentralizableUseMergeResultForNextMerge.RevertToDefault();

			CentralizableMergeOperationSurvivor.RevertToDefault();

			CentralizableAllowMultipartResult.RevertToDefault();

			CentralizableTransferRelationships.RevertToDefault();
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

			if (HasLocalOverride(CentralizableAllowMultipartResult,
			                     "Allow multi-part result geometry",
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
			       $"Allow multi-part result geometry: {AllowMultipartResult}{Environment.NewLine}" +
			       $"Transfer relationships from deleted feature, if possible: {TransferRelationships}{Environment.NewLine}";
		}

		#endregion
	}
}
