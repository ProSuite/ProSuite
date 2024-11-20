using ProSuite.Commons.ManagedOptions;

namespace ProSuite.AGP.Editing.AdvancedReshape
{
	public class PartialReshapeToolOptions : PartialOptionsBase
	{
		#region Overridable Settings

		public OverridableSetting<bool> ShowPreview { get; set; }

		public OverridableSetting<bool> MoveOpenJawEndJunction { get; set; }

		public OverridableSetting<bool> RemainInSketchMode { get; set; }

		public OverridableSetting<bool> UseTopologyTypeSelection { get; set; }

		#endregion

		public override PartialOptionsBase Clone()
		{
			var result = new PartialReshapeToolOptions
			             {
				             ShowPreview = TryClone(ShowPreview), 
				             MoveOpenJawEndJunction = TryClone(MoveOpenJawEndJunction), 
				             RemainInSketchMode = TryClone(RemainInSketchMode), 
				             UseTopologyTypeSelection = TryClone(UseTopologyTypeSelection)
			             };
			return result;
		}
	}
}
