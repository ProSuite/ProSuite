using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

public class PartialDestroyAndRebuildOptions : PartialOptionsBase
{
	#region Overridable Settings

	[CanBeNull]
	[UsedImplicitly]
	public OverridableSetting<bool> HighlightOriginalGeometry { get; set; }

	[CanBeNull]
	[UsedImplicitly]
	public OverridableSetting<bool> HideEditedFeature { get; set; }

	[CanBeNull]
	[UsedImplicitly]
	public OverridableSetting<bool> MoveOpenJawEndJunction { get; set; }

	[CanBeNull]
	[UsedImplicitly]
	public OverridableSetting<DestroyAndRebuildLinearNetworkDefinition>
		LinearNetworkDefinition { get; set; }

	#endregion

	public override PartialOptionsBase Clone()
	{
		var result = new PartialDestroyAndRebuildOptions
		             {
			             HighlightOriginalGeometry = TryClone(HighlightOriginalGeometry),
			             HideEditedFeature = TryClone(HideEditedFeature),
			             MoveOpenJawEndJunction = TryClone(MoveOpenJawEndJunction),
			             LinearNetworkDefinition = TryClone(LinearNetworkDefinition)
		             };

		return result;
	}
}
