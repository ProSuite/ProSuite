using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.AdvancedReshape;

public class PartialAdvancedReshapeOptions : PartialOptionsBase
{
	#region Overridable Settings

	public OverridableSetting<bool> ShowPreview { get; set; }

	public OverridableSetting<bool> RemainInSketchMode { get; set; }

	public OverridableSetting<bool> AllowOpenJawReshape { get; set; }

	public OverridableSetting<bool> MoveOpenJawEndJunction { get; set; }

	public OverridableSetting<bool> UseTopologyTypeSelection { get; set; }

	#endregion

	public override PartialOptionsBase Clone()
	{
		var result = new PartialAdvancedReshapeOptions
		             {
			             ShowPreview = TryClone(ShowPreview),
			             RemainInSketchMode = TryClone(RemainInSketchMode),
			             AllowOpenJawReshape = TryClone(AllowOpenJawReshape),
			             MoveOpenJawEndJunction = TryClone(MoveOpenJawEndJunction),
			             UseTopologyTypeSelection = TryClone(UseTopologyTypeSelection)
		             };
		return result;
	}
}