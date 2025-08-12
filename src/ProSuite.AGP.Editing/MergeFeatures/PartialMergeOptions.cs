using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.MergeFeatures
{
	public class PartialMergeOptions : PartialOptionsBase
	{
		[CanBeNull]
		public OverridableSetting<bool> UseMergeResultForNextMerge { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> AllowMultipartResult { get; set; }

		[CanBeNull]
		public OverridableSetting<MergeOperationSurvivor> MergeOperationSurvivor { get; set; }

		[CanBeNull]
		public OverridableSetting<bool> TransferRelationships { get; set; }

		#region Overrides of PartialOptionsBase

		public override PartialOptionsBase Clone()
		{
			var result = new PartialMergeOptions();

			result.UseMergeResultForNextMerge = TryClone(UseMergeResultForNextMerge);
			result.AllowMultipartResult = TryClone(AllowMultipartResult);
			result.MergeOperationSurvivor = MergeOperationSurvivor;
			result.TransferRelationships = TryClone(TransferRelationships);
			return result;
		}

		#endregion
	}
}
