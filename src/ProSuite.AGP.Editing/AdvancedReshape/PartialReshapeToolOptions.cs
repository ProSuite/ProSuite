using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public class PartialReshapeToolOptions : PartialOptionsBase
	{
		#region Overridable Settings

		public OverridableSetting<bool> AllowOpenJawReshape { get; set; }

		public OverridableSetting<bool> MultiReshapeAsUnion { get; set; }

		public OverridableSetting<bool> TryReshapeNonDefault { get; set; }

		public OverridableSetting<bool> UseNonDefaultReshapeSide { get; set; }

		#endregion

		public override PartialOptionsBase Clone()
		{
			var result = new PartialReshapeToolOptions
			             {
				             AllowOpenJawReshape = TryClone(AllowOpenJawReshape), // YReshape
				             MultiReshapeAsUnion = TryClone(MultiReshapeAsUnion), // "N" Key (N is already used by AGP)
				             TryReshapeNonDefault = TryClone(TryReshapeNonDefault), // M Key
				             UseNonDefaultReshapeSide = TryClone(UseNonDefaultReshapeSide) // S Key
			             };
			return result;
		}
	}
}
